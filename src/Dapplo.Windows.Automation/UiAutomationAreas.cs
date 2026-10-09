// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
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
    ///     The default for the timeout of <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, CancellationToken)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     How long to wait before the second attempt when the tree looks incomplete
    /// </summary>
    private static readonly TimeSpan SecondAttemptDelay = TimeSpan.FromMilliseconds(500);

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
    ///     first answer then has only the frame of the window. When an area whose children were read has none visible and covers at least a quarter of
    ///     the window (an empty overlay with a sibling of the same bounds which has content doesn't count), this waits half a second and reads
    ///     the tree once more, the second result is returned. A window with a large empty area pays this half second on every call.
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
    /// <param name="cancellationToken">CancellationToken, checked before every request to the application</param>
    /// <returns>Task with the UiAutomationArea for the window, null when UI Automation isn't available or the window is gone</returns>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> was canceled</exception>
    public static Task<UiAutomationArea> FindAreasAsync(IntPtr windowHandle, int maxDepth = 3, int minimumSize = 0, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (maxDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "The maximum depth can't be negative.");
        }
        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult<UiAutomationArea>(null);
        }
        // The thread pool threads are MTA, UI Automation must not run on the UI thread which owns the window
        return Task.Run(() => FindAreas(windowHandle, maxDepth, minimumSize, timeout ?? DefaultTimeout, cancellationToken), cancellationToken);
    }

    /// <summary>
    ///     Read the element tree of a window, see <see cref="FindAreasAsync(IntPtr, int, int, TimeSpan?, CancellationToken)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="maxDepth">int with the number of levels below the window, after merging elements with the same rectangle as their parent</param>
    /// <param name="minimumSize">int with the minimum width and height of an area in pixels, 0 (default) for all</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultTimeout"/></param>
    /// <param name="cancellationToken">CancellationToken, checked before every request to the application</param>
    /// <returns>Task with the UiAutomationArea for the window, null when UI Automation isn't available or the window is gone</returns>
    public static Task<UiAutomationArea> FindAreasAsync(IInteropWindow window, int maxDepth = 3, int minimumSize = 0, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => window is null ? Task.FromResult<UiAutomationArea>(null) : FindAreasAsync(window.Handle, maxDepth, minimumSize, timeout, cancellationToken);

    private static UiAutomationArea FindAreas(IntPtr windowHandle, int maxDepth, int minimumSize, TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var automation = UiAutomationScroller.CreateAutomation(timeout);
        if (automation is null)
        {
            return null;
        }
        try
        {
            var reader = new TreeReader(automation, maxDepth, minimumSize, cancellationToken);
            var root = reader.Read(windowHandle);
            if (root != null && HasLargeEmptyArea(root, reader.EmptyAreas.Contains))
            {
                // The accessibility tree may just have been built because of this request, read it again
                Log.Verbose().WriteLine("The tree of window 0x{0:X} has a large area without content, reading it again.", windowHandle.ToInt64());
                if (cancellationToken.WaitHandle.WaitOne(SecondAttemptDelay))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                root = new TreeReader(automation, maxDepth, minimumSize, cancellationToken).Read(windowHandle) ?? root;
            }
            return root;
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
        /// <returns>true when the element has children (whether or not they were kept)</returns>
        private bool AddChildren(IUIAutomationElement element, NativeRect parentBounds, int childDepth, List<UiAutomationArea> areas, int nesting)
        {
            if (childDepth > _maxDepth || nesting > MaxNesting)
            {
                return false;
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
                for (var index = 0; index < length; index++)
                {
                    if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                    {
                        continue;
                    }
                    try
                    {
                        AddArea(child, parentBounds, childDepth, areas, nesting);
                    }
                    finally
                    {
                        UiAutomationScroller.Release(child);
                    }
                }
                return length > 0;
            }
            finally
            {
                UiAutomationScroller.Release(children);
            }
        }

        private void AddArea(IUIAutomationElement element, NativeRect parentBounds, int depth, List<UiAutomationArea> areas, int nesting)
        {
            if (element.get_CachedIsOffscreen(out var isOffscreen) == UiaConstants.S_OK && isOffscreen != 0)
            {
                return;
            }
            if (element.get_CachedBoundingRectangle(out var bounds) != UiaConstants.S_OK || bounds.IsEmpty)
            {
                return;
            }
            // Web pages report elements outside the visible part
            bounds = bounds.Intersect(parentBounds);
            if (bounds.IsEmpty || bounds.Width < _minimumSize || bounds.Height < _minimumSize)
            {
                return;
            }
            if (bounds == parentBounds)
            {
                // Adds nothing: its children belong to the parent, on the same level
                AddChildren(element, parentBounds, depth, areas, nesting + 1);
                return;
            }
            var children = new List<UiAutomationArea>();
            var expanded = depth < _maxDepth;
            AddChildren(element, bounds, depth + 1, children, nesting);
            var area = new UiAutomationArea(bounds, GetControlType(element), GetName(element), children);
            // Read, and nothing visible inside (no children, or only offscreen / empty ones)
            if (expanded && children.Count == 0)
            {
                EmptyAreas.Add(area);
            }
            areas.Add(area);
        }
    }

    private static int GetControlType(IUIAutomationElement element) =>
        element.get_CachedControlType(out var controlType) == UiaConstants.S_OK ? controlType : 0;

    private static string GetName(IUIAutomationElement element) =>
        element.get_CachedName(out var name) == UiaConstants.S_OK ? name ?? string.Empty : string.Empty;
}
