//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using Dapplo.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using System.Drawing.Imaging;
using System.Linq;
using Dapplo.Log;
using Dapplo.Windows.Extensions;

#endregion

namespace Dapplo.Windows.Desktop
{
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
        /// <param name="cacheFlags">InteropWindowCacheFlags to specify which information is retrieved and what not</param>
        /// <returns>IInteropWindow for fluent calls</returns>
        public static IInteropWindow Fill(this IInteropWindow interopWindow, InteropWindowCacheFlags cacheFlags = InteropWindowCacheFlags.CacheAll)
        {
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Children) && cacheFlags.HasFlag(InteropWindowCacheFlags.ZOrderedChildren))
            {
                throw new ArgumentException("Can't have both Children & ZOrderedChildren", nameof(cacheFlags));
            }
            var forceUpdate = cacheFlags.HasFlag(InteropWindowCacheFlags.ForceUpdate);

            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Info))
            {
                interopWindow.GetInfo(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Caption))
            {
                interopWindow.GetCaption(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Classname))
            {
                interopWindow.GetClassname(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.ProcessId))
            {
                interopWindow.GetProcessId(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Parent))
            {
                interopWindow.GetParent(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Visible))
            {
                interopWindow.IsVisible(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Maximized))
            {
                interopWindow.IsMaximized(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Minimized))
            {
                interopWindow.IsMinimized(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.ScrollInfo))
            {
                interopWindow.GetWindowScroller(forceUpdate: forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Children))
            {
                interopWindow.GetChildren(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.ZOrderedChildren))
            {
                interopWindow.GetZOrderedChildren(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Placement))
            {
                interopWindow.GetPlacement(forceUpdate);
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Text))
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
            if (interopWindow.Caption == null || forceUpdate)
            {
                var caption = User32Api.GetText(interopWindow.Handle);
                interopWindow.Caption = caption;
            }
            return interopWindow.Caption;
        }

        /// <summary>
        ///     Get the children of the specified interopWindow, this is not lazy!
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">True to force updating</param>
        /// <returns>IEnumerable with InteropWindow</returns>
        public static IEnumerable<IInteropWindow> GetChildren(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.HasChildren && !interopWindow.HasZOrderedChildren && !forceUpdate)
            {
                return interopWindow.Children;
            }
            interopWindow.HasZOrderedChildren = false;

            var children = new List<IInteropWindow>();
            // Store it in the Children property
            interopWindow.Children = children;
            foreach (var child in WindowsEnumerator.EnumerateWindows(interopWindow))
            {
                child.ParentWindow = interopWindow;
                children.Add(child);
            }
            return children;
        }

        /// <summary>
        ///     Get the Windows class name
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <returns>string with the classname</returns>
        public static string GetClassname(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.Classname == null || forceUpdate)
            {
                var className = User32Api.GetClassname(interopWindow.Handle);
                interopWindow.Classname = className;
            }
            return interopWindow.Classname;
        }

        /// <summary>
        ///     Get the WindowInfo
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <param name="cropToParent">set to true to have the bounds cropped to the parent(s)</param>
        /// <returns>WindowInfo</returns>
        public static WindowInfo GetInfo(this IInteropWindow interopWindow, bool forceUpdate = false, bool cropToParent = true)
        {
            if (interopWindow.Info.HasValue && !forceUpdate)
            {
                return interopWindow.Info.Value;
            }

            var windowInfo = WindowInfo.Create();
            User32Api.GetWindowInfo(interopWindow.Handle, ref windowInfo);

            // Now correct the bounds, for Windows 8+
            if (Dwm.IsDwmEnabled)
            {
                // This only works for top level windows, otherwise a access denied is returned
                bool gotFrameBounds = Dwm.GetExtendedFrameBounds(interopWindow.Handle, out var extendedFrameBounds);
                if (gotFrameBounds && (interopWindow.IsApp() || WindowsVersion.IsWindows10OrLater && !interopWindow.IsMaximized()))
                {
                    windowInfo.Bounds = extendedFrameBounds;
                }
            }

            if (cropToParent)
            {
                var parentWindow = interopWindow.GetParentWindow();
                if (interopWindow.HasParent)
                {
                    var parentInfo = parentWindow.GetInfo(forceUpdate);
                    windowInfo.Bounds = windowInfo.Bounds.Intersect(parentInfo.Bounds);
                    windowInfo.ClientBounds = windowInfo.ClientBounds.Intersect(parentInfo.ClientBounds);
                }
            }

            interopWindow.Info = windowInfo;
            return interopWindow.Info.Value;
        }

        /// <summary>
        ///     Get the parent
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <returns>IntPtr for the parent</returns>
        public static IntPtr GetParent(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.Parent.HasValue && !forceUpdate)
            {
                return interopWindow.Parent.Value;
            }
            // TODO: Invalidate ParentWindow if the value changed or is IntPtr.Zero?
            var parent = User32Api.GetParent(interopWindow.Handle);
            interopWindow.Parent = parent;
            return interopWindow.Parent.Value;
        }

        /// <summary>
        ///     Get the parent IInteropWindow
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <returns>IInteropWindow for the parent</returns>
        public static IInteropWindow GetParentWindow(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.ParentWindow != null && !forceUpdate)
            {
                return interopWindow.ParentWindow;
            }
            interopWindow.GetParent(forceUpdate);
            if (interopWindow.Parent.HasValue && interopWindow.Parent.Value != IntPtr.Zero)
            {
                interopWindow.ParentWindow = InteropWindowFactory.CreateFor(interopWindow.Parent.Value);
            }
            // TODO: Invalidate ParentWindow if IntPtr.Zero?!
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
        ///     Get the process which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <returns>int with process Id</returns>
        public static int GetProcessId(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.ProcessId.HasValue && !forceUpdate)
            {
                return interopWindow.ProcessId.Value;
            }
            int processId;
            User32Api.GetWindowThreadProcessId(interopWindow.Handle, out processId);
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
            return interopWindow.Text;
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
                    WheelDelta = (int) (120 * (initialScrollInfo.PageSize / WindowScroller.ScrollWheelLinesFromRegistry))
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
                    WheelDelta = (int) (120 * (initialScrollInfo.PageSize / WindowScroller.ScrollWheelLinesFromRegistry))
                };
                interopWindow.CanScroll = true;
                return windowScroller;
            }
            interopWindow.CanScroll = false;
            return null;
        }

        /// <summary>
        ///     Get the children of the specified interopWindow, from top to bottom. This is not lazy
        ///     This might get different results than the GetChildren
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">True to force updating</param>
        /// <returns>IEnumerable with InteropWindow</returns>
        public static IEnumerable<IInteropWindow> GetZOrderedChildren(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (interopWindow.HasChildren && interopWindow.HasZOrderedChildren && !forceUpdate)
            {
                return interopWindow.Children;
            }
            interopWindow.HasZOrderedChildren = true;

            var children = new List<IInteropWindow>();
            // Store it in the Children property
            interopWindow.Children = children;
            foreach (var child in InteropWindowQuery.GetTopWindows(interopWindow))
            {
                child.ParentWindow = interopWindow;
                children.Add(child);
            }
            return children;
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
            retrieveBoundsFunc = retrieveBoundsFunc ?? (window => window.GetInfo().Bounds);
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
            retrieveBoundsFunc = retrieveBoundsFunc ?? (window => window.GetInfo().Bounds);
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
        ///     Retrieve if the window is Visible (IsWindowVisible, whatever that means)
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="forceUpdate">set to true to make sure the value is updated</param>
        /// <returns>bool true if minimizedIconic (minimized)</returns>
        public static bool IsVisible(this IInteropWindow interopWindow, bool forceUpdate = false)
        {
            if (!interopWindow.IsVisible.HasValue || forceUpdate)
            {
                interopWindow.IsVisible = User32Api.IsWindowVisible(interopWindow.Handle);
            }
            return interopWindow.IsVisible.Value;
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
            User32Api.SetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint) extendedWindowStyleFlags));
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
            User32Api.SetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint)windowStyleFlags));
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
        ///     Set the window as foreground window
        /// </summary>
        /// <param name="interopWindow">The window to bring to the foreground</param>
        /// <param name="workaround">bool with true to use a trick (press Alt) to really bring the window to the foreground</param>
        public static async Task ToForegroundAsync(this IInteropWindow interopWindow, bool workaround = true)
        {
            // Nothing we can do if it's not visible!
            if (!interopWindow.IsVisible())
            {
                return;
            }
            // Window is already the foreground window
            if (User32Api.GetForegroundWindow() == interopWindow.Handle)
            {
                return;
            }
            if (interopWindow.IsMinimized())
            {
                interopWindow.Restore();
                while (interopWindow.IsMinimized())
                {
                    await Task.Delay(50).ConfigureAwait(false);
                }
            }

            // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx
            if (workaround)
            {
                // Simulate an "ALT" key press, make it double to remove menu activation
                InputGenerator.KeyPress(VirtualKeyCodes.MENU, VirtualKeyCodes.MENU);
            }
            // Show window in forground.
            User32Api.BringWindowToTop(interopWindow.Handle);
            User32Api.SetForegroundWindow(interopWindow.Handle);
        }

        /// <summary>
        /// Move the specified window to a new location
        /// </summary>
        /// <param name="interopWindow">IInteropWindow</param>
        /// <param name="location">NativePoin with the offset</param>
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
            return InteropWindowQuery.GetTopLevelWindows().Where(window => window.Handle != windowToLinkTo.Handle && window.GetProcessId() == processIdSelectedWindow);
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
            var primaryDisplay = User32Api.AllDisplays().First(x => x.IsPrimary);
            using (var workingArea = new Region(primaryDisplay.Bounds))
            {
                // Create a region with the screens working area
                foreach (var display in User32Api.AllDisplays())
                {
                    if (!display.IsPrimary)
                    {
                        workingArea.Union(display.Bounds);
                    }
                }

                // If the formLocation is not inside the visible area
                if (!workingArea.AreRectangleCornersVisisble(windowRectangle))
                {
                    // If none found we find the biggest screen
                    foreach (var display in User32Api.AllDisplays())
                    {
                        var newWindowRectangle = new Rectangle(display.WorkingArea.Location, windowRectangle.Size);
                        if (!workingArea.AreRectangleCornersVisisble(newWindowRectangle))
                        {
                            continue;
                        }
                        formLocation = display.Bounds.Location;
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
        /// Return an Image representing the Window!
        /// As GDI+ draws it, it will be without Aero borders!
        /// </summary>
        public static TBitmap PrintWindow<TBitmap>(this IInteropWindow interopWindow) where TBitmap : class
        {
            var windowRect = interopWindow.GetInfo().Bounds;
            // Start the capture
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
                        bool printSucceeded = User32Api.PrintWindow(interopWindow.Handle, graphicsDc.DangerousGetHandle(), PrintWindowFlags.PW_COMPLETE);
                        if (!printSucceeded)
                        {
                            // something went wrong, most likely a "0x80004005" (Acess Denied) when using UAC
                            exceptionOccured = new Win32Exception();
                        }
                    }

                    // Apply the region "transparency"
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
                return default(TBitmap);
            }
            if (typeof(TBitmap).IsAssignableFrom(typeof(Bitmap)))
            {
                return printWindowBitmap as TBitmap;
            }
            try
            {
                return printWindowBitmap.ToBitmapSource() as TBitmap;
            }
            finally
            {
                printWindowBitmap.Dispose();
            }
        }
    }
}