// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dapplo.Windows.App;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Query for native windows
/// </summary>
public static class InteropWindowQuery
{
    private static readonly object IgnoreClassesLock = new object();

    // Replaced as a whole (copy on write), so readers never need a lock. "Button" is e.g. the top-level Start button of Windows 7.
    private static HashSet<string> _ignoreClasses = new HashSet<string>(StringComparer.Ordinal) {"Progman", "Button", "Dwm"}; //"MS-SDIa"

    /// <summary>
    ///     Window classes which can be ignored, this is a snapshot, use <see cref="AddIgnoreClass"/> and <see cref="RemoveIgnoreClass"/> to change it.
    /// </summary>
    public static IReadOnlyCollection<string> IgnoreClasses => Volatile.Read(ref _ignoreClasses);

    /// <summary>
    ///     Add a window class to the classes which are ignored
    /// </summary>
    /// <param name="classname">string with the window class</param>
    /// <returns>true if it was added, false if it was already ignored</returns>
    public static bool AddIgnoreClass(string classname)
    {
        if (classname == null)
        {
            throw new ArgumentNullException(nameof(classname));
        }
        lock (IgnoreClassesLock)
        {
            if (_ignoreClasses.Contains(classname))
            {
                return false;
            }
            var newIgnoreClasses = new HashSet<string>(_ignoreClasses, StringComparer.Ordinal) { classname };
            Volatile.Write(ref _ignoreClasses, newIgnoreClasses);
            return true;
        }
    }

    /// <summary>
    ///     Remove a window class from the classes which are ignored
    /// </summary>
    /// <param name="classname">string with the window class</param>
    /// <returns>true if it was removed, false if it wasn't ignored</returns>
    public static bool RemoveIgnoreClass(string classname)
    {
        if (classname == null)
        {
            throw new ArgumentNullException(nameof(classname));
        }
        lock (IgnoreClassesLock)
        {
            if (!_ignoreClasses.Contains(classname))
            {
                return false;
            }
            var newIgnoreClasses = new HashSet<string>(_ignoreClasses, StringComparer.Ordinal);
            newIgnoreClasses.Remove(classname);
            Volatile.Write(ref _ignoreClasses, newIgnoreClasses);
            return true;
        }
    }

    /// <summary>
    ///     Get the window with which the user is currently working
    /// </summary>
    /// <returns>IInteropWindow</returns>
    public static IInteropWindow GetForegroundWindow()
    {
        return InteropWindowFactory.CreateFor(User32Api.GetForegroundWindow());
    }

    /// <summary>
    ///     Gets the Desktop window
    /// </summary>
    /// <returns>IInteropWindow for the desktop window</returns>
    public static IInteropWindow GetDesktopWindow()
    {
        return InteropWindowFactory.CreateFor(User32Api.GetDesktopWindow());
    }

    /// <summary>
    ///     Find windows belonging to the same process (thread) as the process ID.
    /// </summary>
    /// <param name="processId">int with process Id</param>
    /// <returns>IEnumerable with IInteropWindow</returns>
    public static IEnumerable<IInteropWindow> GetWindowsForProcess(int processId)
    {
        using (var process = Process.GetProcessById(processId))
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var handles = User32Api.EnumThreadWindows(thread.Id);
                thread.Dispose();
                foreach (var handle in handles)
                {
                    yield return InteropWindowFactory.CreateFor(handle);
                }
            }
        }
    }

    /// <summary>
    ///     Get the windows the user sees as application windows (see <see cref="IsVisibleApplicationWindow(IInteropWindow, bool, bool)"/>), from top to bottom:
    ///     visible (and not cloaked), not minimized unless <paramref name="includeMinimized"/>, with a title and a size, no tool window and no child window.
    ///     The windows are a snapshot taken by <see cref="GetTopWindows"/> when this method is called, the filter is applied lazily while enumerating the result.
    /// </summary>
    /// <param name="ignoreKnownClasses">true (default) to ignore windows with a class from <see cref="IgnoreClasses"/></param>
    /// <param name="includeMinimized">true to include minimized windows, e.g. for a "capture this window" menu which restores the window first</param>
    /// <returns>IEnumerable with the visible application windows</returns>
    public static IEnumerable<IInteropWindow> GetVisibleApplicationWindows(bool ignoreKnownClasses = true, bool includeMinimized = false)
    {
        return GetTopWindows().Where(window => window.IsVisibleApplicationWindow(ignoreKnownClasses, includeMinimized));
    }

    /// <summary>
    ///     Get the windows the user sees as application windows, see <see cref="GetVisibleApplicationWindows(bool, bool)"/>.
    ///     Kept for binary compatibility.
    /// </summary>
    /// <param name="ignoreKnownClasses">true to ignore windows with a class from <see cref="IgnoreClasses"/></param>
    /// <returns>IEnumerable with the visible application windows</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IEnumerable<IInteropWindow> GetVisibleApplicationWindows(bool ignoreKnownClasses) => GetVisibleApplicationWindows(ignoreKnownClasses, false);

    /// <summary>
    ///     Get the windows in Z-order, from top (front) to bottom (back), without any filter.
    ///     This is a snapshot, taken at once when this method is called (it's not lazy): without a parent (or with the desktop window as parent)
    ///     EnumWindows is used, with a parent EnumChildWindows, filtered to the direct children (GetAncestor with GA_PARENT is the parent).
    ///     Windows builds the list when the enumeration starts, so unlike a GetWindow(GW_HWNDNEXT) walk the result can't loop, skip or repeat windows when the Z-order changes.
    ///     The windows can still change or be destroyed after the snapshot was taken, use <see cref="InteropWindowExtensions.Exists"/> when that matters.
    ///     Note: the EnumWindows documentation states that Windows 8 and later only enumerate the top-level windows of desktop apps, so windows of Windows 8 style immersive apps might be missing.
    /// </summary>
    /// <param name="parent">InteropWindow as the parent, to get its direct children, or null for the top-level windows</param>
    /// <returns>IReadOnlyList with the windows, empty if there are none</returns>
    public static IReadOnlyList<IInteropWindow> GetTopWindows(IInteropWindow parent = null)
    {
        var parentHandle = parent?.Handle ?? IntPtr.Zero;
        if (parentHandle == User32Api.GetDesktopWindow())
        {
            // The children of the desktop window are the top-level windows, EnumWindows enumerates them without their descendants
            parentHandle = IntPtr.Zero;
        }

        var windows = new List<IInteropWindow>();
        // EnumChildWindows enumerates depth-first in Z-order: a child is followed by its descendants and then its next sibling,
        // so filtering the descendants to the direct children keeps the children in Z-order
        WindowsEnumerator.EnumerateHandles(parentHandle, hWnd =>
        {
            if (parentHandle == IntPtr.Zero || User32Api.GetAncestor(hWnd, GetAncestorFlags.GA_PARENT) == parentHandle)
            {
                windows.Add(InteropWindowFactory.CreateFor(hWnd));
            }
            return true;
        });
        return windows;
    }

    /// <summary>
    /// Check the Classname of the IInteropWindow against a list of know classes which can be ignored.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <returns>bool</returns>
    public static bool CanIgnoreClass(this IInteropWindow interopWindow)
    {
        var classname = interopWindow.GetClassname();
        return classname != null && Volatile.Read(ref _ignoreClasses).Contains(classname);
    }

    /// <summary>
    /// The first checks of <see cref="IsVisibleApplicationWindow(IInteropWindow, bool, bool)"/> and <see cref="IsVisiblePopup"/>, cheapest first:
    /// most top-level windows are invisible, so IsWindowVisible (WS_VISIBLE, e.g. not the hidden preview windows of Firefox) rejects them before
    /// the class name (GetClassName) and the cloak check (DwmGetWindowAttribute: on another virtual desktop, or a suspended UWP app).
    /// The cached <see cref="IInteropWindow.IsVisible"/> keeps its meaning: visible and not cloaked.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="ignoreKnownClasses">true to reject windows with a class from <see cref="IgnoreClasses"/></param>
    /// <returns>true if the window is visible, not cloaked and its class is not ignored</returns>
    private static bool IsVisibleAndNotIgnored(this IInteropWindow interopWindow, bool ignoreKnownClasses)
    {
        if (interopWindow.IsVisible.HasValue)
        {
            return interopWindow.IsVisible.Value && !(ignoreKnownClasses && interopWindow.CanIgnoreClass());
        }

        if (!User32Api.IsWindowVisible(interopWindow.Handle))
        {
            // Not visible, so also not "visible and not cloaked"
            interopWindow.IsVisible = false;
            return false;
        }

        if (ignoreKnownClasses && interopWindow.CanIgnoreClass())
        {
            return false;
        }

        var isVisible = !DwmApi.IsWindowCloaked(interopWindow.Handle);
        interopWindow.IsVisible = isVisible;
        return isVisible;
    }

    /// <summary>
    /// Is the specified window a visible popup: a top-level window (it can be owned, it has no parent) with the WS_POPUP style,
    /// which is visible (WS_VISIBLE and not cloaked), has a size and is not minimized. Unlike <see cref="IsVisibleApplicationWindow(IInteropWindow, bool, bool)"/>
    /// tool windows and windows without a title are included.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="ignoreKnownClasses">true (default) to ignore windows with a class from <see cref="IgnoreClasses"/></param>
    /// <returns>true if the IInteropWindow is a visible popup</returns>
    public static bool IsVisiblePopup(this IInteropWindow interopWindow, bool ignoreKnownClasses = true)
    {
        if (!interopWindow.IsVisibleAndNotIgnored(ignoreKnownClasses))
        {
            return false;
        }

        // Get the info for the size, the style & extended style
        var windowInfo = interopWindow.GetInfo();
        // Windows without size
        if (windowInfo.Bounds.IsEmpty)
        {
            return false;
        }

        // Only top-level windows, child windows (which have a parent) are no popups. The owner is not the parent, so owned popups are popups.
        if (interopWindow.GetParent() != IntPtr.Zero)
        {
            return false;
        }

        if ((windowInfo.Style & WindowStyleFlags.WS_POPUP) == 0)
        {
            return false;
        }
        // A Windows 10 App which runs in the background, has a HWnd but is not visible.
        if (interopWindow.IsBackgroundWin10App())
        {
            return false;
        }
        return !interopWindow.IsMinimized();
    }

    /// <summary>
    ///     Check if the window is what the user sees as an application window (e.g. what Alt+Tab shows), see <see cref="IsVisibleApplicationWindow(IInteropWindow, bool, bool)"/>.
    ///     Kept for binary compatibility.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="ignoreKnownClasses">true to ignore windows with a class from <see cref="IgnoreClasses"/></param>
    /// <returns>true if the window is a visible application window</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool IsVisibleApplicationWindow(this IInteropWindow interopWindow, bool ignoreKnownClasses) => interopWindow.IsVisibleApplicationWindow(ignoreKnownClasses, false);

    /// <summary>
    ///     Check if the window is what the user sees as an application window (e.g. what Alt+Tab shows):
    ///     a top-level window (it can be owned, it has no parent) which is visible (WS_VISIBLE and not cloaked, so not on another virtual desktop
    ///     and no suspended UWP app), has a size, is not a tool window (WS_EX_TOOLWINDOW), is not a background Windows 10 app, has a title and
    ///     is not minimized (unless <paramref name="includeMinimized"/>).
    ///     Windows which render with DirectComposition (WS_EX_NOREDIRECTIONBITMAP, e.g. Chromium based browsers) are included.
    ///     This method will retrieve all information, and fill it to the interopWindow, it needs to make the decision.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="ignoreKnownClasses">true (default) to ignore windows with a class from <see cref="IgnoreClasses"/></param>
    /// <param name="includeMinimized">true to accept minimized windows (their bounds are the small off-screen rectangle, which is not empty)</param>
    /// <returns>true if the window is a visible application window</returns>
    public static bool IsVisibleApplicationWindow(this IInteropWindow interopWindow, bool ignoreKnownClasses = true, bool includeMinimized = false)
    {
        if (!interopWindow.IsVisibleAndNotIgnored(ignoreKnownClasses))
        {
            return false;
        }

        var info = interopWindow.GetInfo();
        // Windows without size
        if (info.Bounds.IsEmpty)
        {
            return false;
        }

        // Ignore child windows, these have a parent. The owner is not the parent, so owned windows (e.g. dialogs) are top-level.
        if (interopWindow.GetParent() != IntPtr.Zero)
        {
            return false;
        }

        if ((info.ExtendedStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
        {
            return false;
        }

        // A Windows 10 App which runs in the background, has a HWnd but is not visible.
        if (interopWindow.IsBackgroundWin10App())
        {
            return false;
        }

        // Ignore windows without title
        if (interopWindow.GetCaption().Length == 0)
        {
            return false;
        }
        return includeMinimized || !interopWindow.IsMinimized();
    }
}