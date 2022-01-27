// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Dapplo.Windows.ImageSharpInterop;

/// <summary>
/// Extension methods to help when we need to use ImageSharp routines on System.Windows.Media.Imaging.WriteableBitmap
/// </summary>
public static class ImageSharpWritableBitmapExtensions
{
    /// <summary>
    /// Use ImageSharp's mutate method on a WriteableBitmap.
    /// Do not change the size or the structure of the image.
    /// </summary>
    /// <param name="writeableBitmap">WriteableBitmap</param>
    /// <param name="mutateAction">Action taking an IImageProcessingContext</param>
    /// <param name="configuration">Configuration</param>
    public static void Mutate(this WriteableBitmap writeableBitmap, Action<IImageProcessingContext> mutateAction, Configuration configuration = null)
    {
        using var w = new WriteableBitmapWrapper(writeableBitmap);
        w.ImageWrapper.Mutate(configuration ?? Configuration.Default, mutateAction);
    }
}