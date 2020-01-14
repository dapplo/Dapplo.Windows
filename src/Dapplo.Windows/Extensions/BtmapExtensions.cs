// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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