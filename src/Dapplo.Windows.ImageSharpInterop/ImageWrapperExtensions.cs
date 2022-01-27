// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Dapplo.Windows.ImageSharpInterop;

/// <summary>
/// Extension methods to help when we need to use ImageSharp routines via a IImageWrapper
/// </summary>
public static class ImageWrapperExtensions
{
    /// <summary>
    /// Use ImageSharp's mutate method on an IImageWrapper.
    /// Do not change the size or the structure of the image.
    /// </summary>
    /// <param name="imageWrapper">IImageWrapper</param>
    /// <param name="mutateAction">Action taking an IImageProcessingContext</param>
    /// <param name="configuration">Configuration</param>
    public static void Mutate(this IImageWrapper imageWrapper, Action<IImageProcessingContext> mutateAction, Configuration configuration = null)
    {
        imageWrapper.ImageWrapper.Mutate(configuration ?? Configuration.Default, mutateAction);
    }
}