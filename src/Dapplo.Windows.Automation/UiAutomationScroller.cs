// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Log;
using Dapplo.Windows.Automation.Interop;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Mouse;

namespace Dapplo.Windows.Automation;

/// <summary>
///     Scrolls an element which supports the UI Automation <c>ScrollPattern</c>. This works for nearly every application which draws
///     its own scroll bars, where <see cref="InteropWindowExtensions.GetWindowScroller"/> returns null: Chromium / Electron, Firefox,
///     WPF, WinUI, Office and the Explorer file list.
///     Controls which have no <c>ScrollPattern</c> but expose their scroll bar as a UI Automation element (e.g. the Visual Studio
///     editor) are scrolled with the mouse wheel, see <see cref="IsScrollBarFallback"/>.
///     Create it with <see cref="FromPoint"/> or <see cref="FromWindow(IntPtr, bool)"/>; it has the same shape as
///     <see cref="WindowScroller"/>, both implement <see cref="IScroller"/>.
/// </summary>
/// <remarks>
///     <para>
///     Threading: use it from a background thread (MTA, e.g. the thread pool); calls block while the target application answers.
///     Never use it on the UI thread which owns the target window (e.g. your own window), UI Automation would wait for that thread.
///     An STA thread is not needed.
///     </para>
///     <para>
///     Every property and method calls into the target process, so they always reflect the current state; a step needs about four
///     calls (position, view size, set, and one or more to see the new position). When the element is gone (the page navigated, the window closed), <see cref="IsAvailable"/> becomes false,
///     <see cref="IsAtEnd"/> and <see cref="IsAtStart"/> return true so loops end, and the methods return false;
///     <see cref="Refresh"/> finds the element again at the original location.
///     </para>
///     <para>
///     Chromium based applications (Chrome, Edge, Electron) build their accessibility tree when the first UI Automation client asks,
///     so the very first <see cref="FromPoint"/> can return null: try again after a short delay.
///     </para>
///     <para>
///     Dispose it to release the COM objects right away instead of waiting for the garbage collector.
///     </para>
/// </remarks>
public sealed class UiAutomationScroller : IScroller, IDisposable
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     How often, and with which interval in milliseconds, the position is checked after scrolling (at most half a second, only
    ///     spent when the position doesn't change or an application applies it late, like WPF at its next layout pass)
    /// </summary>
    private const int PositionChangeChecks = 50;
    private const int PositionChangeCheckInterval = 10;

    /// <summary>
    ///     Start, End and Reset with the mouse wheel: at most this many notches in one wheel input (12000, well below the 16 bit limit of
    ///     the wheel delta in WM_MOUSEWHEEL), aiming for this many pages per input
    /// </summary>
    private const int MaxNotchesPerWheelInput = 100;
    private const int PagesPerWheelInput = 5;

    /// <summary>A scroll bar at the edge of its parent: how far (in pixels) it may be from that edge</summary>
    private const int EdgeTolerance = 2;

    private readonly IUIAutomation _automation;
    private readonly NativePoint? _point;
    private readonly IntPtr _windowHandle;
    private IUIAutomationElement _element;
    private IUIAutomationScrollPattern _scrollPattern;
    private ScrollBarParts _scrollBar;
    private UiAutomationScrollModes _scrollMode = UiAutomationScrollModes.ScrollPattern;

    /// <summary>When the last position came from a thumb: the percentage one pixel of the thumb stands for, else 0</summary>
    private double _thumbPixelPercent;
    private double _stepFraction = 1.0;
    private bool _isDisposed;

    private UiAutomationScroller(IUIAutomation automation, IUIAutomationElement element, IUIAutomationScrollPattern scrollPattern, bool horizontal, NativePoint? point, IntPtr windowHandle,
        ScrollBarParts scrollBar = null)
    {
        _automation = automation;
        _element = element;
        _scrollPattern = scrollPattern;
        _scrollBar = scrollBar;
        if (scrollBar is not null)
        {
            IsScrollBarFallback = true;
            _scrollMode = UiAutomationScrollModes.MouseWheel;
        }
        Horizontal = horizontal;
        _point = point;
        _windowHandle = windowHandle;
        TryGetScrollPercent(out var percent);
        InitialScrollPercent = percent;
    }

    /// <summary>
    ///     Find the scrollable element under a screen point: the element at the point, or the first of its ancestors with a
    ///     <c>ScrollPattern</c> which can scroll in the requested direction. When there is none, the first of them (the element at the
    ///     point included) with a child scroll bar element in that direction, scrolled with the mouse wheel (<see cref="IsScrollBarFallback"/>).
    /// </summary>
    /// <param name="screenPoint">NativePoint in screen coordinates, e.g. where the user clicked</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when nothing at that point can scroll in that direction</returns>
    public static UiAutomationScroller FromPoint(NativePoint screenPoint, bool horizontal = false)
    {
        var automation = CreateAutomation();
        if (automation is null)
        {
            return null;
        }
        var scroller = FindAtPoint(automation, screenPoint, horizontal, screenPoint, IntPtr.Zero, true);
        if (scroller is null)
        {
            Release(automation);
        }
        return scroller;
    }

    /// <summary>
    ///     Find the scrollable element of a window: the window element itself when it can scroll, else the element under the middle of
    ///     the window (walking up to a scrollable ancestor), else the first scrollable descendant of the window. When none of them has
    ///     a <c>ScrollPattern</c>, the same lookups for an element with a child scroll bar element, scrolled with the mouse wheel
    ///     (<see cref="IsScrollBarFallback"/>).
    /// </summary>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when the window has nothing which can scroll in that direction</returns>
    public static UiAutomationScroller FromWindow(IntPtr windowHandle, bool horizontal = false)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }
        var automation = CreateAutomation();
        if (automation is null)
        {
            return null;
        }
        var scroller = FindInWindow(automation, windowHandle, horizontal);
        if (scroller is null)
        {
            Release(automation);
        }
        return scroller;
    }

    /// <summary>
    ///     Find the scrollable element of a window, see <see cref="FromWindow(IntPtr, bool)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="horizontal">false (default) for vertical scrolling, true for horizontal scrolling</param>
    /// <returns>UiAutomationScroller, or null when the window has nothing which can scroll in that direction</returns>
    public static UiAutomationScroller FromWindow(IInteropWindow window, bool horizontal = false) => window is null ? null : FromWindow(window.Handle, horizontal);

    /// <summary>
    ///     The default for the timeout of <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultFindTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     List the areas of a window which can scroll in the direction, without hit testing on the screen: the window element itself
    ///     and every descendant which is vertically (or horizontally) scrollable, and the parent of every scroll bar element with that
    ///     orientation (a control which scrolls by itself but has no <c>ScrollPattern</c>, like the Visual Studio editor), as their
    ///     bounding rectangles in screen coordinates (physical pixels for a per-monitor DPI aware process). Works while another window
    ///     (e.g. a full-screen selection window) covers the window, where <see cref="FromPoint"/> would find the covering window.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The result is in tree order, an outer area always before the areas inside it; areas inside each other are all returned (hit
    ///     test with the smallest rectangle containing the point). Offscreen elements and empty rectangles are left out, duplicates (e.g. a
    ///     list and its scroll viewer, or a control and the parent of its own scroll bar, with the same bounds) are returned once. A
    ///     rectangle can extend beyond the window when the element is partly scrolled out of view. The rectangle of a scroll bar's parent
    ///     leaves out the scroll bar when it is at an edge of the parent (and a scroll bar of the other orientation at an edge, e.g. a
    ///     horizontal one at the bottom of a vertically scrolled area), so a capture of the area doesn't show it; else it is the
    ///     parent's rectangle. Elements with a ScrollPattern are returned as they are.
    ///     </para>
    ///     <para>
    ///     This blocks: it is one UI Automation search (FindAll with a cache request for the bounds, the offscreen state and the control
    ///     type, so no extra call per element; one more call per scroll bar for its parent), but UI Automation walks the whole tree of
    ///     the window, which takes a while for Visual Studio, Office or browsers.
    ///     Call it on a background thread, never on the UI thread which owns the window, and cache the result. Where IUIAutomation2 is
    ///     available (Windows 8+) the connection and transaction timeouts of the UI Automation object used for this call are set to
    ///     <paramref name="timeout"/>, so a hanging application can't block the caller for the default 20 seconds.
    ///     </para>
    ///     <para>
    ///     This overload searches once. Chromium based browsers and Electron apps build their accessibility tree only when a UI Automation
    ///     client asks for it, so the first search after the browser started or a page loaded can miss the page; the overload with a
    ///     <c>contentWait</c> (<see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?, bool, TimeSpan?, CancellationToken)"/>) searches again
    ///     while nothing is found and the tree looks incomplete.
    ///     </para>
    ///     <para>
    ///     To scroll an area later, e.g. after the covering window closed, use <see cref="FromPoint"/> with the middle of its rectangle.
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="horizontal">false (default) for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultFindTimeout"/></param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IntPtr windowHandle, bool horizontal = false, TimeSpan? timeout = null)
        => FindScrollableAreas(windowHandle, horizontal, timeout, true);

    /// <summary>
    ///     List the areas of a window which can scroll in the direction, see <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?)"/>
    /// </summary>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="horizontal">false for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, null for <see cref="DefaultFindTimeout"/></param>
    /// <param name="includeScrollBarAreas">true to include the parents of scroll bar elements (areas without a ScrollPattern),
    ///     false for the elements with a ScrollPattern only</param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IntPtr windowHandle, bool horizontal, TimeSpan? timeout, bool includeScrollBarAreas)
        => FindScrollableAreas(windowHandle, horizontal, timeout, includeScrollBarAreas, TimeSpan.Zero);

    /// <summary>
    ///     List the areas of a window which can scroll in the direction, and wait for content which isn't there yet, see
    ///     <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?)"/>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Some applications (e.g. Chromium based browsers and Electron apps) build their accessibility tree only when a UI Automation client
    ///     asks for it, the first answer then has only the frame of the window, so the page has no scrollable area yet. When the search finds no
    ///     area, a shallow read of the control view (as <see cref="UiAutomationAreas.FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>
    ///     does) tells if the tree looks incomplete: an element without content (it reports no children which aren't offscreen, only children with
    ///     an empty rectangle, or children with its own rectangle which have no content themselves) covers at least a quarter of the window, or
    ///     the window's own element has no content and it is Chromium's render widget window (<c>Chrome_RenderWidgetHostHWND</c>, also in Electron
    ///     and WebView2) before its tree is built; other windows without children, like a button or a custom drawn control, are complete. Then the search is done again,
    ///     with a short pause (a quarter of a second) between the attempts, until an area is found, the tree is complete or
    ///     <paramref name="contentWait"/> has passed; the last result is returned. The search itself makes Chromium build the tree, so when the
    ///     tree looks complete after a search which found nothing, the search is done once more. A window which has nothing to scroll and a
    ///     complete tree never waits, an empty overlay with a sibling of the same bounds which has content doesn't count. A window with an element which really
    ///     is large and empty (and nothing to scroll) waits the whole <paramref name="contentWait"/> on every call: cache the result, or pass a
    ///     shorter one (or <see cref="TimeSpan.Zero"/>) for such windows.
    ///     </para>
    ///     <para>
    ///     All attempts use one UI Automation object. <paramref name="cancellationToken"/> is checked during every pause and before every search,
    ///     a single search can't be interrupted.
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="horizontal">false for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, null for <see cref="DefaultFindTimeout"/></param>
    /// <param name="includeScrollBarAreas">true to include the parents of scroll bar elements (areas without a ScrollPattern),
    ///     false for the elements with a ScrollPattern only</param>
    /// <param name="contentWait">TimeSpan, how long the search is done again while it finds nothing and the tree looks incomplete,
    ///     null for <see cref="UiAutomationAreas.DefaultContentWait"/>; <see cref="TimeSpan.Zero"/> searches once</param>
    /// <param name="cancellationToken">CancellationToken, checked during every pause and before every search</param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> was canceled</exception>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IntPtr windowHandle, bool horizontal, TimeSpan? timeout, bool includeScrollBarAreas,
        TimeSpan? contentWait, CancellationToken cancellationToken = default)
    {
        if (contentWait < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(contentWait), contentWait, "The content wait can't be negative.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (windowHandle == IntPtr.Zero)
        {
            return new List<NativeRect>();
        }
        var automation = CreateAutomation(timeout ?? DefaultFindTimeout);
        if (automation is null)
        {
            return new List<NativeRect>();
        }
        try
        {
            return ReadScrollableAreas(() => FindScrollableAreasOnce(automation, windowHandle, horizontal, includeScrollBarAreas),
                () => UiAutomationAreas.LooksIncomplete(automation, windowHandle, cancellationToken),
                contentWait ?? UiAutomationAreas.DefaultContentWait, UiAutomationAreas.ContentPause, windowHandle, cancellationToken);
        }
        finally
        {
            Release(automation);
        }
    }

    /// <summary>
    ///     Search, and search again while nothing is found and the tree looks incomplete, see
    ///     <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?, bool, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="find">searches the scrollable areas once</param>
    /// <param name="looksIncomplete">tells if the tree looks incomplete, only asked when the search found nothing; when it says complete the
    ///     search is done once more</param>
    /// <param name="contentWait">TimeSpan with the total time for the attempts, TimeSpan.Zero searches once</param>
    /// <param name="pause">TimeSpan between two attempts</param>
    /// <param name="windowHandle">IntPtr for the log</param>
    /// <param name="cancellationToken">CancellationToken, checked during every pause and before every search</param>
    internal static IReadOnlyList<NativeRect> ReadScrollableAreas(Func<IReadOnlyList<NativeRect>> find, Func<bool> looksIncomplete, TimeSpan contentWait,
        TimeSpan pause, IntPtr windowHandle, CancellationToken cancellationToken)
    {
        if (contentWait <= TimeSpan.Zero)
        {
            return find();
        }
        return UiAutomationAreas.ReadUntil(() =>
        {
            var areas = find();
            if (areas.Count > 0)
            {
                return (areas, false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (looksIncomplete())
            {
                return (areas, true);
            }
            // Complete now, but the search itself makes Chromium build the tree: it may have been completed during the search, after the
            // part with the page was searched. A search after the check is reliable.
            cancellationToken.ThrowIfCancellationRequested();
            return (find(), false);
        }, contentWait, pause, windowHandle, cancellationToken);
    }

    /// <summary>
    ///     One search for the scrollable areas, with the UI Automation object of the caller
    /// </summary>
    private static List<NativeRect> FindScrollableAreasOnce(IUIAutomation automation, IntPtr windowHandle, bool horizontal, bool includeScrollBarAreas)
    {
        var areas = new List<NativeRect>();
        IUIAutomationElement windowElement = null;
        IUIAutomationCondition scrollableCondition = null;
        IUIAutomationCondition scrollBarCondition = null;
        IUIAutomationCondition condition = null;
        IUIAutomationCacheRequest cacheRequest = null;
        IUIAutomationTreeWalker walker = null;
        IUIAutomationElementArray found = null;
        try
        {
            if (automation.ElementFromHandle(windowHandle, out windowElement) != UiaConstants.S_OK || windowElement is null)
            {
                return areas;
            }
            var propertyId = horizontal ? UiaConstants.HorizontallyScrollablePropertyId : UiaConstants.VerticallyScrollablePropertyId;
            if (automation.CreatePropertyCondition(propertyId, true, out scrollableCondition) != UiaConstants.S_OK || scrollableCondition is null
                || automation.CreateCacheRequest(out cacheRequest) != UiaConstants.S_OK || cacheRequest is null
                || cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.IsOffscreenPropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.OrientationPropertyId) != UiaConstants.S_OK)
            {
                return areas;
            }
            condition = scrollableCondition;
            if (includeScrollBarAreas)
            {
                // All scroll bars: the ones in the direction give the areas, the others are cut off those areas when at their edge.
                // The parent of a scroll bar is fetched from the found element, that needs a reference to the live element.
                if (automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.ScrollBarControlTypeId, out scrollBarCondition) != UiaConstants.S_OK
                    || scrollBarCondition is null
                    || automation.CreateOrCondition(scrollableCondition, scrollBarCondition, out condition) != UiaConstants.S_OK || condition is null
                    || automation.get_ControlViewWalker(out walker) != UiaConstants.S_OK || walker is null)
                {
                    // Fall back to the ScrollPattern elements only
                    if (!ReferenceEquals(condition, scrollableCondition))
                    {
                        Release(condition);
                    }
                    condition = scrollableCondition;
                    includeScrollBarAreas = false;
                }
            }
            // Without scroll bars only the cached properties are needed, not references to the live elements
            if (cacheRequest.put_AutomationElementMode(includeScrollBarAreas ? UiaConstants.AutomationElementModeFull : UiaConstants.AutomationElementModeNone) != UiaConstants.S_OK)
            {
                return areas;
            }
            var hResult = windowElement.FindAllBuildCache(UiaConstants.TreeScopeElement | UiaConstants.TreeScopeDescendants, condition, cacheRequest, out found);
            if (hResult != UiaConstants.S_OK || found is null || found.get_Length(out var length) != UiaConstants.S_OK)
            {
                if (hResult != UiaConstants.S_OK)
                {
                    Log.Verbose().WriteLine("Searching the scrollable areas of 0x{0:X} failed with 0x{1:X8}", windowHandle.ToInt64(), hResult);
                }
                return areas;
            }
            // In tree order: the ScrollPattern elements, and the parents of the scroll bars in the direction with their scroll bar
            var candidates = new List<(NativeRect Bounds, NativeRect ScrollBar)>();
            var patternBounds = new HashSet<NativeRect>();
            var otherScrollBars = new List<NativeRect>();
            var wantedOrientation = horizontal ? UiaConstants.OrientationHorizontal : UiaConstants.OrientationVertical;
            for (var index = 0; index < length; index++)
            {
                if (found.GetElement(index, out var element) != UiaConstants.S_OK || element is null)
                {
                    continue;
                }
                IUIAutomationElement parent = null;
                try
                {
                    if (!TryGetCachedVisibleBounds(element, out var bounds))
                    {
                        continue;
                    }
                    if (!includeScrollBarAreas || element.get_CachedControlType(out var controlType) != UiaConstants.S_OK || controlType != UiaConstants.ScrollBarControlTypeId)
                    {
                        candidates.Add((bounds, NativeRect.Empty));
                        patternBounds.Add(bounds);
                        continue;
                    }
                    if (element.get_CachedOrientation(out var orientation) != UiaConstants.S_OK || orientation != wantedOrientation)
                    {
                        otherScrollBars.Add(bounds);
                        continue;
                    }
                    // The control which scrolls is the parent of the scroll bar
                    if (walker.GetParentElementBuildCache(element, cacheRequest, out parent) == UiaConstants.S_OK && parent is not null
                        && TryGetCachedVisibleBounds(parent, out var parentBounds))
                    {
                        candidates.Add((parentBounds, bounds));
                    }
                }
                finally
                {
                    Release(parent);
                    Release(element);
                }
            }
            foreach (var (bounds, scrollBar) in candidates)
            {
                if (scrollBar.IsEmpty)
                {
                    AddArea(areas, bounds);
                }
                // A ScrollPattern element and the parent of its own scroll bar: the element as it is
                else if (!patternBounds.Contains(bounds))
                {
                    AddArea(areas, WithoutScrollBars(bounds, scrollBar, horizontal, otherScrollBars));
                }
            }
            return areas;
        }
        finally
        {
            Release(found);
            Release(walker);
            Release(cacheRequest);
            if (!ReferenceEquals(condition, scrollableCondition))
            {
                Release(condition);
            }
            Release(scrollBarCondition);
            Release(scrollableCondition);
            Release(windowElement);
        }
    }

    private static bool TryGetCachedVisibleBounds(IUIAutomationElement element, out NativeRect bounds)
    {
        bounds = NativeRect.Empty;
        if (element.get_CachedIsOffscreen(out var isOffscreen) == UiaConstants.S_OK && isOffscreen != 0)
        {
            return false;
        }
        return element.get_CachedBoundingRectangle(out bounds) == UiaConstants.S_OK && !bounds.IsEmpty;
    }

    /// <summary>
    ///     The bounds of a scroll bar's parent without the scroll bar when it is at an edge (right or left for a vertical one, bottom or
    ///     top for a horizontal one), and without a scroll bar of the other orientation at an edge of what is left
    /// </summary>
    private static NativeRect WithoutScrollBars(NativeRect parentBounds, NativeRect scrollBar, bool horizontal, IEnumerable<NativeRect> otherScrollBars)
    {
        var area = CutScrollBar(parentBounds, scrollBar, horizontal);
        foreach (var otherScrollBar in otherScrollBars)
        {
            var cut = CutScrollBar(area, otherScrollBar, !horizontal);
            if (cut != area)
            {
                return cut;
            }
        }
        return area;
    }

    /// <summary>
    ///     The area without the scroll bar when the scroll bar lies inside the area, along one of its edges and covers at least half of
    ///     that edge; else the area as it is
    /// </summary>
    private static NativeRect CutScrollBar(NativeRect area, NativeRect scrollBar, bool scrollBarIsHorizontal)
    {
        if (area.IsEmpty || scrollBar.IsEmpty
            || scrollBar.X < area.X - EdgeTolerance || scrollBar.Y < area.Y - EdgeTolerance
            || scrollBar.Right > area.Right + EdgeTolerance || scrollBar.Bottom > area.Bottom + EdgeTolerance)
        {
            return area;
        }
        NativeRect cut;
        if (scrollBarIsHorizontal)
        {
            if (scrollBar.Width * 2 < area.Width || scrollBar.Height * 2 >= area.Height)
            {
                return area;
            }
            if (area.Bottom - scrollBar.Bottom <= EdgeTolerance)
            {
                cut = new NativeRect(area.X, area.Y, area.Width, scrollBar.Y - area.Y);
            }
            else if (scrollBar.Y - area.Y <= EdgeTolerance)
            {
                cut = new NativeRect(area.X, scrollBar.Bottom, area.Width, area.Bottom - scrollBar.Bottom);
            }
            else
            {
                return area;
            }
        }
        else
        {
            if (scrollBar.Height * 2 < area.Height || scrollBar.Width * 2 >= area.Width)
            {
                return area;
            }
            if (area.Right - scrollBar.Right <= EdgeTolerance)
            {
                cut = new NativeRect(area.X, area.Y, scrollBar.X - area.X, area.Height);
            }
            else if (scrollBar.X - area.X <= EdgeTolerance)
            {
                cut = new NativeRect(scrollBar.Right, area.Y, area.Right - scrollBar.Right, area.Height);
            }
            else
            {
                return area;
            }
        }
        return cut.IsEmpty ? area : cut;
    }

    /// <summary>
    ///     Add an area once, before the first area inside it, so an outer area always comes first (a scroll bar comes after the
    ///     content of its parent in tree order)
    /// </summary>
    private static void AddArea(List<NativeRect> areas, NativeRect bounds)
    {
        if (areas.Contains(bounds))
        {
            return;
        }
        var index = areas.FindIndex(area => bounds.X <= area.X && bounds.Y <= area.Y && bounds.Right >= area.Right && bounds.Bottom >= area.Bottom);
        if (index < 0)
        {
            areas.Add(bounds);
        }
        else
        {
            areas.Insert(index, bounds);
        }
    }

    /// <summary>
    ///     List the scrollable areas of a window, see <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="horizontal">false (default) for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultFindTimeout"/></param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IInteropWindow window, bool horizontal = false, TimeSpan? timeout = null)
        => window is null ? Array.Empty<NativeRect>() : FindScrollableAreas(window.Handle, horizontal, timeout);

    /// <summary>
    ///     List the scrollable areas of a window, see <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?, bool)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="horizontal">false for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, null for <see cref="DefaultFindTimeout"/></param>
    /// <param name="includeScrollBarAreas">true to include the parents of scroll bar elements, false for the elements with a ScrollPattern only</param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IInteropWindow window, bool horizontal, TimeSpan? timeout, bool includeScrollBarAreas)
        => window is null ? Array.Empty<NativeRect>() : FindScrollableAreas(window.Handle, horizontal, timeout, includeScrollBarAreas);

    /// <summary>
    ///     List the scrollable areas of a window and wait for content which isn't there yet, see
    ///     <see cref="FindScrollableAreas(IntPtr, bool, TimeSpan?, bool, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="horizontal">false for vertically scrollable areas, true for horizontally scrollable areas</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, null for <see cref="DefaultFindTimeout"/></param>
    /// <param name="includeScrollBarAreas">true to include the parents of scroll bar elements, false for the elements with a ScrollPattern only</param>
    /// <param name="contentWait">TimeSpan, how long the search is done again while it finds nothing and the tree looks incomplete,
    ///     null for <see cref="UiAutomationAreas.DefaultContentWait"/>; <see cref="TimeSpan.Zero"/> searches once</param>
    /// <param name="cancellationToken">CancellationToken, checked during every pause and before every search</param>
    /// <returns>IReadOnlyList with the rectangles, empty (never null) when there are none or UI Automation is not available</returns>
    public static IReadOnlyList<NativeRect> FindScrollableAreas(IInteropWindow window, bool horizontal, TimeSpan? timeout, bool includeScrollBarAreas,
        TimeSpan? contentWait, CancellationToken cancellationToken = default)
        => window is null ? Array.Empty<NativeRect>() : FindScrollableAreas(window.Handle, horizontal, timeout, includeScrollBarAreas, contentWait, cancellationToken);

    /// <summary>
    ///     True when this scrolls horizontally, false for vertical scrolling
    /// </summary>
    public bool Horizontal { get; }

    /// <summary>
    ///     How <see cref="Next"/>, <see cref="Previous"/>, <see cref="Start"/> and <see cref="End"/> scroll, default is
    ///     <see cref="UiAutomationScrollModes.ScrollPattern"/>; always <see cref="UiAutomationScrollModes.MouseWheel"/> when
    ///     <see cref="IsScrollBarFallback"/> is true.
    /// </summary>
    /// <exception cref="InvalidOperationException">ScrollPattern was set while <see cref="IsScrollBarFallback"/> is true</exception>
    public UiAutomationScrollModes ScrollMode
    {
        get => _scrollMode;
        set
        {
            if (value != UiAutomationScrollModes.MouseWheel && IsScrollBarFallback)
            {
                throw new InvalidOperationException("The element has no ScrollPattern, it can only be scrolled with the mouse wheel.");
            }
            _scrollMode = value;
        }
    }

    /// <summary>
    ///     True when the element has no <c>ScrollPattern</c> but a child scroll bar element: it is scrolled with the mouse wheel
    ///     (<see cref="ScrollMode"/> is <see cref="UiAutomationScrollModes.MouseWheel"/>), the cursor moves to <see cref="WheelLocation"/>
    ///     (or the middle of <see cref="ViewportBounds"/>), so the area must be visible on the screen.
    ///     The position is read from the scroll bar: its RangeValue pattern (Value, Minimum, Maximum, LargeChange), else the position of
    ///     its thumb between its line buttons, see <see cref="IsPositionKnown"/>. A thumb tells the position to a pixel: on long content
    ///     <see cref="IsAtStart"/> / <see cref="IsAtEnd"/> can be true while up to a thumb pixel's worth of content is left (compare the
    ///     captured frames when that matters), and when one wheel notch moves the content less than a thumb pixel a step can't see it
    ///     move and returns false; <see cref="Start"/>, <see cref="End"/> and <see cref="Reset"/> to the start or end wheel a
    ///     little further to make up for it.
    /// </summary>
    public bool IsScrollBarFallback { get; private set; }

    /// <summary>
    ///     True when the scroll position can be read (always for a <c>ScrollPattern</c> element while it is available). False for a
    ///     <see cref="IsScrollBarFallback"/> scroller whose scroll bar reports neither a RangeValue nor a thumb: then
    ///     <see cref="ScrollPercent"/> is -1, <see cref="IsAtStart"/> / <see cref="IsAtEnd"/> are true only when the scroll bar's line
    ///     button in that direction is disabled and the other one is enabled (both disabled says nothing, e.g. WPF disables both while
    ///     the mouse isn't over the scroll bar), or when the element is gone, <see cref="Next"/> / <see cref="Previous"/> move one mouse
    ///     wheel notch and return true without knowing whether the content moved, and <see cref="Start"/>, <see cref="End"/> and
    ///     <see cref="Reset"/> return false. The caller has to detect the end itself, e.g. when the captured content stops changing.
    /// </summary>
    public bool IsPositionKnown => TryGetScrollPercent(out _);

    /// <summary>
    ///     False when the element is not available anymore (e.g. the page navigated or the window closed), see <see cref="Refresh"/>
    /// </summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    ///     The scroll percentage when this scroller was created (or refreshed), used by <see cref="Reset"/>; -1 when it was unknown
    /// </summary>
    public double InitialScrollPercent { get; private set; }

    /// <summary>
    ///     The tolerance in percentage points for <see cref="IsAtStart"/> and <see cref="IsAtEnd"/>, for applications which don't report
    ///     exactly 0 or 100 because of rounding. Default is 0.01: the percentage is relative to the whole scrollable range, so on a page
    ///     of 50 screens even 0.5 would already be a quarter of a screen.
    /// </summary>
    public double PercentTolerance { get; set; } = 0.01;

    /// <summary>
    ///     The part of a page that <see cref="Next"/> and <see cref="Previous"/> scroll, greater than 0 and at most 1.
    ///     The default 1.0 scrolls a full page; a scrolling capture uses e.g. 0.5 so consecutive frames overlap.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not greater than 0 and at most 1.</exception>
    public double StepFraction
    {
        get => _stepFraction;
        set
        {
            if (!(value > 0 && value <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The step fraction must be greater than 0 and at most 1.");
            }
            _stepFraction = value;
        }
    }

    /// <summary>
    ///     The maximum number of SmallIncrement scroll calls (when SetScrollPercent is not supported) or mouse wheel notches for one step.
    ///     <see cref="Start"/>, <see cref="End"/> and <see cref="Reset"/> are not limited by it: they continue as long as the position
    ///     moves, limited by the remaining percentage (twice what it needs at the speed of the first movement). Default is 100.
    /// </summary>
    public int MaxIncrementsPerStep { get; set; } = 100;

    /// <summary>
    ///     Where the mouse wheel input goes in <see cref="UiAutomationScrollModes.MouseWheel"/>, in screen coordinates.
    ///     Null (the default) uses the middle of <see cref="ViewportBounds"/>. The cursor moves there, see <see cref="RestoreCursorAfterWheel"/>.
    /// </summary>
    public NativePoint? WheelLocation { get; set; }

    /// <summary>
    ///     In <see cref="UiAutomationScrollModes.MouseWheel"/> the cursor moves to the wheel location. When this is true the cursor is moved
    ///     back to where it was after every wheel movement. Default is false.
    /// </summary>
    public bool RestoreCursorAfterWheel { get; set; }

    /// <summary>
    ///     The visible, scrolling area: the bounding rectangle of the element in screen coordinates (physical pixels for a per-monitor
    ///     DPI aware process), e.g. to crop the frames of a scrolling capture. Empty when the element is not available.
    ///     For a <see cref="IsScrollBarFallback"/> scroller the element's scroll bar (and one of the other orientation) is left out when
    ///     it is at an edge of the element, measured when the scroller was created or refreshed.
    /// </summary>
    public NativeRect ViewportBounds
    {
        get
        {
            var element = _element;
            if (element is null || !Check(element.get_CurrentBoundingRectangle(out var bounds)))
            {
                return NativeRect.Empty;
            }
            var scrollBar = _scrollBar;
            if (scrollBar is null || bounds.IsEmpty)
            {
                return bounds;
            }
            var withoutScrollBars = new NativeRect(bounds.X + scrollBar.LeftInset, bounds.Y + scrollBar.TopInset,
                bounds.Width - scrollBar.LeftInset - scrollBar.RightInset, bounds.Height - scrollBar.TopInset - scrollBar.BottomInset);
            return withoutScrollBars.IsEmpty ? bounds : withoutScrollBars;
        }
    }

    /// <summary>
    ///     The current scroll position in percent (0 to 100) in the scroll direction, -1 when it can't scroll or the element is not available
    /// </summary>
    public double ScrollPercent => TryGetScrollPercent(out var percent) ? percent : UiaConstants.NoScroll;

    /// <summary>
    ///     The visible part of the content in the scroll direction, between 0 and 1 (VerticalViewSize / 100), 0 when unknown.
    ///     E.g. 0.25 means a page shows a quarter of the content.
    /// </summary>
    public double VisibleFraction => TryGetViewSize(out var viewSize) ? viewSize / 100 : 0;

    /// <summary>
    ///     True when the content is at the start (0 percent, within <see cref="PercentTolerance"/>), when it can't scroll, or when the element is not available.
    ///     Without a known position see <see cref="IsPositionKnown"/>.
    /// </summary>
    public bool IsAtStart => IsAtLimit(false);

    /// <summary>
    ///     True when the content is at the end (100 percent, within <see cref="PercentTolerance"/>), when it can't scroll, or when the element is not available.
    ///     Without a known position see <see cref="IsPositionKnown"/>.
    /// </summary>
    public bool IsAtEnd => IsAtLimit(true);

    /// <summary>
    ///     Scroll to the start (0 percent). Uses SetScrollPercent, or a scroll bar's writable RangeValue (checking that the position read
    ///     back got there, and with one wheel notch towards the start that the content followed: some controls, like the Visual Studio
    ///     editor, only follow the Scroll events of their scroll bar), else large increments or, in <see cref="UiAutomationScrollModes.MouseWheel"/>, wheel input of several pages
    ///     at a time (independent of <see cref="StepFraction"/>) until the start is reached or the position stops moving. Without a known
    ///     position (<see cref="IsPositionKnown"/> false) this only returns true when <see cref="IsAtStart"/> already is.
    /// </summary>
    /// <returns>bool, true when the start was reached</returns>
    public bool Start() => ScrollTo(0, false);

    /// <summary>
    ///     Scroll to the end (100 percent), see <see cref="Start"/>
    /// </summary>
    /// <returns>bool, true when the end was reached</returns>
    public bool End() => ScrollTo(100, true);

    /// <summary>
    ///     Scroll forward (down or right) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked, false at the end, when the element is gone or when the position didn't change</returns>
    public bool Next() => Step(true, StepFraction);

    /// <summary>
    ///     Scroll back (up or left) by <see cref="StepFraction"/> of a page
    /// </summary>
    /// <returns>bool if this worked, false at the start, when the element is gone or when the position didn't change</returns>
    public bool Previous() => Step(false, StepFraction);

    /// <summary>
    ///     Scroll back to <see cref="InitialScrollPercent"/>
    /// </summary>
    /// <returns>bool if this worked</returns>
    public bool Reset()
    {
        if (InitialScrollPercent < 0 || !TryGetScrollPercent(out var before))
        {
            return false;
        }
        if (Math.Abs(before - InitialScrollPercent) <= PercentTolerance)
        {
            return true;
        }
        // Not for a scroll bar: setting its value may move only the scroll bar, not the content (see Start)
        if (_scrollPattern is not null && SetScrollPercent(InitialScrollPercent))
        {
            return WaitForPercentChange(before, out _);
        }
        // A scroll bar: wheel back
        return ScrollMode == UiAutomationScrollModes.MouseWheel && IsAvailable && WheelTo(InitialScrollPercent, before);
    }

    /// <summary>
    ///     Find the element again at the original point (or in the original window), e.g. after <see cref="IsAvailable"/> became false
    ///     because the page navigated. <see cref="InitialScrollPercent"/> and <see cref="IsScrollBarFallback"/> are taken from the new element.
    /// </summary>
    /// <returns>true when a scrollable element was found</returns>
    public bool Refresh()
    {
        ThrowIfDisposed();
        var found = _point.HasValue && _windowHandle == IntPtr.Zero
            ? FindAtPoint(_automation, _point.Value, Horizontal, null, IntPtr.Zero, true)
            : FindInWindow(_automation, _windowHandle, Horizontal);
        if (found is null)
        {
            return false;
        }
        ReleaseElement();
        _element = found._element;
        _scrollPattern = found._scrollPattern;
        _scrollBar = found._scrollBar;
        // The parts belong to this scroller now; found shares the automation object, so it isn't disposed
        found._element = null;
        found._scrollPattern = null;
        found._scrollBar = null;
        IsScrollBarFallback = found.IsScrollBarFallback;
        if (IsScrollBarFallback)
        {
            _scrollMode = UiAutomationScrollModes.MouseWheel;
        }
        IsAvailable = true;
        InitialScrollPercent = found.InitialScrollPercent;
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        ReleaseElement();
        Release(_automation);
    }

    // ── Scrolling ────────────────────────────────────────────────────────────

    private bool ScrollTo(double percent, bool toEnd)
    {
        ThrowIfDisposed();
        if (!TryGetScrollPercent(out var before))
        {
            // A scroll bar without a position: there is no way to know where the start or end is
            return IsScrollBarFallback && IsAvailable && (toEnd ? IsAtEnd : IsAtStart);
        }
        if (before < 0)
        {
            return false;
        }
        if (IsAtTarget(before, percent, toEnd) && !(ScrollMode == UiAutomationScrollModes.MouseWheel && _thumbPixelPercent > 0))
        {
            return true;
        }
        if (ScrollMode == UiAutomationScrollModes.MouseWheel)
        {
            // A scroll bar with a writable RangeValue: set it, and use it when the position read back got there
            if (_scrollPattern is null && SetScrollPercent(percent))
            {
                // The value read back is the scroll bar's own, some controls only follow its Scroll events (e.g. the Visual Studio
                // editor): then the scroll bar moved, the content didn't. A wheel notch towards the target makes the control put its
                // real position on the scroll bar again: at the target nothing moves.
                if (WaitForPercent(percent, toEnd, out var after) && ConfirmWithWheelNotch(percent, toEnd, out after))
                {
                    return true;
                }
                Log.Verbose().WriteLine("Setting the scroll bar to {0} percent didn't move the content (at {1} percent), using the mouse wheel.", percent, after);
                before = after;
            }
            return WheelTo(percent, before);
        }
        if (SetScrollPercent(percent))
        {
            return WaitForPercentChange(before, out var after) || Math.Abs(after - percent) <= PercentTolerance;
        }
        // SetScrollPercent not supported: large increments until the start / end, as long as the position moves. The limit comes
        // from the first increment: twice the increments the remaining percentage needs, plus some.
        var limit = MaxIncrementsPerStep;
        for (var step = 0; step < limit; step++)
        {
            if (!ScrollAmountAndWait(toEnd ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement) || !TryGetScrollPercent(out var current))
            {
                return IsAvailable && TryGetScrollPercent(out var last) && IsAtTarget(last, percent, toEnd);
            }
            if (IsAtTarget(current, percent, toEnd))
            {
                return true;
            }
            if (step == 0)
            {
                limit = IncrementLimit(Math.Abs(before - percent), Math.Abs(current - before), 1);
            }
        }
        return false;
    }

    /// <summary>
    ///     Within the tolerance of the target, or past it (the start or end can't be passed)
    /// </summary>
    private bool IsAtTarget(double current, double target, bool forward) =>
        Math.Abs(current - target) <= PercentTolerance || (forward ? current >= target : current <= target);

    /// <summary>
    ///     Twice the increments the remaining percentage needs at the measured speed, plus some, at least <see cref="MaxIncrementsPerStep"/>
    /// </summary>
    private int IncrementLimit(double remaining, double moved, int increments)
    {
        if (moved <= 0)
        {
            return MaxIncrementsPerStep;
        }
        var needed = remaining / (moved / increments);
        return (int)Math.Max(MaxIncrementsPerStep, Math.Min(int.MaxValue / 2.0, Math.Ceiling(needed * 2) + 10));
    }

    /// <summary>
    ///     After setting a scroll bar's value: one wheel notch towards the target (start or end). When the content is there nothing
    ///     moves; when only the scroll bar moved, the control scrolls a notch and its scroll bar shows the real position again.
    /// </summary>
    /// <returns>true when the position is still at the target after the notch</returns>
    private bool ConfirmWithWheelNotch(double target, bool forward, out double after)
    {
        after = target;
        if (!TryGetScrollPercent(out var current) || !TryGetWheelLocation(out var location) || !WheelNotch(forward, location))
        {
            return false;
        }
        if (!WaitForPercentChange(current, out after))
        {
            // Nothing moved: at the target
            after = current;
            return IsAvailable;
        }
        return IsAtTarget(after, target, forward);
    }

    /// <summary>
    ///     Poll until the position reached the target (it may arrive late)
    /// </summary>
    private bool WaitForPercent(double target, bool forward, out double after)
    {
        after = UiaConstants.NoScroll;
        for (var check = 0; check < PositionChangeChecks; check++)
        {
            if (!TryGetScrollPercent(out after))
            {
                return false;
            }
            if (IsAtTarget(after, target, forward))
            {
                return true;
            }
            Thread.Sleep(PositionChangeCheckInterval);
        }
        return false;
    }

    /// <summary>
    ///     Wheel to a position far away (Start, End, Reset), independent of <see cref="StepFraction"/>: several pages per wheel input once
    ///     the movement per notch is measured, until the target is reached or the position stops moving. The number of notches is limited
    ///     by the remaining percentage (twice what it needs at the measured speed).
    /// </summary>
    /// <returns>true when the target was reached</returns>
    private bool WheelTo(double target, double percent)
    {
        if (!TryGetWheelLocation(out var location))
        {
            return false;
        }
        var forward = target > percent;
        var pagePercent = TryGetViewSize(out var viewSize) && viewSize > 0 && viewSize < 100 ? viewSize / (100 - viewSize) * 100 : 0;
        double percentPerNotch = 0;
        // After an input which didn't move anything the next one has more notches
        var minimumNotches = 1;
        // How far one notch in its own input moved, and whether the application takes only one notch per input
        double singleNotchPercent = 0;
        var oneNotchPerInput = false;
        var sentNotches = 0;
        var notchLimit = int.MaxValue;
        while (true)
        {
            if (IsAtTarget(percent, target, forward))
            {
                WheelPastThumbPrecision(target, forward, location, percentPerNotch);
                return true;
            }
            var notches = 1;
            if (percentPerNotch > 0)
            {
                // Several pages, but not (much) more than what is left
                var remaining = Math.Abs(target - percent);
                var wanted = pagePercent > 0 ? Math.Min(remaining, pagePercent * PagesPerWheelInput) : remaining;
                notches = (int)Math.Max(1, Math.Min(MaxNotchesPerWheelInput, Math.Ceiling(wanted / percentPerNotch)));
            }
            notches = oneNotchPerInput ? 1 : Math.Max(notches, minimumNotches);
            if (sentNotches + notches > notchLimit)
            {
                Log.Verbose().WriteLine("The target {0} percent was not reached after {1} wheel notches, at {2} percent.", target, sentNotches, percent);
                return false;
            }
            if (!WheelNotches(forward, location, notches))
            {
                return false;
            }
            sentNotches += notches;
            if (!WaitForPercentChange(percent, out var after) || (forward ? after <= percent : after >= percent))
            {
                if (!IsAvailable)
                {
                    return false;
                }
                // Nothing moved: e.g. a thumb moves in whole pixels, so on long content a notch may not move it. Try more notches, at most
                // one input with the maximum, before deciding that the position doesn't change anymore.
                if (oneNotchPerInput || notches >= MaxNotchesPerWheelInput)
                {
                    Log.Verbose().WriteLine("The scroll position doesn't change anymore at {0} percent, the target was {1}.", percent, target);
                    return TryGetScrollPercent(out var last) && IsAtTarget(last, target, forward);
                }
                minimumNotches = Math.Min(MaxNotchesPerWheelInput, notches * 4);
                continue;
            }
            var moved = Math.Abs(after - percent);
            if (percentPerNotch <= 0)
            {
                notchLimit = sentNotches + IncrementLimit(Math.Abs(target - after), moved, notches);
            }
            if (notches == 1 && singleNotchPercent <= 0)
            {
                singleNotchPercent = moved;
            }
            else if (notches > 1 && singleNotchPercent > 0 && moved < singleNotchPercent * notches / 4)
            {
                // Several notches in one input moved about as far as one: the application scrolls one notch per input whatever the delta
                // is (a WPF ScrollViewer only looks at the sign, and applies queued wheel input over several layout passes). Continue with
                // one notch per input, the speed per notch is the one measured with a single notch.
                Log.Verbose().WriteLine("{0} wheel notches in one input moved {1} percent, one notch moved {2} percent: continuing with one notch per input.",
                    notches, moved, singleNotchPercent);
                oneNotchPerInput = true;
                percentPerNotch = singleNotchPercent;
                minimumNotches = 1;
                percent = after;
                continue;
            }
            // The latest measurement: some applications scroll smoothly and were still moving when the position was read
            percentPerNotch = moved / notches;
            minimumNotches = 1;
            percent = after;
        }
    }

    /// <summary>
    ///     A thumb tells the position to a pixel, on long content that is a lot: when the thumb says the start or end is reached, wheel
    ///     about two thumb pixels further, which stops at the start or end
    /// </summary>
    private void WheelPastThumbPrecision(double target, bool forward, NativePoint location, double percentPerNotch)
    {
        var pixelPercent = _thumbPixelPercent;
        if (pixelPercent <= 0 || (forward ? target < 100 : target > 0))
        {
            return;
        }
        var notches = percentPerNotch > 0
            ? (int)Math.Max(1, Math.Min(MaxNotchesPerWheelInput, Math.Ceiling(2 * pixelPercent / percentPerNotch)))
            : MaxNotchesPerWheelInput;
        if (WheelNotches(forward, location, notches))
        {
            // Let it arrive, the position can't be checked to less than a thumb pixel
            WaitForPercentChange(target, out _);
        }
    }

    private bool Step(bool forward, double stepFraction)
    {
        ThrowIfDisposed();
        if (!TryGetScrollPercent(out var percent))
        {
            return IsScrollBarFallback && IsAvailable && StepWithoutPosition(forward);
        }
        if (percent < 0)
        {
            return false;
        }
        if (forward ? percent >= 100 - PercentTolerance : percent <= PercentTolerance)
        {
            return false;
        }
        // The view size is the visible part of the content in percent, a page in scroll percent is the view size relative to the rest
        // of the content: scrolling from 0 to 100 percent moves the content by (100 - viewSize) percent of its size.
        var pagePercent = TryGetViewSize(out var viewSize) && viewSize > 0 && viewSize < 100
            ? viewSize / (100 - viewSize) * 100
            : 0;
        var target = pagePercent > 0
            ? Math.Max(0, Math.Min(100, percent + (forward ? 1 : -1) * pagePercent * stepFraction))
            : -1;

        if (ScrollMode == UiAutomationScrollModes.MouseWheel)
        {
            return WheelUntil(forward, percent, target);
        }

        if (target >= 0 && SetScrollPercent(target))
        {
            // Some frameworks (e.g. WPF) apply the position at their next layout pass: wait for it, so a capture right after this call
            // sees the new position and IsAtEnd is up to date. A control which accepts the call but doesn't move returns false, so a
            // "while (!IsAtEnd && Next())" loop ends; use UiAutomationScrollModes.MouseWheel for such a control.
            return WaitForPercentChange(percent, out _);
        }
        if (!IsAvailable)
        {
            return false;
        }

        // SetScrollPercent is not supported (or the view size is unknown): increments until the target is reached
        var amount = target < 0 || stepFraction >= 1
            ? forward ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement
            : forward ? ScrollAmount.SmallIncrement : ScrollAmount.SmallDecrement;
        for (var increment = 0; increment < MaxIncrementsPerStep; increment++)
        {
            if (!ScrollAmountAndWait(amount))
            {
                return increment > 0;
            }
            if (target < 0 || amount == ScrollAmount.LargeIncrement || amount == ScrollAmount.LargeDecrement)
            {
                return true;
            }
            if (!TryGetScrollPercent(out var current) || (forward ? current >= target : current <= target))
            {
                return true;
            }
        }
        return true;
    }

    /// <summary>
    ///     Wheel one notch at a time until the target percentage is reached (or a page when it's unknown), for controls which report
    ///     the ScrollPattern but don't move when it's used
    /// </summary>
    private bool WheelUntil(bool forward, double startPercent, double target)
    {
        if (!TryGetWheelLocation(out var location))
        {
            return false;
        }
        var moved = false;
        var before = startPercent;
        for (var notch = 0; notch < MaxIncrementsPerStep; notch++)
        {
            if (!WheelNotch(forward, location))
            {
                return moved;
            }
            if (!WaitForPercentChange(before, out var after))
            {
                Log.Verbose().WriteLine("Scroll position didn't change after a mouse wheel notch, stopping.");
                return moved;
            }
            moved = true;
            if (target < 0 || (forward ? after >= target : after <= target) || (forward ? after >= 100 - PercentTolerance : after <= PercentTolerance))
            {
                return true;
            }
            before = after;
        }
        return moved;
    }

    /// <summary>
    ///     A scroll bar which reports no position: one wheel notch per step, so a caller which watches the content can't overshoot.
    ///     True when the input was sent, whether the content moved is unknown.
    /// </summary>
    private bool StepWithoutPosition(bool forward)
    {
        if (forward ? IsAtEnd : IsAtStart)
        {
            return false;
        }
        // Also checks that the element is still there
        if (ViewportBounds.IsEmpty || !TryGetWheelLocation(out var location))
        {
            return false;
        }
        return WheelNotch(forward, location);
    }

    private bool TryGetWheelLocation(out NativePoint location)
    {
        if (WheelLocation.HasValue)
        {
            location = WheelLocation.Value;
            return true;
        }
        var viewport = ViewportBounds;
        location = viewport.IsEmpty ? default : CenterOf(viewport);
        return !viewport.IsEmpty;
    }

    private bool WheelNotch(bool forward, NativePoint location) => WheelNotches(forward, location, 1);

    private bool WheelNotches(bool forward, NativePoint location, int notches)
    {
        // Negative is down (towards the user) for the vertical wheel, positive is right for the horizontal wheel
        var delta = notches * (Horizontal == forward ? WindowScroller.WheelDeltaPerNotch : -WindowScroller.WheelDeltaPerNotch);
        return MouseInputGenerator.MoveMouseWheelAt(delta, location, RestoreCursorAfterWheel, Horizontal);
    }

    private bool ScrollAmountAndWait(ScrollAmount amount)
    {
        if (!TryGetScrollPercent(out var before))
        {
            return false;
        }
        var pattern = _scrollPattern;
        var hResult = Horizontal ? pattern.Scroll(amount, ScrollAmount.NoAmount) : pattern.Scroll(ScrollAmount.NoAmount, amount);
        if (!Check(hResult))
        {
            return false;
        }
        return WaitForPercentChange(before, out _);
    }

    private bool SetScrollPercent(double percent)
    {
        var pattern = _scrollPattern;
        if (pattern is null)
        {
            // A scroll bar with a writable RangeValue
            var rangeValue = _scrollBar?.RangeValue;
            if (rangeValue is null || !Check(rangeValue.get_CurrentIsReadOnly(out var isReadOnly)) || isReadOnly != 0
                || !TryGetRange(rangeValue, out var minimum, out var maximum, out _, out _) || maximum <= minimum)
            {
                return false;
            }
            return Check(rangeValue.SetValue(minimum + (maximum - minimum) * percent / 100));
        }
        var hResult = Horizontal ? pattern.SetScrollPercent(percent, UiaConstants.NoScroll) : pattern.SetScrollPercent(UiaConstants.NoScroll, percent);
        return Check(hResult);
    }

    /// <summary>
    ///     The position of most applications changes synchronously, input and some applications are asynchronous, so poll for a short time
    /// </summary>
    private bool WaitForPercentChange(double before, out double after)
    {
        for (var check = 0; check < PositionChangeChecks; check++)
        {
            if (!TryGetScrollPercent(out after))
            {
                return false;
            }
            if (Math.Abs(after - before) > 0.0001)
            {
                return true;
            }
            Thread.Sleep(PositionChangeCheckInterval);
        }
        after = before;
        return false;
    }

    // ── UI Automation calls ──────────────────────────────────────────────────

    private bool TryGetScrollPercent(out double percent)
    {
        percent = UiaConstants.NoScroll;
        var pattern = _scrollPattern;
        if (pattern is not null)
        {
            return Check(Horizontal ? pattern.get_CurrentHorizontalScrollPercent(out percent) : pattern.get_CurrentVerticalScrollPercent(out percent));
        }
        return TryGetScrollBarPosition(out percent, out _);
    }

    private bool TryGetViewSize(out double viewSize)
    {
        viewSize = 0;
        var pattern = _scrollPattern;
        if (pattern is not null)
        {
            return Check(Horizontal ? pattern.get_CurrentHorizontalViewSize(out viewSize) : pattern.get_CurrentVerticalViewSize(out viewSize));
        }
        return TryGetScrollBarPosition(out _, out viewSize) && viewSize > 0;
    }

    /// <summary>
    ///     The position of a scroll bar in percent (-1 when the content fits, it can't scroll) and the visible part in percent (0 when unknown):
    ///     from its RangeValue, else from the position of its thumb between the line buttons
    /// </summary>
    private bool TryGetScrollBarPosition(out double percent, out double viewSize)
    {
        percent = UiaConstants.NoScroll;
        viewSize = 0;
        _thumbPixelPercent = 0;
        var scrollBar = _scrollBar;
        if (scrollBar is null)
        {
            return false;
        }
        if (scrollBar.RangeValue is not null && TryGetRange(scrollBar.RangeValue, out var minimum, out var maximum, out var value, out var largeChange))
        {
            if (maximum <= minimum)
            {
                return true;
            }
            percent = Math.Max(0, Math.Min(100, (value - minimum) * 100 / (maximum - minimum)));
            // The content is the range plus a page, a page is the large change
            viewSize = largeChange > 0 ? largeChange * 100 / (maximum - minimum + largeChange) : 0;
            return true;
        }
        if (scrollBar.Thumb is null || !IsAvailable
            || !Check(scrollBar.ScrollBar.get_CurrentBoundingRectangle(out var scrollBarBounds))
            || !Check(scrollBar.Thumb.get_CurrentBoundingRectangle(out var thumbBounds)))
        {
            return false;
        }
        var trackStart = (Horizontal ? scrollBarBounds.X : scrollBarBounds.Y) + scrollBar.DecreaseLength;
        var trackEnd = (Horizontal ? scrollBarBounds.Right : scrollBarBounds.Bottom) - scrollBar.IncreaseLength;
        var thumbStart = Horizontal ? thumbBounds.X : thumbBounds.Y;
        var thumbEnd = Horizontal ? thumbBounds.Right : thumbBounds.Bottom;
        var trackLength = trackEnd - trackStart;
        var thumbLength = thumbEnd - thumbStart;
        if (trackLength <= 0 || thumbLength <= 0)
        {
            // No thumb visible: the content fits (or the scroll bar is collapsed), it can't scroll
            return thumbBounds.IsEmpty && !scrollBarBounds.IsEmpty;
        }
        if (thumbLength >= trackLength)
        {
            return true;
        }
        // Pixels: the thumb within a pixel of the track end is at the end
        percent = thumbStart - trackStart <= 1 ? 0
            : trackEnd - thumbEnd <= 1 ? 100
            : Math.Max(0, Math.Min(100, (thumbStart - trackStart) * 100.0 / (trackLength - thumbLength)));
        viewSize = thumbLength * 100.0 / trackLength;
        _thumbPixelPercent = 100.0 / (trackLength - thumbLength);
        return true;
    }

    private bool TryGetRange(IUIAutomationRangeValuePattern rangeValue, out double minimum, out double maximum, out double value, out double largeChange)
    {
        minimum = maximum = value = largeChange = 0;
        return Check(rangeValue.get_CurrentMinimum(out minimum)) && Check(rangeValue.get_CurrentMaximum(out maximum))
            && Check(rangeValue.get_CurrentValue(out value)) && Check(rangeValue.get_CurrentLargeChange(out largeChange));
    }

    private bool IsAtLimit(bool end)
    {
        if (TryGetScrollPercent(out var percent))
        {
            return percent < 0 || (end ? percent >= 100 - PercentTolerance : percent <= PercentTolerance);
        }
        if (!IsScrollBarFallback || !IsAvailable)
        {
            // Can't scroll, or the element is gone: loops end
            return true;
        }
        // No position: the line button of this end disabled while the other one is enabled means the content is at this end.
        // Both disabled says nothing: e.g. WPF's Aero2 theme disables both while the mouse isn't over the scroll bar.
        var scrollBar = _scrollBar;
        var button = end ? scrollBar?.IncreaseButton : scrollBar?.DecreaseButton;
        var otherButton = end ? scrollBar?.DecreaseButton : scrollBar?.IncreaseButton;
        if (button is null || otherButton is null
            || !Check(button.get_CurrentIsEnabled(out var isEnabled)) || !Check(otherButton.get_CurrentIsEnabled(out var otherIsEnabled)))
        {
            return !IsAvailable;
        }
        return isEnabled == 0 && otherIsEnabled != 0;
    }

    /// <summary>
    ///     Check an HRESULT, a gone element makes this scroller unavailable
    /// </summary>
    private bool Check(int hResult)
    {
        if (hResult == UiaConstants.S_OK)
        {
            return true;
        }
        if (UiaConstants.IsGone(hResult))
        {
            IsAvailable = false;
        }
        if (Log.IsVerboseEnabled())
        {
            Log.Verbose().WriteLine("UI Automation call failed with 0x{0:X8}", hResult);
        }
        return false;
    }

    /// <summary>
    ///     Create a UI Automation object with short timeouts (Windows 8+: CUIAutomation8 and IUIAutomation2), else the default one
    /// </summary>
    internal static IUIAutomation CreateAutomation(TimeSpan timeout)
    {
        try
        {
            var type = Type.GetTypeFromCLSID(UiaConstants.CUIAutomation8Clsid, false);
            if (type is not null && Activator.CreateInstance(type) is IUIAutomation automation)
            {
                if (automation is IUIAutomation2 automation2)
                {
                    var milliseconds = (uint)Math.Max(1, Math.Min(uint.MaxValue, timeout.TotalMilliseconds));
                    automation2.put_ConnectionTimeout(milliseconds);
                    automation2.put_TransactionTimeout(milliseconds);
                }
                return automation;
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or TypeLoadException)
        {
            // Before Windows 8: no CUIAutomation8, use the default object without timeouts
            Log.Verbose().WriteLine("CUIAutomation8 is not available: {0}", ex.Message);
        }
        return CreateAutomation();
    }

    private static IUIAutomation CreateAutomation()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(UiaConstants.CUIAutomationClsid, true);
            return (IUIAutomation)Activator.CreateInstance(type);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or TypeLoadException)
        {
            Log.Warn().WriteLine(ex, "UI Automation is not available.");
            return null;
        }
    }

    private static UiAutomationScroller FindAtPoint(IUIAutomation automation, NativePoint screenPoint, bool horizontal, NativePoint? rememberPoint, IntPtr rememberWindow,
        bool scrollBarFallback)
    {
        if (automation.ElementFromPoint(screenPoint, out var element) != UiaConstants.S_OK || element is null)
        {
            return null;
        }
        return FindScrollableAncestor(automation, element, horizontal, rememberPoint, rememberWindow, scrollBarFallback);
    }

    private static UiAutomationScroller FindInWindow(IUIAutomation automation, IntPtr windowHandle, bool horizontal)
    {
        if (automation.ElementFromHandle(windowHandle, out var windowElement) != UiaConstants.S_OK || windowElement is null)
        {
            return null;
        }
        try
        {
            // 1. The window itself
            var pattern = GetScrollPattern(windowElement, horizontal);
            if (pattern is not null)
            {
                var self = new UiAutomationScroller(automation, windowElement, pattern, horizontal, null, windowHandle);
                windowElement = null;
                return self;
            }

            // 2. The element under the middle of the window, or one of its ancestors
            var centerIsVisible = windowElement.get_CurrentBoundingRectangle(out var bounds) == UiaConstants.S_OK && !bounds.IsEmpty
                && NativeMethods.WindowFromPoint(CenterOf(bounds)) is var hwndAtCenter
                && (hwndAtCenter == windowHandle || NativeMethods.IsChild(windowHandle, hwndAtCenter));
            if (centerIsVisible)
            {
                var atCenter = FindAtPoint(automation, CenterOf(bounds), horizontal, null, windowHandle, false);
                if (atCenter is not null)
                {
                    return atCenter;
                }
            }

            // 3. The first descendant which can scroll in the direction (a tree search, this can take a while for large windows)
            var propertyId = horizontal ? UiaConstants.HorizontallyScrollablePropertyId : UiaConstants.VerticallyScrollablePropertyId;
            if (automation.CreatePropertyCondition(propertyId, true, out var condition) == UiaConstants.S_OK && condition is not null)
            {
                try
                {
                    if (windowElement.FindFirst(UiaConstants.TreeScopeDescendants, condition, out var descendant) == UiaConstants.S_OK && descendant is not null)
                    {
                        var descendantPattern = GetScrollPattern(descendant, horizontal);
                        if (descendantPattern is not null)
                        {
                            return new UiAutomationScroller(automation, descendant, descendantPattern, horizontal, null, windowHandle);
                        }
                        Release(descendant);
                    }
                }
                finally
                {
                    Release(condition);
                }
            }

            // 4. Nothing has a ScrollPattern: an element with a scroll bar, under the middle of the window, else the first one in the window
            if (centerIsVisible)
            {
                var atCenter = FindAtPoint(automation, CenterOf(bounds), horizontal, null, windowHandle, true);
                if (atCenter is not null)
                {
                    return atCenter;
                }
            }
            return FindFirstScrollBarArea(automation, windowElement, horizontal, windowHandle);
        }
        finally
        {
            Release(windowElement);
        }
    }

    /// <summary>
    ///     The parent of the first visible scroll bar element in the direction inside the window, as a mouse wheel scroller
    /// </summary>
    private static UiAutomationScroller FindFirstScrollBarArea(IUIAutomation automation, IUIAutomationElement windowElement, bool horizontal, IntPtr windowHandle)
    {
        IUIAutomationTreeWalker walker = null;
        IUIAutomationElement scrollBar = null;
        IUIAutomationElement parent = null;
        if (!CreateScrollBarCondition(automation, horizontal, true, out var condition))
        {
            return null;
        }
        try
        {
            if (windowElement.FindFirst(UiaConstants.TreeScopeDescendants, condition, out scrollBar) != UiaConstants.S_OK || scrollBar is null
                || scrollBar.get_CurrentBoundingRectangle(out var scrollBarBounds) != UiaConstants.S_OK || scrollBarBounds.IsEmpty
                || automation.get_ControlViewWalker(out walker) != UiaConstants.S_OK || walker is null
                || walker.GetParentElement(scrollBar, out parent) != UiaConstants.S_OK || parent is null)
            {
                return null;
            }
            var scroller = CreateScrollBarScroller(automation, parent, scrollBar, horizontal, null, windowHandle);
            parent = null;
            scrollBar = null;
            return scroller;
        }
        finally
        {
            Release(parent);
            Release(scrollBar);
            Release(walker);
            Release(condition);
        }
    }

    /// <summary>
    ///     Walk from the element up to the first element with a ScrollPattern which can scroll in the direction. When there is none and
    ///     scrollBarFallback is true, the first element on the way (the element itself included) which has a visible child scroll bar
    ///     element in the direction.
    /// </summary>
    private static UiAutomationScroller FindScrollableAncestor(IUIAutomation automation, IUIAutomationElement element, bool horizontal, NativePoint? rememberPoint, IntPtr rememberWindow,
        bool scrollBarFallback)
    {
        IUIAutomationTreeWalker walker = null;
        IUIAutomationCondition scrollBarCondition = null;
        IUIAutomationElement candidate = null;
        IUIAutomationElement candidateScrollBar = null;
        try
        {
            // The depth is limited, a misbehaving provider can't make this loop forever
            for (var depth = 0; depth < 64 && element is not null; depth++)
            {
                var pattern = GetScrollPattern(element, horizontal);
                if (pattern is not null)
                {
                    var scroller = new UiAutomationScroller(automation, element, pattern, horizontal, rememberPoint, rememberWindow);
                    element = null;
                    return scroller;
                }
                if (scrollBarFallback && candidate is null)
                {
                    // One search over the children per element, until the first element with a scroll bar is found
                    if (scrollBarCondition is null && !CreateScrollBarCondition(automation, horizontal, true, out scrollBarCondition))
                    {
                        scrollBarFallback = false;
                    }
                    else if (element.FindFirst(UiaConstants.TreeScopeChildren, scrollBarCondition, out var scrollBar) == UiaConstants.S_OK && scrollBar is not null)
                    {
                        if (scrollBar.get_CurrentBoundingRectangle(out var scrollBarBounds) == UiaConstants.S_OK && !scrollBarBounds.IsEmpty)
                        {
                            candidate = element;
                            candidateScrollBar = scrollBar;
                        }
                        else
                        {
                            Release(scrollBar);
                        }
                    }
                }
                if (walker is null && (automation.get_RawViewWalker(out walker) != UiaConstants.S_OK || walker is null))
                {
                    break;
                }
                // The parent of the root is null
                var hResult = walker.GetParentElement(element, out var parent);
                if (!ReferenceEquals(element, candidate))
                {
                    Release(element);
                }
                element = hResult == UiaConstants.S_OK ? parent : null;
            }
            if (candidate is null)
            {
                return null;
            }
            if (ReferenceEquals(element, candidate))
            {
                element = null;
            }
            var fallback = CreateScrollBarScroller(automation, candidate, candidateScrollBar, horizontal, rememberPoint, rememberWindow);
            candidate = null;
            candidateScrollBar = null;
            return fallback;
        }
        finally
        {
            if (!ReferenceEquals(element, candidate))
            {
                Release(element);
            }
            Release(candidate);
            Release(candidateScrollBar);
            Release(scrollBarCondition);
            Release(walker);
        }
    }

    /// <summary>
    ///     ControlType ScrollBar and the orientation of the direction, optionally not offscreen
    /// </summary>
    private static bool CreateScrollBarCondition(IUIAutomation automation, bool horizontal, bool onlyOnscreen, out IUIAutomationCondition condition)
    {
        condition = null;
        IUIAutomationCondition controlType = null;
        IUIAutomationCondition orientation = null;
        IUIAutomationCondition notOffscreen = null;
        IUIAutomationCondition scrollBar = null;
        try
        {
            if (automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.ScrollBarControlTypeId, out controlType) != UiaConstants.S_OK || controlType is null
                || automation.CreatePropertyCondition(UiaConstants.OrientationPropertyId, horizontal ? UiaConstants.OrientationHorizontal : UiaConstants.OrientationVertical, out orientation) != UiaConstants.S_OK || orientation is null
                || automation.CreateAndCondition(controlType, orientation, out scrollBar) != UiaConstants.S_OK || scrollBar is null)
            {
                return false;
            }
            if (!onlyOnscreen)
            {
                condition = scrollBar;
                scrollBar = null;
                return true;
            }
            if (automation.CreatePropertyCondition(UiaConstants.IsOffscreenPropertyId, false, out notOffscreen) != UiaConstants.S_OK || notOffscreen is null
                || automation.CreateAndCondition(scrollBar, notOffscreen, out condition) != UiaConstants.S_OK || condition is null)
            {
                condition = null;
                return false;
            }
            return true;
        }
        finally
        {
            Release(scrollBar);
            Release(notOffscreen);
            Release(orientation);
            Release(controlType);
        }
    }

    /// <summary>
    ///     A mouse wheel scroller for the element which owns the scroll bar; takes over both references
    /// </summary>
    private static UiAutomationScroller CreateScrollBarScroller(IUIAutomation automation, IUIAutomationElement element, IUIAutomationElement scrollBar, bool horizontal,
        NativePoint? rememberPoint, IntPtr rememberWindow)
    {
        var parts = new ScrollBarParts { ScrollBar = scrollBar };
        if (scrollBar.GetCurrentPattern(UiaConstants.RangeValuePatternId, out var patternObject) == UiaConstants.S_OK && patternObject is not null)
        {
            if (patternObject is IUIAutomationRangeValuePattern rangeValue)
            {
                parts.RangeValue = rangeValue;
            }
            else
            {
                Release(patternObject);
            }
        }
        FindScrollBarChildren(automation, parts, horizontal);
        MeasureScrollBarInsets(automation, element, parts, horizontal);
        return new UiAutomationScroller(automation, element, null, horizontal, rememberPoint, rememberWindow, parts);
    }

    /// <summary>
    ///     How much the scroll bar, and a visible scroll bar of the other orientation among the element's children, take at the edges of
    ///     the element: the bounds of both and one search
    /// </summary>
    private static void MeasureScrollBarInsets(IUIAutomation automation, IUIAutomationElement element, ScrollBarParts parts, bool horizontal)
    {
        if (element.get_CurrentBoundingRectangle(out var bounds) != UiaConstants.S_OK || bounds.IsEmpty
            || parts.ScrollBar.get_CurrentBoundingRectangle(out var scrollBarBounds) != UiaConstants.S_OK)
        {
            return;
        }
        var otherScrollBars = new List<NativeRect>();
        if (CreateScrollBarCondition(automation, !horizontal, true, out var condition))
        {
            try
            {
                if (element.FindFirst(UiaConstants.TreeScopeChildren, condition, out var other) == UiaConstants.S_OK && other is not null)
                {
                    if (other.get_CurrentBoundingRectangle(out var otherBounds) == UiaConstants.S_OK && !otherBounds.IsEmpty)
                    {
                        otherScrollBars.Add(otherBounds);
                    }
                    Release(other);
                }
            }
            finally
            {
                Release(condition);
            }
        }
        var area = WithoutScrollBars(bounds, scrollBarBounds, horizontal, otherScrollBars);
        parts.LeftInset = area.X - bounds.X;
        parts.TopInset = area.Y - bounds.Y;
        parts.RightInset = bounds.Right - area.Right;
        parts.BottomInset = bounds.Bottom - area.Bottom;
    }

    /// <summary>
    ///     The thumb and the line buttons at both ends of the scroll bar, with one search
    /// </summary>
    private static void FindScrollBarChildren(IUIAutomation automation, ScrollBarParts parts, bool horizontal)
    {
        IUIAutomationCondition buttons = null;
        IUIAutomationCondition thumbs = null;
        IUIAutomationCondition condition = null;
        IUIAutomationCacheRequest cacheRequest = null;
        IUIAutomationElementArray found = null;
        try
        {
            if (parts.ScrollBar.get_CurrentBoundingRectangle(out var scrollBarBounds) != UiaConstants.S_OK || scrollBarBounds.IsEmpty
                || automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.ButtonControlTypeId, out buttons) != UiaConstants.S_OK || buttons is null
                || automation.CreatePropertyCondition(UiaConstants.ControlTypePropertyId, UiaConstants.ThumbControlTypeId, out thumbs) != UiaConstants.S_OK || thumbs is null
                || automation.CreateOrCondition(buttons, thumbs, out condition) != UiaConstants.S_OK || condition is null
                || automation.CreateCacheRequest(out cacheRequest) != UiaConstants.S_OK || cacheRequest is null
                || cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) != UiaConstants.S_OK
                || parts.ScrollBar.FindAllBuildCache(UiaConstants.TreeScopeChildren, condition, cacheRequest, out found) != UiaConstants.S_OK || found is null
                || found.get_Length(out var length) != UiaConstants.S_OK)
            {
                return;
            }
            var scrollBarStart = horizontal ? scrollBarBounds.X : scrollBarBounds.Y;
            var scrollBarEnd = horizontal ? scrollBarBounds.Right : scrollBarBounds.Bottom;
            var scrollBarLength = scrollBarEnd - scrollBarStart;
            NativeRect decreaseBounds = default, increaseBounds = default;
            for (var index = 0; index < length; index++)
            {
                if (found.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                {
                    continue;
                }
                if (child.get_CachedControlType(out var controlType) != UiaConstants.S_OK || child.get_CachedBoundingRectangle(out var bounds) != UiaConstants.S_OK)
                {
                    Release(child);
                    continue;
                }
                if (controlType == UiaConstants.ThumbControlTypeId && parts.Thumb is null)
                {
                    parts.Thumb = child;
                    continue;
                }
                if (controlType == UiaConstants.ButtonControlTypeId && !bounds.IsEmpty)
                {
                    // The line buttons are at the ends: the one starting first decreases, the one ending last increases (page buttons are between them)
                    var start = horizontal ? bounds.X : bounds.Y;
                    var end = horizontal ? bounds.Right : bounds.Bottom;
                    if (parts.DecreaseButton is null || start < (horizontal ? decreaseBounds.X : decreaseBounds.Y))
                    {
                        if (!ReferenceEquals(parts.DecreaseButton, parts.IncreaseButton))
                        {
                            Release(parts.DecreaseButton);
                        }
                        parts.DecreaseButton = child;
                        decreaseBounds = bounds;
                    }
                    if (parts.IncreaseButton is null || end > (horizontal ? increaseBounds.Right : increaseBounds.Bottom))
                    {
                        if (!ReferenceEquals(parts.IncreaseButton, parts.DecreaseButton))
                        {
                            Release(parts.IncreaseButton);
                        }
                        parts.IncreaseButton = child;
                        increaseBounds = bounds;
                    }
                    if (ReferenceEquals(parts.DecreaseButton, child) || ReferenceEquals(parts.IncreaseButton, child))
                    {
                        continue;
                    }
                }
                Release(child);
            }
            if (ReferenceEquals(parts.DecreaseButton, parts.IncreaseButton))
            {
                // A single button can't be both ends
                var start = horizontal ? decreaseBounds.X : decreaseBounds.Y;
                if (start - scrollBarStart <= scrollBarEnd - (horizontal ? decreaseBounds.Right : decreaseBounds.Bottom))
                {
                    parts.IncreaseButton = null;
                }
                else
                {
                    parts.DecreaseButton = null;
                }
            }
            // The track of the thumb is between the line buttons, when they are at the ends of the scroll bar
            if (parts.DecreaseButton is not null)
            {
                var start = horizontal ? decreaseBounds.X : decreaseBounds.Y;
                var buttonLength = horizontal ? decreaseBounds.Width : decreaseBounds.Height;
                parts.DecreaseLength = start - scrollBarStart <= 2 && buttonLength < scrollBarLength / 3 ? buttonLength : 0;
            }
            if (parts.IncreaseButton is not null)
            {
                var end = horizontal ? increaseBounds.Right : increaseBounds.Bottom;
                var buttonLength = horizontal ? increaseBounds.Width : increaseBounds.Height;
                parts.IncreaseLength = scrollBarEnd - end <= 2 && buttonLength < scrollBarLength / 3 ? buttonLength : 0;
            }
        }
        finally
        {
            Release(found);
            Release(cacheRequest);
            Release(condition);
            Release(thumbs);
            Release(buttons);
        }
    }

    /// <summary>
    ///     The ScrollPattern of the element when it can scroll in the direction, else null
    /// </summary>
    private static IUIAutomationScrollPattern GetScrollPattern(IUIAutomationElement element, bool horizontal)
    {
        if (element.GetCurrentPattern(UiaConstants.ScrollPatternId, out var patternObject) != UiaConstants.S_OK || patternObject is null)
        {
            return null;
        }
        if (patternObject is not IUIAutomationScrollPattern pattern)
        {
            Release(patternObject);
            return null;
        }
        var hResult = horizontal ? pattern.get_CurrentHorizontallyScrollable(out var scrollable) : pattern.get_CurrentVerticallyScrollable(out scrollable);
        if (hResult == UiaConstants.S_OK && scrollable != 0)
        {
            return pattern;
        }
        Release(pattern);
        return null;
    }

    private static NativePoint CenterOf(NativeRect bounds) => new NativePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

    private void ReleaseElement()
    {
        var pattern = _scrollPattern;
        var element = _element;
        var scrollBar = _scrollBar;
        _scrollPattern = null;
        _element = null;
        _scrollBar = null;
        Release(pattern);
        Release(element);
        scrollBar?.Release();
    }

    /// <summary>
    ///     The scroll bar of an element without a ScrollPattern, with what tells its position
    /// </summary>
    private sealed class ScrollBarParts
    {
        public IUIAutomationElement ScrollBar;
        public IUIAutomationRangeValuePattern RangeValue;
        public IUIAutomationElement Thumb;
        public IUIAutomationElement DecreaseButton;
        public IUIAutomationElement IncreaseButton;

        /// <summary>The length of the line buttons in the scroll direction when they are at the ends of the scroll bar, else 0</summary>
        public int DecreaseLength;
        public int IncreaseLength;

        /// <summary>How much the scroll bars at the edges of the element take, see ViewportBounds</summary>
        public int LeftInset;
        public int TopInset;
        public int RightInset;
        public int BottomInset;

        public void Release()
        {
            UiAutomationScroller.Release(RangeValue);
            UiAutomationScroller.Release(Thumb);
            UiAutomationScroller.Release(DecreaseButton);
            UiAutomationScroller.Release(IncreaseButton);
            UiAutomationScroller.Release(ScrollBar);
            RangeValue = null;
            Thumb = DecreaseButton = IncreaseButton = ScrollBar = null;
        }
    }

    internal static void Release(object comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UiAutomationScroller));
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"UiAutomationScroller {{Horizontal: {Horizontal}; ScrollMode: {ScrollMode}; ScrollBarFallback: {IsScrollBarFallback}; Available: {IsAvailable}}}";
}
