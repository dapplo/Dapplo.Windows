// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using Dapplo.Windows.App;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Enums;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using System.Drawing.Imaging;
using System.Linq;
using Dapplo.Log;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Extensions for the interopWindow, all get or set commands update the value in the InteropWindow that is used.
/// </summary>
public static class InteropWindowExtensions
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    /// Tests if the interopWindow still exists
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <returns>True if it's still there.
    /// Because window handles are recycled the handle could point to a different window!
    /// </returns>
    public static bool Exists(this IInteropWindow interopWindow)
    {
        return User32Api.IsWindow(interopWindow.Handle);
    }

    /// <summary>
    ///     Fill ALL the information of the InteropWindow
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="retrieveSettings">InteropWindowRetrieveSettings to specify which information is retrieved and what not</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow Fill(this IInteropWindow interopWindow, InteropWindowRetrieveSettings retrieveSettings = InteropWindowRetrieveSettings.CacheAllAutoCorrect)
    {
        var forceUpdate = (retrieveSettings & InteropWindowRetrieveSettings.ForceUpdate) != 0;
        var autoCorrect = (retrieveSettings & InteropWindowRetrieveSettings.AutoCorrectValues) != 0;

        if ((retrieveSettings & InteropWindowRetrieveSettings.Info) != 0)
        {
            interopWindow.GetInfo(forceUpdate, autoCorrect);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Caption) != 0)
        {
            interopWindow.GetCaption(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Classname) != 0)
        {
            interopWindow.GetClassname(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.ProcessId) != 0)
        {
            interopWindow.GetProcessId(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Parent) != 0)
        {
            interopWindow.GetParent(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Owner) != 0)
        {
            interopWindow.GetOwner(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Visible) != 0)
        {
            interopWindow.IsVisible(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Maximized) != 0)
        {
            interopWindow.IsMaximized(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Minimized) != 0)
        {
            interopWindow.IsMinimized(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.ScrollInfo) != 0)
        {
            interopWindow.GetWindowScroller(forceUpdate: forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Children) != 0)
        {
            interopWindow.GetChildren(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Placement) != 0)
        {
            interopWindow.GetPlacement(forceUpdate);
        }
        if ((retrieveSettings & InteropWindowRetrieveSettings.Text) != 0)
        {
            interopWindow.GetText(forceUpdate);
        }
        return interopWindow;
    }

    /// <summary>
    ///     Get the Windows caption (title)
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>string with the caption</returns>
    public static string GetCaption(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Caption != null && !forceUpdate)
        {
            return interopWindow.Caption;
        }

        string caption;
        if (interopWindow.IsOwnedByCurrentThread())
        {
            // GetWindowText sends WM_GETTEXT, for a window of the calling thread this is a direct call of the window procedure, which can't deadlock
            caption = User32Api.GetText(interopWindow.Handle);
        }
        else if (interopWindow.IsOwnedByCurrentProcess())
        {
            // GetWindowText would send WM_GETTEXT to another thread of this process, which deadlocks when that thread waits for the caller.
            // InternalGetWindowText reads the stored caption without sending a message.
            caption = User32Api.GetInternalText(interopWindow.Handle);
        }
        else
        {
            // For windows of other processes GetWindowText doesn't send a message, it reads the stored caption
            caption = User32Api.GetText(interopWindow.Handle);
        }
        interopWindow.Caption = caption;
        return caption;
    }

    /// <summary>
    ///     Get the direct children of the specified interopWindow in Z-order, from top (front) to bottom (back), this is not lazy!
    ///     The children are a snapshot taken with <see cref="InteropWindowQuery.GetTopWindows"/>, the result is stored in <see cref="IInteropWindow.Children"/>
    ///     and returned from there until forceUpdate is true. Use <see cref="GetChildren(IInteropWindow, bool, bool)"/> with allLevels to fill the
    ///     children of the children too (one enumeration for the whole tree), or <see cref="GetDescendants"/> for a flat list.
    ///     The children of the desktop window are the top-level windows, these have no parent (<see cref="IInteropWindow.Parent"/> is IntPtr.Zero).
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">True to force updating</param>
    /// <returns>IEnumerable with InteropWindow</returns>
    public static IEnumerable<IInteropWindow> GetChildren(this IInteropWindow interopWindow, bool forceUpdate)
    {
        if (interopWindow.Children != null && !forceUpdate)
        {
            return interopWindow.Children;
        }

        var children = InteropWindowQuery.GetTopWindows(interopWindow);
        // Top-level windows have no parent, see GetParent
        var isDesktop = interopWindow.Handle == User32Api.GetDesktopWindow();
        foreach (var child in children)
        {
            child.Parent = isDesktop ? IntPtr.Zero : interopWindow.Handle;
            child.ParentWindow = isDesktop ? null : interopWindow;
        }
        // Store it in the Children property
        interopWindow.Children = children;
        return children;
    }

    /// <summary>
    ///     Get the direct children of the specified interopWindow in Z-order, see <see cref="GetChildren(IInteropWindow, bool)"/>.
    ///     With <paramref name="allLevels"/> the whole tree below the window is filled from one enumeration (EnumChildWindows) instead of one
    ///     enumeration per window: every descendant gets its <see cref="IInteropWindow.Children"/> (empty for a window without children, so it
    ///     isn't enumerated again), <see cref="IInteropWindow.Parent"/> and <see cref="IInteropWindow.ParentWindow"/>, so <see cref="GetInfo"/>
    ///     can clip to the parents without more lookups. For the desktop window the tree of every top-level window is filled, one enumeration each.
    ///     Without forceUpdate a tree which is already filled is used as it is.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">True to force updating</param>
    /// <param name="allLevels">True to fill the children of the children too, from one enumeration</param>
    /// <returns>IEnumerable with InteropWindow, the direct children</returns>
    public static IEnumerable<IInteropWindow> GetChildren(this IInteropWindow interopWindow, bool forceUpdate = false, bool allLevels = false)
    {
        if (!allLevels)
        {
            return interopWindow.GetChildren(forceUpdate);
        }
        if (!forceUpdate && IsTreeFilled(interopWindow))
        {
            return interopWindow.Children;
        }
        if (interopWindow.Handle == User32Api.GetDesktopWindow())
        {
            // The top-level windows, then the tree of each of them
            var topLevelWindows = interopWindow.GetChildren(true);
            foreach (var topLevelWindow in topLevelWindows)
            {
                FillChildTree(topLevelWindow);
            }
            return topLevelWindows;
        }
        FillChildTree(interopWindow);
        return interopWindow.Children;
    }

    /// <summary>
    ///     True when the window and all windows below it have their Children
    /// </summary>
    private static bool IsTreeFilled(IInteropWindow interopWindow)
    {
        var pending = new Stack<IInteropWindow>();
        pending.Push(interopWindow);
        while (pending.Count > 0)
        {
            var children = pending.Pop().Children;
            if (children == null)
            {
                return false;
            }
            foreach (var child in children)
            {
                pending.Push(child);
            }
        }
        return true;
    }

    /// <summary>
    ///     Fill the tree below the window from one EnumChildWindows: it enumerates depth-first in Z-order, so a parent comes before its
    ///     children and the children of each window keep their Z-order
    /// </summary>
    private static void FillChildTree(IInteropWindow root)
    {
        var windows = new Dictionary<IntPtr, IInteropWindow> { [root.Handle] = root };
        var children = new Dictionary<IntPtr, List<IInteropWindow>> { [root.Handle] = new List<IInteropWindow>() };
        WindowsEnumerator.EnumerateHandles(root.Handle, hWnd =>
        {
            var parent = User32Api.GetAncestor(hWnd, GetAncestorFlags.GA_PARENT);
            // A window which was re-parented during the enumeration (its parent isn't known) is left out
            if (windows.ContainsKey(hWnd) || !windows.TryGetValue(parent, out var parentWindow))
            {
                return true;
            }
            var window = InteropWindowFactory.CreateFor(hWnd);
            window.Parent = parent;
            window.ParentWindow = parentWindow;
            windows[hWnd] = window;
            children[hWnd] = new List<IInteropWindow>();
            children[parent].Add(window);
            return true;
        });
        foreach (var pair in children)
        {
            windows[pair.Key].Children = pair.Value;
        }
    }

    /// <summary>
    ///     Find the deepest visible child window at a point: starting with the window, take the first child (in Z-order) which is visible
    ///     (WS_VISIBLE in its WindowInfo) and whose <see cref="GetInfo"/> bounds contain the point, and continue with that child.
    ///     The children are filled with <see cref="GetChildren(IInteropWindow, bool, bool)"/> (all levels, one enumeration) when they weren't yet.
    ///     This works on the cached values, so it can be used on a snapshot of a screen which has changed since, e.g. under a full-screen overlay.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow to start with</param>
    /// <param name="point">NativePoint in screen coordinates</param>
    /// <returns>IInteropWindow, the deepest window containing the point, the window itself when no child contains it, null when the window doesn't contain it</returns>
    public static IInteropWindow FindChildAt(this IInteropWindow interopWindow, NativePoint point)
    {
        if (interopWindow == null)
        {
            throw new ArgumentNullException(nameof(interopWindow));
        }
        if (!interopWindow.GetInfo().Bounds.Contains(point))
        {
            return null;
        }
        if (interopWindow.Children == null)
        {
            interopWindow.GetChildren(false, true);
        }
        var current = interopWindow;
        // The depth is limited, a window tree which changed into a loop can't make this run forever
        for (var depth = 0; depth < 1000; depth++)
        {
            var next = current.Children?.FirstOrDefault(child =>
            {
                var info = child.GetInfo();
                return (info.Style & WindowStyleFlags.WS_VISIBLE) != 0 && info.Bounds.Contains(point);
            });
            if (next == null)
            {
                return current;
            }
            if (next.Children == null)
            {
                next.GetChildren(false, true);
            }
            current = next;
        }
        return current;
    }

    /// <summary>
    ///     Get all the descendants (children, their children etc.) of the specified interopWindow, as EnumChildWindows returns them. This is not lazy!
    ///     The result is not stored in the interopWindow, and the ParentWindow of the returned windows is not set.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>IReadOnlyList with IInteropWindow</returns>
    public static IReadOnlyList<IInteropWindow> GetDescendants(this IInteropWindow interopWindow)
    {
        return WindowsEnumerator.EnumerateWindows(interopWindow);
    }

    /// <summary>
    ///     Get the Windows class name
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>string with the classname</returns>
    public static string GetClassname(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Classname != null && !forceUpdate)
        {
            return interopWindow.Classname;
        }

        var className = User32Api.GetClassname(interopWindow.Handle);
        interopWindow.Classname = className;
        return interopWindow.Classname;
    }

    /// <summary>
    ///     Get the WindowInfo
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <param name="autoCorrect">enable auto correction, e,g, have the bounds cropped to the parent(s)</param>
    /// <returns>WindowInfo</returns>
    public static WindowInfo GetInfo(this IInteropWindow interopWindow, bool forceUpdate = false, bool autoCorrect = true)
    {
        if (interopWindow.Info.HasValue && !forceUpdate)
        {
            return interopWindow.Info.Value;
        }

        var windowInfo = WindowInfo.Create();
        if (!User32Api.GetWindowInfo(interopWindow.Handle, ref windowInfo))
        {
            // e.g. the window doesn't exist (anymore), don't cache the empty information
            Log.Debug().WriteLine("Couldn't retrieve the WindowInfo for window {0}", interopWindow.Handle);
            interopWindow.Info = null;
            return windowInfo;
        }

        // Test if we need to correct some values
        if (autoCorrect)
        {
            // Correct the bounds, for Windows 8+. DWM only answers for top-level windows, so child windows (WS_CHILD) skip this and the
            // IsApp / IsMaximized checks, they are only clipped to their parents. This matters for windows with many descendants.
            if (DwmApi.IsDwmEnabled && (windowInfo.Style & WindowStyleFlags.WS_CHILD) == 0)
            {
                bool gotFrameBounds = DwmApi.GetExtendedFrameBounds(interopWindow.Handle, out var extendedFrameBounds);
                if (gotFrameBounds && (interopWindow.IsApp() || WindowsVersion.IsWindows10OrLater && !interopWindow.IsMaximized()))
                {
                    windowInfo.Bounds = extendedFrameBounds;
                }
            }

            // Only a real parent (child windows), not the owner, clips the window
            var parentWindow = interopWindow.GetParentWindow(forceUpdate);
            if (parentWindow != null)
            {
                var parentInfo = parentWindow.GetInfo(forceUpdate, true);
                windowInfo.Bounds = windowInfo.Bounds.Intersect(parentInfo.Bounds);
                windowInfo.ClientBounds = windowInfo.ClientBounds.Intersect(parentInfo.ClientBounds);
            }
        }

        interopWindow.Info = windowInfo;
        return windowInfo;
    }

    /// <summary>
    ///     Get the (real) parent of the window, this is IntPtr.Zero for a top-level window.
    ///     Unlike the Win32 GetParent function this never returns the owner, use <see cref="GetOwner"/> for that.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>IntPtr for the parent, IntPtr.Zero if there is none</returns>
    public static IntPtr GetParent(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Parent.HasValue && !forceUpdate)
        {
            return interopWindow.Parent.Value;
        }
        // Like the Win32 GetParent, only a window with WS_CHILD has a parent. Other windows are top-level (also message-only windows),
        // for these GetParent would return the owner and GetAncestor(GA_PARENT) the desktop (or message-only root) window.
        var parent = IntPtr.Zero;
        var style = unchecked((WindowStyleFlags)(int)User32Api.GetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_STYLE).ToInt64());
        if ((style & WindowStyleFlags.WS_CHILD) != 0)
        {
            parent = User32Api.GetAncestor(interopWindow.Handle, GetAncestorFlags.GA_PARENT);
            if (parent == User32Api.GetDesktopWindow())
            {
                parent = IntPtr.Zero;
            }
        }
        // Invalidate ParentWindow if the value changed or is IntPtr.Zero
        if (interopWindow.ParentWindow?.Handle != parent)
        {
            interopWindow.ParentWindow = null;
        }
        interopWindow.Parent = parent;
        return parent;
    }

    /// <summary>
    ///     Get the owner of the window (GetWindow with GW_OWNER), e.g. the main window of the application for a dialog.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>IntPtr for the owner, IntPtr.Zero if there is none</returns>
    public static IntPtr GetOwner(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Owner.HasValue && !forceUpdate)
        {
            return interopWindow.Owner.Value;
        }
        var owner = User32Api.GetWindow(interopWindow.Handle, GetWindowCommands.GW_OWNER);
        interopWindow.Owner = owner;
        return owner;
    }

    /// <summary>
    ///     Get the owner of the window as IInteropWindow, a new instance is created every call.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the owner is retrieved again</param>
    /// <returns>IInteropWindow for the owner, or null if there is none</returns>
    public static IInteropWindow GetOwnerWindow(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        var owner = interopWindow.GetOwner(forceUpdate);
        return owner == IntPtr.Zero ? null : InteropWindowFactory.CreateFor(owner);
    }

    /// <summary>
    ///     Get the parent IInteropWindow
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>IInteropWindow for the parent, or null if there is none</returns>
    public static IInteropWindow GetParentWindow(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.ParentWindow != null && !forceUpdate)
        {
            return interopWindow.ParentWindow;
        }

        var parent = interopWindow.GetParent(forceUpdate);
        if (parent == IntPtr.Zero)
        {
            interopWindow.ParentWindow = null;
        }
        else if (interopWindow.ParentWindow?.Handle != parent)
        {
            interopWindow.ParentWindow = InteropWindowFactory.CreateFor(parent);
        }
        return interopWindow.ParentWindow;
    }

    /// <summary>
    ///     Get the WindowPlacement
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>WindowPlacement</returns>
    public static WindowPlacement GetPlacement(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Placement.HasValue && !forceUpdate)
        {
            return interopWindow.Placement.Value;
        }
        var placement = WindowPlacement.Create();
        User32Api.GetWindowPlacement(interopWindow.Handle, ref placement);
        interopWindow.Placement = placement;
        return interopWindow.Placement.Value;
    }

    /// <summary>
    ///     Get the process and thread which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>uint with process Id</returns>
    public static int GetProcessId(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.ProcessId.HasValue && !forceUpdate)
        {
            return interopWindow.ProcessId.Value;
        }

        var threadId = User32Api.GetWindowThreadProcessId(interopWindow.Handle, out var processId);
        interopWindow.ThreadId = threadId;
        interopWindow.ProcessId = processId;
        return interopWindow.ProcessId.Value;
    }

    /// <summary>
    ///     Get the region for a window
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    public static Region GetRegion(this IInteropWindow interopWindow)
    {
        using (var region = Gdi32Api.CreateRectRgn(0, 0, 0, 0))
        {
            if (region.IsInvalid)
            {
                return null;
            }
            var result = User32Api.GetWindowRgn(interopWindow.Handle, region);
            if (result != RegionResults.Error && result != RegionResults.NullRegion)
            {
                return Region.FromHrgn(region.DangerousGetHandle());
            }
        }
        return null;
    }

    /// <summary>
    ///     Get text from the window
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>string with the text</returns>
    public static string GetText(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (interopWindow.Text != null && !forceUpdate)
        {
            return interopWindow.Text;
        }
        var text = User32Api.GetTextFromWindow(interopWindow.Handle);
        interopWindow.Text = text;
        return text;
    }

    /// <summary>
    ///     Extension method to create a WindowScroller
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="scrollBarType">ScrollBarTypes</param>
    /// <param name="forceUpdate">true to force a retry, even if the previous check failed</param>
    /// <returns>WindowScroller or null</returns>
    public static WindowScroller GetWindowScroller(this IInteropWindow interopWindow, ScrollBarTypes scrollBarType = ScrollBarTypes.Vertical, bool forceUpdate = false)
    {
        if (!forceUpdate && interopWindow.CanScroll.HasValue && !interopWindow.CanScroll.Value)
        {
            return null;
        }
        var initialScrollInfo = ScrollInfo.Create(ScrollInfoMask.All);
        if (User32Api.GetScrollInfo(interopWindow.Handle, scrollBarType, ref initialScrollInfo) && initialScrollInfo.Minimum != initialScrollInfo.Maximum)
        {
            var windowScroller = new WindowScroller
            {
                ScrollingWindow = interopWindow,
                ScrollBarWindow = interopWindow,
                ScrollBarType = scrollBarType,
                InitialScrollInfo = initialScrollInfo,
                WheelDelta = WindowScroller.CalculateWheelDelta(initialScrollInfo.PageSize, WindowScroller.ScrollWheelLines)
            };
            interopWindow.CanScroll = true;
            return windowScroller;
        }
        if (User32Api.GetScrollInfo(interopWindow.Handle, ScrollBarTypes.Control, ref initialScrollInfo) && initialScrollInfo.Minimum != initialScrollInfo.Maximum)
        {
            var windowScroller = new WindowScroller
            {
                ScrollingWindow = interopWindow,
                ScrollBarWindow = interopWindow,
                ScrollBarType = ScrollBarTypes.Control,
                InitialScrollInfo = initialScrollInfo,
                WheelDelta = WindowScroller.CalculateWheelDelta(initialScrollInfo.PageSize, WindowScroller.ScrollWheelLines)
            };
            interopWindow.CanScroll = true;
            return windowScroller;
        }
        interopWindow.CanScroll = false;
        return null;
    }

    /// <summary>
    ///     Returns if the IInteropWindow is docked to the left of the other IInteropWindow
    /// </summary>
    /// <param name="window1">IInteropWindow</param>
    /// <param name="window2">IInteropWindow</param>
    /// <param name="retrieveBoundsFunc">Function which returns the bounds for the IInteropWindow</param>
    /// <returns>bool true if docked</returns>
    public static bool IsDockedToLeftOf(this IInteropWindow window1, IInteropWindow window2, Func<IInteropWindow, NativeRect> retrieveBoundsFunc = null)
    {
        retrieveBoundsFunc ??= (window => window.GetInfo().Bounds);
        return retrieveBoundsFunc(window1).IsDockedToLeftOf(retrieveBoundsFunc(window2));
    }

    /// <summary>
    ///     Returns if the IInteropWindow is docked to the left of the other IInteropWindow
    /// </summary>
    /// <param name="window1">IInteropWindow</param>
    /// <param name="window2">IInteropWindow</param>
    /// <param name="retrieveBoundsFunc">Function which returns the bounds for the IInteropWindow</param>
    /// <returns>bool true if docked</returns>
    public static bool IsDockedToRightOf(this IInteropWindow window1, IInteropWindow window2, Func<IInteropWindow, NativeRect> retrieveBoundsFunc = null)
    {
        retrieveBoundsFunc ??= (window => window.GetInfo().Bounds);
        return retrieveBoundsFunc(window1).IsDockedToRightOf(retrieveBoundsFunc(window2));
    }

    /// <summary>
    ///     Retrieve if the window is maximized (Iconic)
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>bool true if Iconic (minimized)</returns>
    public static bool IsMaximized(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (!interopWindow.IsMaximized.HasValue || forceUpdate)
        {
            interopWindow.IsMaximized = User32Api.IsZoomed(interopWindow.Handle);
        }
        return interopWindow.IsMaximized.Value;
    }

    /// <summary>
    ///     Retrieve if the window is minimized (Iconic)
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>bool true if Iconic (minimized)</returns>
    public static bool IsMinimized(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (!interopWindow.IsMinimized.HasValue || forceUpdate)
        {
            interopWindow.IsMinimized = User32Api.IsIconic(interopWindow.Handle);
        }
        return interopWindow.IsMinimized.Value;
    }

    /// <summary>
    ///     Retrieve if the window is Visible and not cloaked (different desktop)
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="forceUpdate">set to true to make sure the value is updated</param>
    /// <returns>bool true if minimizedIconic (minimized)</returns>
    public static bool IsVisible(this IInteropWindow interopWindow, bool forceUpdate = false)
    {
        if (!interopWindow.IsVisible.HasValue || forceUpdate)
        {
            interopWindow.IsVisible = User32Api.IsWindowVisible(interopWindow.Handle) && !DwmApi.IsWindowCloaked(interopWindow.Handle);
        }
        return interopWindow.IsVisible.Value;
    }

    /// <summary>
    ///     Test if the window is owned by the current process
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>bool true if the window is owned by the current process</returns>
    public static bool IsOwnedByCurrentProcess(this IInteropWindow interopWindow)
    {
        return Kernel32Api.GetCurrentProcessId() == interopWindow.GetProcessId();
    }

    /// <summary>
    ///     Test if the window is owned by the current thread
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>bool true if the window is owned by the current thread</returns>
    public static bool IsOwnedByCurrentThread(this IInteropWindow interopWindow)
    {
        // Although the process id is returned, the following also reads the Thread-ID
        interopWindow.GetProcessId();
        return Kernel32Api.GetCurrentThreadId() == interopWindow.ThreadId;
    }

    /// <summary>
    ///     Maximize the window
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow Maximize(this IInteropWindow interopWindow)
    {
        User32Api.ShowWindow(interopWindow.Handle, ShowWindowCommands.Maximize);
        interopWindow.IsMaximized = true;
        interopWindow.IsMinimized = false;
        return interopWindow;
    }

    /// <summary>
    ///     Minimize the Window
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow Minimize(this IInteropWindow interopWindow)
    {
        User32Api.ShowWindow(interopWindow.Handle, ShowWindowCommands.Minimize);
        interopWindow.IsMinimized = true;
        return interopWindow;
    }

    /// <summary>
    ///     Post a message to the window: it is placed in the message queue of the thread which created the window, this doesn't wait until it's processed.
    ///     Use this e.g. for WM_CLOSE, so a window which shows a "save changes?" dialog doesn't block the caller.
    ///     Don't post messages with pointers to memory (e.g. WM_SETTEXT), the memory might be gone before the message is processed.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="windowsMessage">WindowsMessages</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information, default IntPtr.Zero</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information, default IntPtr.Zero</param>
    /// <returns>true if the message was posted, false if not (e.g. the window doesn't exist, the message queue is full, or UIPI blocks it), use Marshal.GetLastWin32Error for the reason</returns>
    public static bool PostMessage(this IInteropWindow interopWindow, WindowsMessages windowsMessage, IntPtr wParam = default, IntPtr lParam = default)
    {
        return User32Api.PostMessage(interopWindow.Handle, windowsMessage, wParam, lParam);
    }

    /// <summary>
    ///     Post a message, e.g. one registered with RegisterWindowMessage, to the window: it is placed in the message queue of the thread which created the window, this doesn't wait until it's processed.
    ///     Don't post messages with pointers to memory, the memory might be gone before the message is processed.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="message">uint with the message id</param>
    /// <param name="wParam">IntPtr, pointer-sized message-specific information, default IntPtr.Zero</param>
    /// <param name="lParam">IntPtr, pointer-sized message-specific information, default IntPtr.Zero</param>
    /// <returns>true if the message was posted, false if not, use Marshal.GetLastWin32Error for the reason</returns>
    public static bool PostMessage(this IInteropWindow interopWindow, uint message, IntPtr wParam = default, IntPtr lParam = default)
    {
        return User32Api.PostMessage(interopWindow.Handle, message, wParam, lParam);
    }

    /// <summary>
    ///     Restore (Un-Minimize/Maximize) the Window
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow Restore(this IInteropWindow interopWindow)
    {
        User32Api.ShowWindow(interopWindow.Handle, ShowWindowCommands.Restore);
        interopWindow.IsMinimized = false;
        interopWindow.IsMaximized = false;
        return interopWindow;
    }

    /// <summary>
    ///     Set the Extended WindowStyle
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="extendedWindowStyleFlags">ExtendedWindowStyleFlags</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow SetExtendedStyle(this IInteropWindow interopWindow, ExtendedWindowStyleFlags extendedWindowStyleFlags)
    {
        User32Api.SetExtendedWindowStyle(interopWindow.Handle, extendedWindowStyleFlags);
        interopWindow.Info = null;
        return interopWindow;
    }

    /// <summary>
    ///     Set the WindowStyle
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="windowStyleFlags">WindowStyleFlags</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow SetStyle(this IInteropWindow interopWindow, WindowStyleFlags windowStyleFlags)
    {
        User32Api.SetWindowStyle(interopWindow.Handle, windowStyleFlags);
        interopWindow.Info = null;
        return interopWindow;
    }

    /// <summary>
    ///     Set the WindowPlacement
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="placement">WindowPlacement</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow SetPlacement(this IInteropWindow interopWindow, WindowPlacement placement)
    {
        User32Api.SetWindowPlacement(interopWindow.Handle, ref placement);
        interopWindow.Placement = placement;
        return interopWindow;
    }

    /// <summary>
    ///     Set the window as foreground window, a minimized window is restored first (also when it already is the foreground window,
    ///     which a minimized window can be)
    /// </summary>
    /// <param name="interopWindow">The window to bring to the foreground</param>
    public static async ValueTask ToForegroundAsync(this IInteropWindow interopWindow)
    {
        // Nothing we can do if it's not visible!
        if (!interopWindow.IsVisible())
        {
            return;
        }

        if (interopWindow.IsMinimized(true))
        {
            interopWindow.Restore();
            // Wait until the window is restored, but not forever
            var waitUntil = DateTime.UtcNow + RestoreTimeout;
            while (interopWindow.IsMinimized(true) && DateTime.UtcNow < waitUntil)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        var foregroundWindow = User32Api.GetForegroundWindow();
        // Window is already the foreground window
        if (foregroundWindow == interopWindow.Handle)
        {
            return;
        }

        // See https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
        // Attaching the input of the calling thread to the thread of the current foreground window allows the calling thread to change the foreground window
        var currentThreadId = Kernel32Api.GetCurrentThreadId();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero ? 0 : User32Api.GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        var isAttached = foregroundThreadId != 0 && foregroundThreadId != currentThreadId && User32Api.AttachThreadInput(currentThreadId, foregroundThreadId, true);
        try
        {
            User32Api.BringWindowToTop(interopWindow.Handle);
            User32Api.SetForegroundWindow(interopWindow.Handle);
        }
        finally
        {
            if (isAttached)
            {
                User32Api.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    /// <summary>
    ///     The maximum time ToForegroundAsync waits for a minimized window to be restored
    /// </summary>
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Move the specified window to a new location
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="location">NativePoint with the offset</param>
    /// <returns>IInteropWindow for fluent calls</returns>
    public static IInteropWindow MoveTo(this IInteropWindow interopWindow, NativePoint location)
    {
        User32Api.SetWindowPos(interopWindow.Handle, IntPtr.Zero, location.X, location.Y, 0, 0, WindowPos.SWP_NOSIZE | WindowPos.SWP_SHOWWINDOW | WindowPos.SWP_NOACTIVATE | WindowPos.SWP_NOZORDER);
        interopWindow.Info = null;
        return interopWindow;
    }

    /// <summary>
    /// Get all the other windows belonging to the process which owns the specified window
    /// </summary>
    /// <param name="windowToLinkTo">IInteropWindow</param>
    /// <returns>IEnumerable of IInteropWindow</returns>
    public static IEnumerable<IInteropWindow> GetLinkedWindows(this IInteropWindow windowToLinkTo)
    {
        int processIdSelectedWindow = windowToLinkTo.GetProcessId();
        return InteropWindowQuery.GetVisibleApplicationWindows().Where(window => window.Handle != windowToLinkTo.Handle && window.GetProcessId() == processIdSelectedWindow);
    }

    /// <summary>
    ///     Get a location where this window would be visible
    ///     * if none is found return false, formLocation = the original location
    ///     * if something is found, return true and formLocation = new location
    /// </summary>
    /// <param name="interopWindow">IInteropWindow, the window to find a location for</param>
    /// <param name="formLocation">NativePoint with the location where the window will fit</param>
    /// <returns>true if a location if found, and the formLocation is also set</returns>
    public static bool GetVisibleLocation(this IInteropWindow interopWindow, out NativePoint formLocation)
    {
        bool doesWindowFit = false;
        var windowRectangle = interopWindow.GetInfo().Bounds;
        // assume own location
        formLocation = windowRectangle.Location;
        var displays = DisplayInfo.AllDisplayInfos;
        if (displays.Length == 0)
        {
            return false;
        }
        using (var workingArea = new Region(Rectangle.Empty))
        {
            // Create a region with the bounds of all screens
            foreach (var display in displays)
            {
                workingArea.Union(display.Bounds);
            }

            // If the formLocation is not inside the visible area
            if (!workingArea.AreRectangleCornersVisible(windowRectangle))
            {
                // Try to place the window at the top-left of the working area (not below a taskbar) of one of the displays, the primary first
                foreach (var display in displays.OrderByDescending(display => display.IsPrimary))
                {
                    var newWindowRectangle = new Rectangle(display.WorkingArea.Location, windowRectangle.Size);
                    if (!workingArea.AreRectangleCornersVisible(newWindowRectangle))
                    {
                        continue;
                    }
                    formLocation = display.WorkingArea.Location;
                    doesWindowFit = true;
                    break;
                }
            }
            else
            {
                doesWindowFit = true;
            }
        }
        return doesWindowFit;
    }

    /// <summary>
    /// Return a Bitmap representing the Window!
    /// As GDI+ draws it, it will be without Aero borders!
    /// The window is printed with its complete window rectangle, and cropped to the (DWM corrected) bounds of <see cref="GetInfo"/>,
    /// so the invisible resize borders are not part of the result.
    /// Dapplo.Windows.Wpf has PrintWindowAsBitmapSource for a WPF BitmapSource.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <returns>Bitmap, which the caller needs to dispose, or null if the window couldn't be printed</returns>
    public static Bitmap PrintWindow(this IInteropWindow interopWindow)
    {
        // PrintWindow draws the window with the origin at the window rectangle, which includes the invisible borders.
        // GetInfo().Bounds might be the DWM extended frame bounds, which excludes these, so the uncorrected window rectangle is needed for the bitmap.
        var rawWindowInfo = WindowInfo.Create();
        if (!User32Api.GetWindowInfo(interopWindow.Handle, ref rawWindowInfo))
        {
            Log.Error().WriteLine("Error calling print window, couldn't get the window rectangle: {0}", new Win32Exception().Message);
            return null;
        }
        var windowRect = rawWindowInfo.Bounds;
        if (windowRect.Width <= 0 || windowRect.Height <= 0)
        {
            return null;
        }

        Exception exceptionOccured = null;
        Bitmap printWindowBitmap;
        using (var region = interopWindow.GetRegion())
        {
            var pixelFormat = PixelFormat.Format24bppRgb;
            // Only use 32 bpp ARGB when the window has a region
            if (region != null)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            printWindowBitmap = new Bitmap(windowRect.Width, windowRect.Height, pixelFormat);
            using (var graphics = Graphics.FromImage(printWindowBitmap))
            {
                using (var graphicsDc = graphics.GetSafeDeviceContext())
                {
                    // PW_RENDERFULLCONTENT (Windows 8.1 and later) is needed for DirectComposition content, e.g. browsers and UWP apps, otherwise this is black
                    var printWindowFlags = WindowsVersion.IsWindows81OrLater ? PrintWindowFlags.PW_RENDERFULLCONTENT : PrintWindowFlags.PW_COMPLETE;
                    bool printSucceeded = User32Api.PrintWindow(interopWindow.Handle, graphicsDc.DangerousGetHandle(), printWindowFlags);
                    if (!printSucceeded)
                    {
                        // something went wrong, most likely a "0x80004005" (Acess Denied) when using UAC
                        exceptionOccured = new Win32Exception();
                    }
                }

                // Apply the region "transparency", the region is relative to the window rectangle
                if (region != null && !region.IsEmpty(graphics))
                {
                    graphics.ExcludeClip(region);
                    graphics.Clear(Color.Transparent);
                }

                graphics.Flush();
            }
        }

        // Return null if error
        if (exceptionOccured != null)
        {
            Log.Error().WriteLine("Error calling print window: {0}", exceptionOccured.Message);
            printWindowBitmap.Dispose();
            return null;
        }

        // Crop to the visible bounds, e.g. without the invisible resize borders
        var visibleBounds = interopWindow.GetInfo().Bounds.Intersect(windowRect);
        if (visibleBounds.IsEmpty || visibleBounds.Equals(windowRect))
        {
            return printWindowBitmap;
        }
        var cropRectangle = new Rectangle(visibleBounds.X - windowRect.X, visibleBounds.Y - windowRect.Y, visibleBounds.Width, visibleBounds.Height);
        try
        {
            return printWindowBitmap.Clone(cropRectangle, printWindowBitmap.PixelFormat);
        }
        finally
        {
            printWindowBitmap.Dispose();
        }
    }
}