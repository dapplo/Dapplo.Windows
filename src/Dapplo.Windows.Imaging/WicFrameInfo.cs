// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The first frame of a decoded image
/// </summary>
public sealed class WicFrameInfo
{
    internal WicFrameInfo(int width, int height, double dpiX, double dpiY, Guid pixelFormat, Guid containerFormat, int frameCount, WicMetadata metadata, bool orientationApplied)
    {
        Width = width;
        Height = height;
        DpiX = dpiX;
        DpiY = dpiY;
        PixelFormat = pixelFormat;
        ContainerFormat = containerFormat;
        FrameCount = frameCount;
        Metadata = metadata ?? WicMetadata.Empty;
        OrientationApplied = orientationApplied;
    }

    /// <summary>
    /// The width in pixels, after the orientation when it is applied
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The height in pixels, after the orientation when it is applied
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The horizontal resolution in dots per inch, after the orientation when it is applied
    /// </summary>
    public double DpiX { get; }

    /// <summary>
    /// The vertical resolution in dots per inch, after the orientation when it is applied
    /// </summary>
    public double DpiY { get; }

    /// <summary>
    /// The pixel format of the frame as stored, see <see cref="WicPixelFormats"/>
    /// </summary>
    public Guid PixelFormat { get; }

    /// <summary>
    /// The container format, see <see cref="WicContainerFormats"/>
    /// </summary>
    public Guid ContainerFormat { get; }

    /// <summary>
    /// The number of frames, only the first is decoded
    /// </summary>
    public int FrameCount { get; }

    /// <summary>
    /// The metadata of the first frame, never null
    /// </summary>
    public WicMetadata Metadata { get; }

    /// <summary>
    /// True when the EXIF orientation was applied, so Width and Height are those of the rotated image.
    /// <see cref="WicMetadata.Orientation"/> keeps the value of the file.
    /// </summary>
    public bool OrientationApplied { get; }
}
