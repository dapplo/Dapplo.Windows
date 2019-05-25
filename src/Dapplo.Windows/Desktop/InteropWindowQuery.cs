//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dapplo.Windows.App;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     Query for native windows
    /// </summary>
    public static class InteropWindowQuery
    {
        /// <summary>
        ///     Window classes which can be ignored
        /// </summary>
        public static ConcurrentBag<string> IgnoreClasses { get; } = new ConcurrentBag<string>(new[] {"Progman", "Button", "Dwm"}); //"MS-SDIa"

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
        /// <param name="parent">InteropWindow as the parent, to iterate over the children, or null for all</param>
        /// <returns>IEnumerable with all the top level windows</returns>
        public static IEnumerable<IInteropWindow> GetTopWindows(IInteropWindow parent = null)
        {
            // TODO: Guard against looping
            var windowPtr = parent == null ? User32Api.GetTopWindow(IntPtr.Zero) : User32Api.GetWindow(parent.Handle, GetWindowCommands.GW_CHILD);
            do
            {
                yield return InteropWindowFactory.CreateFor(windowPtr);
                windowPtr = User32Api.GetWindow(windowPtr, GetWindowCommands.GW_HWNDNEXT);
            } while (windowPtr != IntPtr.Zero);
        }

        /// <summary>
        /// Check the Classname of the IInteropWindow against a list of know classes which can be ignored.
        /// </summary>
        /// <param name="interopWindow">IInteropWindow</param>
        /// <returns>bool</returns>
        public static bool CanIgnoreClass(this IInteropWindow interopWindow)
        {
            return IgnoreClasses.Contains(interopWindow.GetClassname());
        }

        /// <summary>
        /// Is the specified window a visible popup
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

            // Windows without parent
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

            // Ignore windows with a parent
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
}