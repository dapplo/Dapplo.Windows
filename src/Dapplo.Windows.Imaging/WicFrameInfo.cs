// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The first frame of a decoded image
/// </summary>
public sealed class WicFrameInfo
{
    internal WicFrameInfo(int width, int height, double dpiX, double dpiY, bool hasResolution, Guid pixelFormat, Guid containerFormat, int frameCount, WicMetadata metadata, bool orientationApplied)
    {
        Width = width;
        Height = height;
        DpiX = dpiX;
        DpiY = dpiY;
        HasResolution = hasResolution;
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
    /// The horizontal resolution in dots per inch, after the orientation when it is applied.
    /// Reported by the decoder; when the file stores its resolution only in EXIF and the decoder doesn't report that (JPEG without JFIF density, WebP, HEIF, ...)
    /// it is the EXIF XResolution. See <see cref="HasResolution"/>.
    /// </summary>
    public double DpiX { get; }

    /// <summary>
    /// The vertical resolution in dots per inch, after the orientation when it is applied
    /// </summary>
    public double DpiY { get; }

    /// <summary>
    /// True when the file itself stores a resolution (PNG pHYs in pixels per meter, JFIF density in inch or cm, EXIF/TIFF XResolution with a unit,
    /// JPEG XR WIDTH_RESOLUTION, BMP pixels per meter, ...).
    /// False when <see cref="DpiX"/> / <see cref="DpiY"/> are only the default of the decoder, because the file stores none, or only an aspect ratio.
    /// WIC reports a resolution in both cases, e.g. 96 for a PNG without pHYs and 72 for a WebP without EXIF; use your own default when this is false.
    /// </summary>
    public bool HasResolution { get; }

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
