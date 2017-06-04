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

using System;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Messages;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.App;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Gdi32.SafeHandles;

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Extension code for icons
    /// </summary>
    public static class IconExtensions
    {
        /// <summary>
        /// Convert an Icon to an ImageSource
        /// </summary>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static BitmapSource ToImageSource(this Icon icon)
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
                return IconHelper.GetModernAppLogo<TIcon>(window);
            }
            var iconSmall = IntPtr.Zero;
            var iconBig = new IntPtr(1);
            var iconSmall2 = new IntPtr(2);

            IntPtr iconHandle;
            if (useLargeIcons)
            {
                iconHandle = User32Api.SendMessage(window.Handle, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
                if (iconHandle == IntPtr.Zero)
                {
                    iconHandle = User32Api.GetClassLongWrapper(window.Handle, ClassLongIndex.IconHandle);
                }
            }
            else
            {
                iconHandle = User32Api.SendMessage(window.Handle, WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.SendMessage(window.Handle, WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.GetClassLongWrapper(window.Handle, ClassLongIndex.SmallIconHandle);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.SendMessage(window.Handle, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = User32Api.GetClassLongWrapper(window.Handle, ClassLongIndex.IconHandle);
            }

            if (iconHandle == IntPtr.Zero)
            {
                return null;
            }
            if (typeof(TIcon) == typeof(Icon))
            {
                return Icon.FromHandle(iconHandle) as TIcon;
            }
            if (typeof(TIcon) == typeof(Bitmap))
            {
                using (var icon = Icon.FromHandle(iconHandle))
                {
                    return icon.ToBitmap() as TIcon;
                }

            }
            if (typeof(TIcon) == typeof(BitmapSource))
            {
                using (var icon = Icon.FromHandle(iconHandle))
                {
                    return icon.ToImageSource() as TIcon;
                }
            }
            return default(TIcon);
        }

    }
}
