// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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