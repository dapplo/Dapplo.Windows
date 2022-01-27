// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Point = SixLabors.ImageSharp.Point;

namespace Dapplo.Windows.ImageSharpInterop;

/// <summary>
/// Extension methods to help when we need to use ImageSharp routines on on System.Drawing.Bitmap
/// </summary>
public static class ImageSharpBitmapExtensions
{
    /// <summary>
    /// Use ImageSharp's mutate method on a GDI bitmap.
    /// Do not change the size or the structure of the image.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <param name="mutateAction">Action taking an IImageProcessingContext</param>
    /// <param name="configuration">Configuration</param>
    public static void Mutate(this Bitmap bitmap, Action<IImageProcessingContext> mutateAction, Configuration configuration = null)
    {
        using var bitmapWrapper = new BitmapWrapper(bitmap);
        bitmapWrapper.ImageWrapper.Mutate(configuration ?? Configuration.Default, mutateAction);
    }

    /// <summary>
    /// Use ImageSharp's clone method on a GDI bitmap.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <param name="mutateAction">Action taking an IImageProcessingContext</param>
    /// <param name="configuration">Configuration</param>
    public static Bitmap Clone(this Bitmap bitmap, Action<IImageProcessingContext> mutateAction, Configuration configuration = null)
    {
        using var bitmapWrapper = new BitmapWrapper(bitmap);

        var newImage = bitmapWrapper.ImageWrapper.Clone(configuration ?? Configuration.Default, mutateAction);

        // TODO: check if this is the best performance, is this way to do so?
        var result = new Bitmap(newImage.Width, newImage.Height, PixelFormat.Format32bppArgb);
        using var resultWrapper = new BitmapWrapper(result);
        resultWrapper.ImageWrapper.Mutate(c => c.DrawImage(bitmapWrapper.ImageWrapper, Point.Empty, 0));
        return result;
    }

    /// <summary>
    /// Wrap a bitmap
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>BitmapWrapper</returns>
    public static BitmapWrapper Wrap(this Bitmap bitmap)
    {
        return new BitmapWrapper(bitmap);
    }
}