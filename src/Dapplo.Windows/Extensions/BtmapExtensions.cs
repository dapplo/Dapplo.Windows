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
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Gdi32.SafeHandles;

namespace Dapplo.Windows.Extensions
{
    /// <summary>
    /// Extensions for Bitmaps
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        ///     Convert a Bitmap to a BitmapSource
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns>BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            using (var hBitmap = new SafeHBitmapHandle(bitmap.GetHbitmap()))
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        /// <summary>
        ///     Convert a Image (Bitmap) to a BitmapSource
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            var bitmap = image as Bitmap;
            return bitmap.ToBitmapSource();
        }
    }
}
#endif