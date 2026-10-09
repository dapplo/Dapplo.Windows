// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Automation.Interop;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Automation;

/// <summary>
///     Read the areas UI Automation knows inside a window (the parts of a browser page, an Electron, WPF or UWP app, an Office ribbon),
///     e.g. to snap a region selection to them where the window has no child windows. Works on a window handle, not on a point, so it also
///     works while another window (a full-screen selection window) covers the window.
/// </summary>
public static class UiAutomationAreas
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     The default for the timeout of <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The default for the content wait of <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>:
    ///     how long the tree is read again while it looks incomplete, 3 seconds
    /// </summary>
    public static readonly TimeSpan DefaultContentWait = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     The pause between two reads while the tree looks incomplete
    /// </summary>
    private static readonly TimeSpan ContentPause = TimeSpan.FromMilliseconds(250);

    /// <summary>
    ///     A guard against a provider which reports a (nearly) endless chain of elements with the same rectangle
    /// </summary>
    private const int MaxNesting = 256;

    /// <summary>
    ///     Read the element tree of a window (the control view) as <see cref="UiAutomationArea"/>s, the window's own element is the root.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The UI Automation calls run on a background (MTA) thread of the thread pool, the returned task completes with a snapshot.
    ///     The tree is read level by level: one request to the application (FindAllBuildCache with TreeScope_Children for the control view
    ///     children which aren't offscreen, and a cache for the bounds, control type, name and offscreen state) per area whose children are read,
    ///     so <paramref name="maxDepth"/> limits the work. A single request can't be interrupted: on a long web page the one for the children of
    ///     the document is by far the largest.
    ///     <paramref name="cancellationToken"/> is checked before every request.
    ///     </para>
    ///     <para>
    ///     The result is cleaned up for snapping: offscreen elements and elements with an empty rectangle are left out (with their children),
    ///     an element smaller than <paramref name="minimumSize"/> in width or height is left out with its children, every rectangle is clipped to
    ///     its parent's (web pages report elements outside the visible part), and an element with the same rectangle as its parent is replaced
    ///     by its children, so there are no chains of identical rectangles. <paramref name="maxDepth"/> counts the levels of the result, after
    ///     this merge: the children of a merged element are still read.
    ///     </para>
    ///     <para>
    ///     Some applications (e.g. Chromium based browsers) build their accessibility tree only when a UI Automation client asks for it, the
    ///     first answer then has only the frame of the window. When an element whose children were read has no content (it reports no children
    ///     which aren't offscreen, only children with an empty rectangle, or children with its own rectangle which have no content themselves)
    ///     and its area covers at least a quarter of the window, the tree looks incomplete: it is read again, with a short pause (a quarter of
    ///     a second) between the reads, until it is complete or <paramref name="contentWait"/> has passed (right after Edge started, Gmail took
    ///     longer than half a second); the last result is returned. An area whose children were all left out (smaller than
    ///     <paramref name="minimumSize"/>, clipped away) has content and doesn't count, neither does an empty overlay with a sibling of the same
    ///     bounds which has content, so a complete tree never waits. A window with an element which really is large and empty waits the whole
    ///     <paramref name="contentWait"/> on every call: pass a shorter one (or <see cref="TimeSpan.Zero"/>) for such windows.
    ///     </para>
    ///     <para>
    ///     Where IUIAutomation2 is available (Windows 8+) the connection and transaction timeouts of every request are set to
    ///     <paramref name="timeout"/>, so a hanging application can't block for the default 20 seconds.
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="maxDepth">int with the number of levels below the window, after merging elements with the same rectangle as their parent; 0 for the window only</param>
    /// <param name="minimumSize">int with the minimum width and height of an area in pixels, 0 (default) for all</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultTimeout"/></param>
    /// <param name="contentWait">TimeSpan, how long the tree is read again while it looks incomplete, default <see cref="DefaultContentWait"/>;
    ///     <see cref="TimeSpan.Zero"/> reads it only once</param>
    /// <param name="cancellationToken">CancellationToken, checked before every request to the application and during every pause</param>
    /// <returns>Task with the UiAutomationArea for the window, null when UI Automation isn't available or the window is gone</returns>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> was canceled</exception>
    public static Task<UiAutomationArea> FindAreasAsync(IntPtr windowHandle, int maxDepth = 3, int minimumSize = 0, TimeSpan? timeout = null,
        TimeSpan? contentWait = null, CancellationToken cancellationToken = default)
    {
        if (maxDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "The maximum depth can't be negative.");
        }
        if (contentWait < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(contentWait), contentWait, "The content wait can't be negative.");
        }
        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult<UiAutomationArea>(null);
        }
        // The thread pool threads are MTA, UI Automation must not run on the UI thread which owns the window
        return Task.Run(() => FindAreas(windowHandle, maxDepth, minimumSize, timeout ?? DefaultTimeout, contentWait ?? DefaultContentWait, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    ///     Read the element tree of a window, see <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="maxDepth">int with the number of levels below the window, after merging elements with the same rectangle as their parent</param>
    /// <param name="minimumSize">int with the minimum width and height of an area in pixels, 0 (default) for all</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultTimeout"/></param>
    /// <param name="contentWait">TimeSpan, how long the tree is read again while it looks incomplete, default <see cref="DefaultContentWait"/></param>
    /// <param name="cancellationToken">CancellationToken, checked before every request to the application and during every pause</param>
    /// <returns>Task with the UiAutomationArea for the window, null when UI Automation isn't available or the window is gone</returns>
    public static Task<UiAutomationArea> FindAreasAsync(IInteropWindow window, int maxDepth = 3, int minimumSize = 0, TimeSpan? timeout = null,
        TimeSpan? contentWait = null, CancellationToken cancellationToken = default)
        => window is null ? Task.FromResult<UiAutomationArea>(null) : FindAreasAsync(window.Handle, maxDepth, minimumSize, timeout, contentWait, cancellationToken);

    /// <summary>
    ///     Kept for binary compatibility, see <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<UiAutomationArea> FindAreasAsync(IntPtr windowHandle, int maxDepth, int minimumSize, TimeSpan? timeout, CancellationToken cancellationToken)
        => FindAreasAsync(windowHandle, maxDepth, minimumSize, timeout, null, cancellationToken);

    /// <summary>
    ///     Kept for binary compatibility, see <see cref="FindAreasAsync(IInteropWindow, int, int, TimeSpan?, TimeSpan?, CancellationToken)"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<UiAutomationArea> FindAreasAsync(IInteropWindow window, int maxDepth, int minimumSize, TimeSpan? timeout, CancellationToken cancellationToken)
        => FindAreasAsync(window, maxDepth, minimumSize, timeout, null, cancellationToken);

    private static UiAutomationArea FindAreas(IntPtr windowHandle, int maxDepth, int minimumSize, TimeSpan timeout, TimeSpan contentWait, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var automation = UiAutomationScroller.CreateAutomation(timeout);
        if (automation is null)
        {
            return null;
        }
        try
        {
            return ReadUntilComplete(() =>
            {
                var root = ReadOnce(automation, windowHandle, maxDepth, minimumSize, cancellationToken, out var readAgain);
                return (root, readAgain);
            }, contentWait, ContentPause, windowHandle, cancellationToken);
        }
        finally
        {
            UiAutomationScroller.Release(automation);
        }
    }

    /// <summary>
    ///     Read, and read again with a pause between the reads while the tree looks incomplete (the accessibility tree may just be built because
    ///     of the request), until it is complete or the content wait has passed. Returns the last result; a read which returns null (the window
    ///     is gone) ends the attempts with the previous result.
    /// </summary>
    /// <param name="read">reads the tree once, with whether it looks incomplete</param>
    /// <param name="contentWait">TimeSpan with the total time for the attempts, TimeSpan.Zero reads once</param>
    /// <param name="pause">TimeSpan between two reads</param>
    /// <param name="windowHandle">IntPtr for the log</param>
    /// <param name="cancellationToken">CancellationToken, checked during every pause and before every read</param>
    internal static UiAutomationArea ReadUntilComplete(Func<(UiAutomationArea Root, bool ReadAgain)> read, TimeSpan contentWait, TimeSpan pause, IntPtr windowHandle,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var (root, readAgain) = read();
        var attempt = 1;
        while (root != null && readAgain)
        {
            var remaining = contentWait - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                Log.Verbose().WriteLine("The tree of window 0x{0:X} still has a large area without content after {1} attempts in {2} ms, using it.",
                    windowHandle.ToInt64(), attempt, (long)stopwatch.Elapsed.TotalMilliseconds);
                break;
            }
            attempt++;
            Log.Verbose().WriteLine("The tree of window 0x{0:X} has a large area without content, reading it again (attempt {1}).", windowHandle.ToInt64(), attempt);
            if (cancellationToken.WaitHandle.WaitOne(remaining < pause ? remaining : pause))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            cancellationToken.ThrowIfCancellationRequested();
            var (next, nextReadAgain) = read();
            if (next == null)
            {
                break;
            }
            root = next;
            readAgain = nextReadAgain;
        }
        return root;
    }

    /// <summary>
    ///     Read the tree once, readAgain tells if it looks incomplete (see FindAreasAsync)
    /// </summary>
    private static UiAutomationArea ReadOnce(IUIAutomation automation, IntPtr windowHandle, int maxDepth, int minimumSize, CancellationToken cancellationToken, out bool readAgain)
    {
        var reader = new TreeReader(automation, maxDepth, minimumSize, cancellationToken);
        var root = reader.Read(windowHandle);
        readAgain = root != null && HasLargeEmptyArea(root, reader.EmptyAreas.Contains);
        return root;
    }

    /// <summary>
    ///     Read the tree of the window once and tell if FindAreasAsync would read it again, for the tests
    /// </summary>
    internal static bool NeedsSecondRead(IntPtr windowHandle, int maxDepth, int minimumSize)
    {
        var automation = UiAutomationScroller.CreateAutomation(DefaultTimeout);
        if (automation is null)
        {
            return false;
        }
        try
        {
            ReadOnce(automation, windowHandle, maxDepth, minimumSize, CancellationToken.None, out var readAgain);
            return readAgain;
        }
        finally
        {
            UiAutomationScroller.Release(automation);
        }
    }

    /// <summary>
    ///     True when an area without children (not the root) covers at least a quarter of the root, see
    ///     <see cref="HasLargeEmptyArea(UiAutomationArea, Func{UiAutomationArea, bool})"/>; every area without children counts as empty
    /// </summary>
    internal static bool HasLargeEmptyArea(UiAutomationArea root) => HasLargeEmptyArea(root, area => area.Children.Count == 0);

    /// <summary>
    ///     True when an empty area (not the root) covers at least a quarter of the root: content which may not be there yet.
    ///     An empty area with a sibling of the same bounds which has content is an overlay (Edge has one over the whole window) and doesn't count.
    /// </summary>
    /// <param name="root">UiAutomationArea</param>
    /// <param name="isEmpty">tells if an area really has no children (not just because they weren't read)</param>
    internal static bool HasLargeEmptyArea(UiAutomationArea root, Func<UiAutomationArea, bool> isEmpty)
    {
        var rootSize = (long)root.Bounds.Width * root.Bounds.Height;
        if (rootSize <= 0)
        {
            return false;
        }
        var pending = new Stack<UiAutomationArea>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var parent = pending.Pop();
            foreach (var area in parent.Children)
            {
                if (area.Children.Count > 0)
                {
                    pending.Push(area);
                    continue;
                }
                if (!isEmpty(area) || (long)area.Bounds.Width * area.Bounds.Height * 4 < rootSize)
                {
                    continue;
                }
                var isOverlay = false;
                foreach (var sibling in parent.Children)
                {
                    if (!ReferenceEquals(sibling, area) && sibling.Bounds == area.Bounds && sibling.Children.Count > 0)
                    {
                        isOverlay = true;
                        break;
                    }
                }
                if (!isOverlay)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    ///     Reads the tree level by level, one FindAllBuildCache per area whose children are read
    /// </summary>
    private sealed class TreeReader
    {
        private readonly IUIAutomation _automation;
        private readonly int _maxDepth;
        private readonly int _minimumSize;
        private readonly CancellationToken _cancellationToken;

        public TreeReader(IUIAutomation automation, int maxDepth, int minimumSize, CancellationToken cancellationToken)
        {
            _automation = automation;
            _maxDepth = maxDepth;
            _minimumSize = minimumSize;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        ///     The areas whose children were read and which have none
        /// </summary>
        public HashSet<UiAutomationArea> EmptyAreas { get; } = new HashSet<UiAutomationArea>();

        private IUIAutomationCacheRequest _cacheRequest;
        private IUIAutomationCondition _controlView;
        private IUIAutomationCondition _notOffscreen;
        private IUIAutomationCondition _visibleChildren;

        public UiAutomationArea Read(IntPtr windowHandle)
        {
            IUIAutomationElement rootElement = null;
            try
            {
                if (_automation.CreateCacheRequest(out _cacheRequest) != UiaConstants.S_OK || _cacheRequest is null
                    || _cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) != UiaConstants.S_OK
                    || _cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) != UiaConstants.S_OK
                    || _cacheRequest.AddProperty(UiaConstants.NamePropertyId) != UiaConstants.S_OK
                    || _cacheRequest.AddProperty(UiaConstants.IsOffscreenPropertyId) != UiaConstants.S_OK
                    || _automation.get_ControlViewCondition(out _controlView) != UiaConstants.S_OK || _controlView is null
                    || _cacheRequest.put_TreeFilter(_controlView) != UiaConstants.S_OK
                    // Offscreen elements are left out anyway: filtered in the application, a long web page has thousands of them
                    || _automation.CreatePropertyCondition(UiaConstants.IsOffscreenPropertyId, false, out _notOffscreen) != UiaConstants.S_OK || _notOffscreen is null
                    || _automation.CreateAndCondition(_controlView, _notOffscreen, out _visibleChildren) != UiaConstants.S_OK || _visibleChildren is null)
                {
                    return null;
                }
                _cancellationToken.ThrowIfCancellationRequested();
                var hResult = _automation.ElementFromHandleBuildCache(windowHandle, _cacheRequest, out rootElement);
                if (hResult != UiaConstants.S_OK || rootElement is null)
                {
                    Log.Verbose().WriteLine("Reading the UI Automation element of 0x{0:X} failed with 0x{1:X8}", windowHandle.ToInt64(), hResult);
                    return null;
                }
                rootElement.get_CachedBoundingRectangle(out var rootBounds);
                var children = new List<UiAutomationArea>();
                if (!rootBounds.IsEmpty)
                {
                    AddChildren(rootElement, rootBounds, 1, children, 0);
                }
                return new UiAutomationArea(rootBounds, GetControlType(rootElement), GetName(rootElement), children);
            }
            finally
            {
                UiAutomationScroller.Release(rootElement);
                UiAutomationScroller.Release(_visibleChildren);
                UiAutomationScroller.Release(_notOffscreen);
                UiAutomationScroller.Release(_controlView);
                UiAutomationScroller.Release(_cacheRequest);
                _visibleChildren = null;
                _notOffscreen = null;
                _controlView = null;
                _cacheRequest = null;
            }
        }

        /// <summary>
        ///     Read the control view children of the element and add their areas, clipped to the parent's bounds
        /// </summary>
        /// <param name="element">IUIAutomationElement whose children are read</param>
        /// <param name="parentBounds">NativeRect of the area the children belong to</param>
        /// <param name="childDepth">int with the level the children get in the result</param>
        /// <param name="areas">List to add the areas to</param>
        /// <param name="nesting">int with the number of merged elements above, a guard</param>
        /// <returns>true when the element has content: a child which was kept, or left out only because it is too small or clipped away
        ///     (also when the children weren't read because of a limit); false when it has no children, only children with an empty
        ///     rectangle (or merged children without content), or reading them failed</returns>
        private bool AddChildren(IUIAutomationElement element, NativeRect parentBounds, int childDepth, List<UiAutomationArea> areas, int nesting)
        {
            if (childDepth > _maxDepth || nesting > MaxNesting)
            {
                // Not read: unknown, which must not look like content which isn't there yet
                return true;
            }
            _cancellationToken.ThrowIfCancellationRequested();
            if (element.FindAllBuildCache(UiaConstants.TreeScopeChildren, _visibleChildren, _cacheRequest, out var children) != UiaConstants.S_OK || children is null)
            {
                return false;
            }
            try
            {
                if (children.get_Length(out var length) != UiaConstants.S_OK)
                {
                    return false;
                }
                var hasContent = false;
                for (var index = 0; index < length; index++)
                {
                    if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    try
                    {
                        hasContent |= AddArea(child, parentBounds, childDepth, areas, nesting);
                    }
                    finally
                    {
                        UiAutomationScroller.Release(child);
                    }
                }
                return hasContent;
            }
            finally
            {
                UiAutomationScroller.Release(children);
            }
        }

        /// <summary>
        ///     Add the area of the element (or, when it has the parent's rectangle, the areas of its children)
        /// </summary>
        /// <returns>true when the element is content, see AddChildren</returns>
        private bool AddArea(IUIAutomationElement element, NativeRect parentBounds, int depth, List<UiAutomationArea> areas, int nesting)
        {
            if (element.get_CachedIsOffscreen(out var isOffscreen) == UiaConstants.S_OK && isOffscreen != 0)
            {
                return false;
            }
            if (element.get_CachedBoundingRectangle(out var bounds) != UiaConstants.S_OK || bounds.IsEmpty)
            {
                return false;
            }
            // Web pages report elements outside the visible part
            bounds = bounds.Intersect(parentBounds);
            if (bounds.IsEmpty || bounds.Width < _minimumSize || bounds.Height < _minimumSize)
            {
                // Content, which is left out here
                return true;
            }
            if (bounds == parentBounds)
            {
                // Adds nothing: its children belong to the parent, on the same level; it is content when they are
                return AddChildren(element, parentBounds, depth, areas, nesting + 1);
            }
            var children = new List<UiAutomationArea>();
            var belowMaxDepth = depth < _maxDepth;
            var hasChildren = AddChildren(element, bounds, depth + 1, children, nesting);
            var area = new UiAutomationArea(bounds, GetControlType(element), GetName(element), children);
            // Content which isn't there yet: the element reports no children which aren't offscreen, only children with an empty rectangle,
            // or merged children (with its rectangle) without content; or reading them failed, the safe side. Children which were all left
            // out here (too small, clipped away) are content, the area doesn't count as empty.
            if (belowMaxDepth && !hasChildren)
            {
                EmptyAreas.Add(area);
            }
            areas.Add(area);
            return true;
        }
    }

    private static int GetControlType(IUIAutomationElement element) =>
        element.get_CachedControlType(out var controlType) == UiaConstants.S_OK ? controlType : 0;

    private static string GetName(IUIAutomationElement element) =>
        element.get_CachedName(out var name) == UiaConstants.S_OK ? name ?? string.Empty : string.Empty;
}
