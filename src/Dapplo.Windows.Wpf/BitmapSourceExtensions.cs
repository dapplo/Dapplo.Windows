// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dapplo.Windows.Wpf;

/// <summary>
///     Conversions from System.Drawing images and icons to a WPF BitmapSource
/// </summary>
public static class BitmapSourceExtensions
{
    /// <summary>
    ///     Convert a Bitmap to a (frozen) BitmapSource with 96 DPI, the alpha channel is preserved.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        // Bitmap.GetHbitmap composites the alpha channel onto a background color, so the pixels are copied instead.
        // LockBits converts every source format (also indexed ones) to the requested 32 bpp format.
        var hasAlpha = System.Drawing.Image.IsAlphaPixelFormat(bitmap.PixelFormat);
        var lockFormat = hasAlpha ? System.Drawing.Imaging.PixelFormat.Format32bppArgb : System.Drawing.Imaging.PixelFormat.Format32bppRgb;
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, lockFormat);
        try
        {
            // Format32bppArgb is BGRA in memory, which is Bgra32 for WPF, Format32bppRgb is Bgr32
            var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, 96, 96,
                hasAlpha ? PixelFormats.Bgra32 : PixelFormats.Bgr32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
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
