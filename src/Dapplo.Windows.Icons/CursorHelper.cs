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

#if !NETSTANDARD2_0
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Helper methods for using cursor information
    /// </summary>
    public static class CursorHelper
    {
        /// <summary>
        /// This method will capture the current Cursor by using User32 Code
        /// </summary>
        /// <returns>A IElement with the Mouse Cursor as Image in it.</returns>
        public static bool TryGetCurrentCursor(out BitmapSource bitmapSource, out NativePoint location)
        {
            bitmapSource = null;
            location = NativePoint.Empty;
            var cursorInfo = CursorInfo.Create();
            if (!NativeCursorMethods.GetCursorInfo(ref cursorInfo))
            {
                return false;
            }

            if (cursorInfo.Flags != CursorInfoFlags.Showing)
            {
                return false;
            }

            using (var safeIcon = NativeIconMethods.CopyIcon(cursorInfo.CursorHandle))
            {
                if (!NativeIconMethods.GetIconInfo(safeIcon, out var iconInfo))
                {
                    return false;
                }

                using (iconInfo.BitmaskBitmapHandle)
                using (iconInfo.ColorBitmapHandle)
                {
                    var cursorLocation = User32Api.GetCursorLocation();
                    bitmapSource = Imaging.CreateBitmapSourceFromHIcon(safeIcon.DangerousGetHandle(), Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    location = new NativePoint(cursorLocation.X - iconInfo.Hotspot.X, cursorLocation.Y - iconInfo.Hotspot.Y);
                }
            }

            return true;
        }
    }
}
#endif