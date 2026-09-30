// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dapplo.Windows.App;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Query for native windows
/// </summary>
public static class InteropWindowQuery
{
    /// <summary>
    ///     The maximum number of windows which GetTopWindows returns, this protects against endless loops when the Z-order changes during the walk
    /// </summary>
    private const int MaxWindowsInWalk = 65536;

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
    ///     Iterate the Top level windows, from top to bottom
    /// </summary>
    /// <param name="ignoreKnownClasses">true to ignore windows with certain known classes</param>
    /// <returns>IEnumerable with all the top level windows</returns>
    public static IEnumerable<IInteropWindow> GetTopLevelWindows(bool ignoreKnownClasses = true)
    {
        return GetTopWindows().Where(possibleTopLevel => possibleTopLevel.IsTopLevel(ignoreKnownClasses));
    }

    /// <summary>
    ///     Iterate the windows, from top to bottom
    /// </summary>
    /// <param name="parent">InteropWindow as the parent, to iterate over its direct children, or null for the top-level windows</param>
    /// <returns>IEnumerable with the windows, empty if there are none</returns>
    public static IEnumerable<IInteropWindow> GetTopWindows(IInteropWindow parent = null)
    {
        var windowPtr = parent == null ? User32Api.GetTopWindow(IntPtr.Zero) : User32Api.GetWindow(parent.Handle, GetWindowCommands.GW_CHILD);
        // The Z-order can change during the walk, which could make it loop, so never visit a window twice
        var visited = new HashSet<IntPtr>();
        while (windowPtr != IntPtr.Zero && visited.Count < MaxWindowsInWalk && visited.Add(windowPtr))
        {
            yield return InteropWindowFactory.CreateFor(windowPtr);
            windowPtr = User32Api.GetWindow(windowPtr, GetWindowCommands.GW_HWNDNEXT);
        }
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
    /// Is the specified window a visible popup, this is a top-level window (it can be owned) with the WS_POPUP style.
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="ignoreKnowClasses">true (default) to ignore some known internal windows classes</param>
    /// <returns>true if the IInteropWindow is a popup</returns>
    public static bool IsPopup(this IInteropWindow interopWindow, bool ignoreKnowClasses = true)
    {
        if (ignoreKnowClasses && interopWindow.CanIgnoreClass())
        {
            return false;
        }

        // Windows without size
        if (interopWindow.GetInfo().Bounds.IsEmpty)
        {
            return false;
        }

        // Only top-level windows, child windows (which have a parent) are no popups. The owner is not the parent, so owned popups are popups.
        if (interopWindow.GetParent() != IntPtr.Zero)
        {
            return false;
        }

        // Get the info for the style & extended style
        var windowInfo = interopWindow.GetInfo();
        var windowStyle = windowInfo.Style;
        if ((windowStyle & WindowStyleFlags.WS_POPUP) == 0)
        {
            return false;
        }
        var exWindowStyle = windowInfo.ExtendedStyle;
        // Skip everything which is not rendered "normally"
        if (!interopWindow.IsWin8App() && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
        {
            return false;
        }
        // A Windows 10 App which runs in the background, has a HWnd but is not visible.
        if (interopWindow.IsBackgroundWin10App())
        {
            return false;
        }
        // Skip preview windows, like the one from Firefox
        if ((windowStyle & WindowStyleFlags.WS_VISIBLE) == 0)
        {
            return false;
        }
        return !interopWindow.IsMinimized();
    }

    /// <summary>
    ///     Check if the window is a top level window.
    ///     This method will retrieve all information, and fill it to the interopWindow, it needs to make the decision.
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    /// <param name="ignoreKnowClasses">true (default) to ignore classes from the IgnoreClasses list</param>
    /// <returns>bool</returns>
    public static bool IsTopLevel(this IInteropWindow interopWindow, bool ignoreKnowClasses = true)
    {
        if (ignoreKnowClasses && interopWindow.CanIgnoreClass())
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

        var exWindowStyle = info.ExtendedStyle;
        if ((exWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
        {
            return false;
        }

        // Skip everything which is not rendered "normally"
        if (!interopWindow.IsWin8App() && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
        {
            return false;
        }

        // A Windows 10 App which runs in the background, has a HWnd but is not visible.
        if (interopWindow.IsBackgroundWin10App())
        {
            return false;
        }

        // Skip preview windows, windows without WS_VISIBLE, like the one from Firefox
        if ((info.Style & WindowStyleFlags.WS_VISIBLE) == 0)
        {
            return false;
        }

        // Ignore windows without title
        if (interopWindow.GetCaption().Length == 0)
        {
            return false;
        }
        return !interopWindow.IsMinimized();
    }
}