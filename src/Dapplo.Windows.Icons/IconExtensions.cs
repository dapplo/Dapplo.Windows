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

using System;
using System.Diagnostics;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Messages;
using System.Linq;
using Dapplo.Windows.App;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Kernel32;

#if !NETSTANDARD2_0
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Gdi32.SafeHandles;
#endif

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Extension code for icons
    /// </summary>
    public static class IconExtensions
    {
#if !NETSTANDARD2_0
        /// <summary>
        /// Convert an Icon to an ImageSource
        /// </summary>
        /// <param name="icon">Icon</param>
        /// <returns>BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Icon icon)
        {
            var bitmap = icon.ToBitmap();
            using (var hBitmapHandle = new SafeHBitmapHandle(bitmap.GetHbitmap()))
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmapHandle.DangerousGetHandle(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        }
#endif
        /// <summary>
        ///     Get the icon for a hWnd
        /// </summary>
        /// <typeparam name="TIcon">The return type for the icon, can be Icon, Bitmap or BitmapSource</typeparam>
        /// <param name="window">IInteropWindow</param>
        /// <param name="useLargeIcons">true to try to get a big icon first</param>
        /// <returns>TIcon</returns>
        public static TIcon GetIcon<TIcon>(this IInteropWindow window, bool useLargeIcons = false) where TIcon : class
        {
            if (window.IsApp())
            {
                return IconHelper.GetAppLogo<TIcon>(window);
            }
            var icon = GetIconFromWindow<TIcon>(window, useLargeIcons);
            if (icon != null)
            {
                return icon;
            }
            var processId = window.GetProcessId();
            // Try to get the icon from the process file itself, if we can query the path
            var processPath = Kernel32Api.GetProcessPath(processId);
            if (processPath != null)
            {
                return IconHelper.ExtractAssociatedIcon<TIcon>(processPath, useLargeIcon: useLargeIcons);
            }
            // Look at the windows of the other similar named processes
            using (var process = Process.GetProcessById(processId))
            {
                var processName = process.ProcessName;
                foreach (var possibleParentProcess in Process.GetProcessesByName(processName))
                {
                    var parentProcessWindow = InteropWindowFactory.CreateFor(possibleParentProcess.MainWindowHandle);
                    icon = GetIconFromWindow<TIcon>(parentProcessWindow, useLargeIcons);
                    if (icon != null)
                    {
                        return icon;
                    }
                    possibleParentProcess.Dispose();
                }
            }
            // Try to find another window, which belongs to the same process, and get the icon from there
            foreach(var otherWindow in InteropWindowQuery.GetTopWindows().Where(interopWindow => interopWindow.GetProcessId() == processId))
            {
                if (otherWindow.Handle == window.Handle)
                {
                    continue;
                }
                icon = GetIconFromWindow<TIcon>(otherWindow, useLargeIcons);
                if (icon != null)
                {
                    return icon;
                }

            }
            // Nothing found, REALLY!
            return default;
        }

        /// <summary>
        ///     Get the icon for an IInteropWindow
        /// </summary>
        /// <typeparam name="TIcon">The return type for the icon, can be Icon, Bitmap or BitmapSource</typeparam>
        /// <param name="window">IInteropWindow</param>
        /// <param name="useLargeIcons">true to try to get a big icon first</param>
        /// <returns>TIcon</returns>
        public static TIcon GetIconFromWindow<TIcon>(this IInteropWindow window, bool useLargeIcons = false) where TIcon : class
        {
            return GetIconForWindowHandle<TIcon>(window.Handle, useLargeIcons);
        }

        /// <summary>
        ///     Get the icon for a hWnd
        /// </summary>
        /// <typeparam name="TIcon">The return type for the icon, can be Icon, Bitmap or BitmapSource</typeparam>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="useLargeIcons">true to try to get a big icon first</param>
        /// <returns>TIcon</returns>
        public static TIcon GetIconForWindowHandle<TIcon>(IntPtr hWnd, bool useLargeIcons = false) where TIcon : class
        {
            var iconSmall = IntPtr.Zero;
            var iconBig = new IntPtr(1);
            var iconSmall2 = new IntPtr(2);

            IntPtr iconHandle;
            if (useLargeIcons)
            {
                if (!User32Api.TrySendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, out iconHandle))
                {
                    iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.IconHandle);
                }
            }
            else if (!User32Api.TrySendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall2, out iconHandle))
            {
                iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.SmallIconHandle);
            }

            if (iconHandle == IntPtr.Zero && !User32Api.TrySendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall, out iconHandle))
            {
                iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.SmallIconHandle);
            }

            if (iconHandle == IntPtr.Zero && !User32Api.TrySendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, out iconHandle))
            {
                iconHandle = User32Api.GetClassLongWrapper(hWnd, ClassLongIndex.IconHandle);
            }
            return IconHelper.IconHandleTo<TIcon>(iconHandle);
        }
    }
}
