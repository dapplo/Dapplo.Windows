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

    private readonly IUIAutomation _automation;
    private readonly NativePoint? _point;
    private readonly IntPtr _windowHandle;
    private IUIAutomationElement _element;
    private IUIAutomationScrollPattern _scrollPattern;
    private ScrollBarParts _scrollBar;
    private UiAutomationScrollModes _scrollMode = UiAutomationScrollModes.ScrollPattern;
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
    ///     includes the scroll bar.
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
    {
        var areas = new List<NativeRect>();
        if (windowHandle == IntPtr.Zero)
        {
            return areas;
        }
        var automation = CreateAutomation(timeout ?? DefaultFindTimeout);
        if (automation is null)
        {
            return areas;
        }
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
                || cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) != UiaConstants.S_OK)
            {
                return areas;
            }
            condition = scrollableCondition;
            if (includeScrollBarAreas)
            {
                // The parent of a scroll bar is fetched from the found element, that needs a reference to the live element
                if (!CreateScrollBarCondition(automation, horizontal, false, out scrollBarCondition)
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
                    if (includeScrollBarAreas && element.get_CachedControlType(out var controlType) == UiaConstants.S_OK && controlType == UiaConstants.ScrollBarControlTypeId)
                    {
                        // The control which scrolls is the parent of the scroll bar
                        if (walker.GetParentElementBuildCache(element, cacheRequest, out parent) != UiaConstants.S_OK || parent is null
                            || !TryGetCachedVisibleBounds(parent, out bounds))
                        {
                            continue;
                        }
                    }
                    AddArea(areas, bounds);
                }
                finally
                {
                    Release(parent);
                    Release(element);
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
            Release(automation);
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
    ///     its thumb between its line buttons, see <see cref="IsPositionKnown"/>.
    /// </summary>
    public bool IsScrollBarFallback { get; private set; }

    /// <summary>
    ///     True when the scroll position can be read (always for a <c>ScrollPattern</c> element while it is available). False for a
    ///     <see cref="IsScrollBarFallback"/> scroller whose scroll bar reports neither a RangeValue nor a thumb: then
    ///     <see cref="ScrollPercent"/> is -1, <see cref="IsAtStart"/> / <see cref="IsAtEnd"/> are true only when the scroll bar's line
    ///     button in that direction is disabled (or the element is gone), <see cref="Next"/> / <see cref="Previous"/> move one mouse
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
    ///     The maximum number of SmallIncrement scroll calls (when SetScrollPercent is not supported) or mouse wheel notches for one step,
    ///     and the maximum number of steps <see cref="Start"/> and <see cref="End"/> take in <see cref="UiAutomationScrollModes.MouseWheel"/>.
    ///     Default is 100.
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
            return bounds;
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
    ///     Scroll to the start (0 percent)
    /// </summary>
    /// <returns>bool if this worked</returns>
    public bool Start() => ScrollTo(0, false);

    /// <summary>
    ///     Scroll to the end (100 percent)
    /// </summary>
    /// <returns>bool if this worked</returns>
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
        if (SetScrollPercent(InitialScrollPercent))
        {
            return WaitForPercentChange(before, out _);
        }
        // A scroll bar without a writable RangeValue: wheel back
        return ScrollMode == UiAutomationScrollModes.MouseWheel && IsAvailable && WheelUntil(InitialScrollPercent > before, before, InitialScrollPercent);
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
        if (ScrollMode == UiAutomationScrollModes.MouseWheel)
        {
            if (_scrollPattern is null)
            {
                // A scroll bar: set its RangeValue when it's writable, without a position there is no way to know where the start or end is
                if (!TryGetScrollPercent(out var current))
                {
                    return toEnd ? IsAtEnd && IsAvailable : IsAtStart && IsAvailable;
                }
                if (current < 0)
                {
                    return false;
                }
                if (Math.Abs(current - percent) <= PercentTolerance)
                {
                    return true;
                }
                if (SetScrollPercent(percent))
                {
                    return WaitForPercentChange(current, out var after) || Math.Abs(after - percent) <= PercentTolerance;
                }
            }
            for (var step = 0; step < MaxIncrementsPerStep; step++)
            {
                if (toEnd ? IsAtEnd : IsAtStart)
                {
                    return IsAvailable;
                }
                if (!Step(toEnd, 1.0))
                {
                    return false;
                }
            }
            return false;
        }
        if (!TryGetScrollPercent(out var before) || before < 0)
        {
            return false;
        }
        if (Math.Abs(before - percent) <= PercentTolerance)
        {
            return true;
        }
        if (SetScrollPercent(percent))
        {
            return WaitForPercentChange(before, out var after) || Math.Abs(after - percent) <= PercentTolerance;
        }
        // SetScrollPercent not supported: large increments until the end
        for (var step = 0; step < MaxIncrementsPerStep; step++)
        {
            if (toEnd ? IsAtEnd : IsAtStart)
            {
                return IsAvailable;
            }
            if (!ScrollAmountAndWait(toEnd ? ScrollAmount.LargeIncrement : ScrollAmount.LargeDecrement))
            {
                return false;
            }
        }
        return false;
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

    private bool WheelNotch(bool forward, NativePoint location)
    {
        // Negative is down (towards the user) for the vertical wheel, positive is right for the horizontal wheel
        var delta = Horizontal == forward ? WindowScroller.WheelDeltaPerNotch : -WindowScroller.WheelDeltaPerNotch;
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
        // No position: a disabled line button means the content is at that end, an enabled one (or none) says nothing
        var button = end ? _scrollBar?.IncreaseButton : _scrollBar?.DecreaseButton;
        if (button is not null && Check(button.get_CurrentIsEnabled(out var isEnabled)))
        {
            return isEnabled == 0;
        }
        return !IsAvailable;
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
    private static IUIAutomation CreateAutomation(TimeSpan timeout)
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
        return new UiAutomationScroller(automation, element, null, horizontal, rememberPoint, rememberWindow, parts);
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

    private static void Release(object comObject)
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
