// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Extension code for icons
    /// </summary>
    public static class IconExtensions
    {
        /// <summary>
        ///     Get the icon for a hWnd
        /// </summary>
        /// <typeparam name="TIcon">The return type for the icon, can be Icon or Bitmap (Dapplo.Windows.Wpf has ToBitmapSource() to convert these to a WPF BitmapSource)</typeparam>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="useLargeIcons">true to try to get a big icon first</param>
        /// <returns>TIcon</returns>
        public static TIcon GetIconForWindowHandle<TIcon>(IntPtr hWnd, bool useLargeIcons = false) where TIcon : class
        {
            var iconSmall = IntPtr.Zero;
            var iconBig = new IntPtr(1);
            var iconSmall2 = new IntPtr(2);

            // The icon set with WM_SETICON, else the class icon: many windows never get WM_SETICON and answer WM_GETICON with 0
            IntPtr GetIcon(IntPtr iconType, ClassLongIndex classIcon)
            {
                if (User32Api.TrySendMessage(hWnd, WindowsMessages.WM_GETICON, iconType, IntPtr.Zero, out var handle) && handle != IntPtr.Zero)
                {
                    return handle;
                }
                return User32Api.GetClassLongWrapper(hWnd, classIcon);
            }

            // The preferred size first, then the other size
            var iconHandle = useLargeIcons ? GetIcon(iconBig, ClassLongIndex.IconHandle) : GetIcon(iconSmall2, ClassLongIndex.SmallIconHandle);
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = GetIcon(iconSmall, ClassLongIndex.SmallIconHandle);
            }
            if (iconHandle == IntPtr.Zero && !useLargeIcons)
            {
                iconHandle = GetIcon(iconBig, ClassLongIndex.IconHandle);
            }
            return IconHelper.IconHandleTo<TIcon>(iconHandle);
        }
    }
}
