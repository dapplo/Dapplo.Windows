// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Gdi32.SafeHandles;

namespace Dapplo.Windows.Wpf;

/// <summary>
///     Conversions from System.Drawing images and icons to a WPF BitmapSource
/// </summary>
public static class BitmapSourceExtensions
{
    /// <summary>
    ///     Convert a Bitmap to a BitmapSource
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        using var hBitmap = new SafeHBitmapHandle(bitmap.GetHbitmap());
        return Imaging.CreateBitmapSourceFromHBitmap(hBitmap.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
    }

    /// <summary>
    ///     Convert an Image (Bitmap) to a BitmapSource
    /// </summary>
    /// <param name="image">Image</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource ToBitmapSource(this Image image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }
        if (image is Bitmap bitmap)
        {
            return bitmap.ToBitmapSource();
        }
        using var copy = new Bitmap(image);
        return copy.ToBitmapSource();
    }

    /// <summary>
    ///     Convert an Icon to a BitmapSource
    /// </summary>
    /// <param name="icon">Icon</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource ToBitmapSource(this Icon icon)
    {
        if (icon == null)
        {
            throw new ArgumentNullException(nameof(icon));
        }
        using var bitmap = icon.ToBitmap();
        return bitmap.ToBitmapSource();
    }
}
