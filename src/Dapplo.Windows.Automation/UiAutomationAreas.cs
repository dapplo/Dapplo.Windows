// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading;
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
    ///     The default for the timeout of <see cref="FindAreas(IntPtr, int, TimeSpan?)"/>: 2 seconds
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     How long to wait before the second attempt when the tree looks incomplete, see <see cref="FindAreas(IntPtr, int, TimeSpan?)"/>
    /// </summary>
    private static readonly TimeSpan SecondAttemptDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     A guard against a provider which reports a (nearly) endless tree
    /// </summary>
    private const int MaxDepth = 256;

    /// <summary>
    ///     Read the element tree of a window (the control view) as <see cref="UiAutomationArea"/>s, the window's own element is the root.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The whole tree is read with one request to the application (ElementFromHandleBuildCache with TreeScope_Subtree and a cache for
    ///     the bounds, control type, name and offscreen state), and then built from the cache without more calls.
    ///     </para>
    ///     <para>
    ///     The result is cleaned up for snapping: offscreen elements and elements with an empty rectangle are left out (with their children),
    ///     an element smaller than <paramref name="minimumSize"/> in width or height is left out with its children, every rectangle is clipped to
    ///     its parent's (web pages report elements outside the visible part), and an element with the same rectangle as its parent is replaced
    ///     by its children, so there are no chains of identical rectangles.
    ///     </para>
    ///     <para>
    ///     Some applications (e.g. Chromium based browsers) build their accessibility tree only when a UI Automation client asks for it, the
    ///     first answer then has only the frame of the window (measured with Edge: the first answer had 31 areas, half a second later 669).
    ///     When the result has an area without children which covers at least a quarter of the window (content which isn't there yet;
    ///     an empty overlay with a sibling of the same bounds which has content doesn't count), this waits half a second and reads the tree
    ///     once more, the second result is returned. A window with a large empty area pays this half second on every call.
    ///     </para>
    ///     <para>
    ///     This blocks, large trees (Word documents, long web pages) take a while: call it on a background (MTA) thread, never on the UI thread
    ///     which owns the window, and keep the result (it is a snapshot). Where IUIAutomation2 is available (Windows 8+) the connection and
    ///     transaction timeouts are set to <paramref name="timeout"/>, so a hanging application can't block the caller for the default 20 seconds.
    ///     </para>
    /// </remarks>
    /// <param name="windowHandle">IntPtr with the handle of the window</param>
    /// <param name="minimumSize">int with the minimum width and height of an area in pixels, 0 (default) for all</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultTimeout"/></param>
    /// <returns>UiAutomationArea for the window with the areas inside it, null when UI Automation isn't available or the window is gone</returns>
    public static UiAutomationArea FindAreas(IntPtr windowHandle, int minimumSize = 0, TimeSpan? timeout = null)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }
        var automation = UiAutomationScroller.CreateAutomation(timeout ?? DefaultTimeout);
        if (automation is null)
        {
            return null;
        }
        try
        {
            var root = ReadTree(automation, windowHandle, minimumSize);
            if (root != null && HasLargeEmptyArea(root))
            {
                // The accessibility tree may just have been built because of this request, read it again
                Log.Verbose().WriteLine("The tree of window 0x{0:X} has a large area without content, reading it again.", windowHandle.ToInt64());
                Thread.Sleep(SecondAttemptDelay);
                root = ReadTree(automation, windowHandle, minimumSize) ?? root;
            }
            return root;
        }
        finally
        {
            UiAutomationScroller.Release(automation);
        }
    }

    /// <summary>
    ///     Read the element tree of a window, see <see cref="FindAreas(IntPtr, int, TimeSpan?)"/>
    /// </summary>
    /// <param name="window">IInteropWindow</param>
    /// <param name="minimumSize">int with the minimum width and height of an area in pixels, 0 (default) for all</param>
    /// <param name="timeout">TimeSpan for the UI Automation timeouts, default <see cref="DefaultTimeout"/></param>
    /// <returns>UiAutomationArea for the window, null when UI Automation isn't available or the window is gone</returns>
    public static UiAutomationArea FindAreas(IInteropWindow window, int minimumSize = 0, TimeSpan? timeout = null)
        => window is null ? null : FindAreas(window.Handle, minimumSize, timeout);

    /// <summary>
    ///     True when an area without children (not the root) covers at least a quarter of the root: content which may not be there yet.
    ///     An empty area with a sibling of the same bounds which has content is an overlay (Edge has one over the whole window) and doesn't count.
    /// </summary>
    internal static bool HasLargeEmptyArea(UiAutomationArea root)
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
                if ((long)area.Bounds.Width * area.Bounds.Height * 4 < rootSize)
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

    private static UiAutomationArea ReadTree(IUIAutomation automation, IntPtr windowHandle, int minimumSize)
    {
        IUIAutomationCacheRequest cacheRequest = null;
        IUIAutomationCondition controlView = null;
        IUIAutomationElement rootElement = null;
        try
        {
            if (automation.CreateCacheRequest(out cacheRequest) != UiaConstants.S_OK || cacheRequest is null
                || cacheRequest.AddProperty(UiaConstants.BoundingRectanglePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.ControlTypePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.NamePropertyId) != UiaConstants.S_OK
                || cacheRequest.AddProperty(UiaConstants.IsOffscreenPropertyId) != UiaConstants.S_OK
                || cacheRequest.put_TreeScope(UiaConstants.TreeScopeSubtree) != UiaConstants.S_OK
                || automation.get_ControlViewCondition(out controlView) != UiaConstants.S_OK || controlView is null
                || cacheRequest.put_TreeFilter(controlView) != UiaConstants.S_OK
                // Only the cached properties and the cached tree are needed, no references to the live elements
                || cacheRequest.put_AutomationElementMode(UiaConstants.AutomationElementModeNone) != UiaConstants.S_OK)
            {
                return null;
            }
            var hResult = automation.ElementFromHandleBuildCache(windowHandle, cacheRequest, out rootElement);
            if (hResult != UiaConstants.S_OK || rootElement is null)
            {
                Log.Verbose().WriteLine("Reading the UI Automation tree of 0x{0:X} failed with 0x{1:X8}", windowHandle.ToInt64(), hResult);
                return null;
            }
            rootElement.get_CachedBoundingRectangle(out var rootBounds);
            var children = new List<UiAutomationArea>();
            if (!rootBounds.IsEmpty)
            {
                AddChildren(rootElement, rootBounds, minimumSize, children, 0);
            }
            return new UiAutomationArea(rootBounds, GetControlType(rootElement), GetName(rootElement), children);
        }
        finally
        {
            UiAutomationScroller.Release(rootElement);
            UiAutomationScroller.Release(controlView);
            UiAutomationScroller.Release(cacheRequest);
        }
    }

    /// <summary>
    ///     Add the areas of the cached children of the element to the list, clipped to the parent's bounds
    /// </summary>
    private static void AddChildren(IUIAutomationElement element, NativeRect parentBounds, int minimumSize, List<UiAutomationArea> areas, int depth)
    {
        if (depth >= MaxDepth || element.GetCachedChildren(out var children) != UiaConstants.S_OK || children is null)
        {
            return;
        }
        try
        {
            if (children.get_Length(out var length) != UiaConstants.S_OK)
            {
                return;
            }
            for (var index = 0; index < length; index++)
            {
                if (children.GetElement(index, out var child) != UiaConstants.S_OK || child is null)
                {
                    continue;
                }
                try
                {
                    AddArea(child, parentBounds, minimumSize, areas, depth);
                }
                finally
                {
                    UiAutomationScroller.Release(child);
                }
            }
        }
        finally
        {
            UiAutomationScroller.Release(children);
        }
    }

    private static void AddArea(IUIAutomationElement element, NativeRect parentBounds, int minimumSize, List<UiAutomationArea> areas, int depth)
    {
        var controlType = GetControlType(element);
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
        if (bounds.IsEmpty || bounds.Width < minimumSize || bounds.Height < minimumSize)
        {
            return;
        }
        if (bounds == parentBounds)
        {
            // Adds nothing: its children belong to the parent
            AddChildren(element, parentBounds, minimumSize, areas, depth + 1);
            return;
        }
        var children = new List<UiAutomationArea>();
        AddChildren(element, bounds, minimumSize, children, depth + 1);
        areas.Add(new UiAutomationArea(bounds, controlType, GetName(element), children));
    }

    private static int GetControlType(IUIAutomationElement element) =>
        element.get_CachedControlType(out var controlType) == UiaConstants.S_OK ? controlType : 0;

    private static string GetName(IUIAutomationElement element) =>
        element.get_CachedName(out var name) == UiaConstants.S_OK ? name ?? string.Empty : string.Empty;
}
