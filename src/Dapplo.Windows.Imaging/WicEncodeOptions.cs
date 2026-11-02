// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The options for <see cref="WicImage.Encode(System.IO.Stream, WicEncodeOptions, IntPtr, int, int, int, Guid)"/>
/// </summary>
public sealed class WicEncodeOptions
{
    /// <summary>
    /// The container format, e.g. <see cref="WicContainerFormats.JpegXl"/>, <see cref="WicContainerFormats.Heif"/>, <see cref="WicContainerFormats.Wmp"/> or <see cref="WicContainerFormats.Png"/>
    /// </summary>
    public Guid ContainerFormat { get; set; }

    /// <summary>
    /// 0..1, the encoder option "ImageQuality", when the encoder supports it
    /// </summary>
    public float? ImageQuality { get; set; }

    /// <summary>
    /// The encoder option "Lossless", when the encoder supports it
    /// </summary>
    public bool? Lossless { get; set; }

    /// <summary>
    /// Any other encoder option by name, e.g. "CompressionQuality", "TiffCompressionMethod" or "FilterOption".
    /// The value is converted to the type the encoder reports; an option the encoder doesn't have is skipped.
    /// See <see cref="WicImage.GetEncoderOptions"/> for the options of an encoder.
    /// </summary>
    public IDictionary<string, object> EncoderOptions { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The horizontal resolution in dots per inch
    /// </summary>
    public double DpiX { get; set; } = 96;

    /// <summary>
    /// The vertical resolution in dots per inch
    /// </summary>
    public double DpiY { get; set; } = 96;

    /// <summary>
    /// Called after the frame is initialized and before the pixels are written, to write metadata and a color profile.
    /// What the encoder doesn't support is skipped and reported in <see cref="WicEncodeResult.MetadataIssues"/>, it never fails the encoding.
    /// </summary>
    public Action<WicMetadataWriter> Metadata { get; set; }

    /// <summary>
    /// The palette for an indexed image (e.g. GIF, or a PNG with a palette), at most 256 colors as 32 bit BGRA values (0xAARRGGBB, the layout of
    /// GDI+ Color.ToArgb and of WICColor). A color with alpha 0 is the transparent color.
    /// Required when the pixels have an indexed pixel format (<see cref="WicPixelFormats.Format8bppIndexed"/>), the indexes refer to this palette.
    /// With other pixels the encoder is asked for an 8 bit indexed format and each pixel gets the nearest color of the palette.
    /// When it is null and the encoder writes an indexed format (GIF), the palette is made from the pixels: with at most 256 colors (and no partial
    /// transparency) these exact colors, otherwise WIC chooses the colors.
    /// </summary>
    public IReadOnlyList<uint> Palette { get; set; }

    /// <summary>
    /// Use error diffusion dithering when the pixels are reduced to a palette with fewer colors than the image has; false (default) uses the nearest color
    /// </summary>
    public bool Dither { get; set; }
}

/// <summary>
/// The result of an encoding
/// </summary>
public sealed class WicEncodeResult
{
    internal WicEncodeResult(Guid pixelFormat, IReadOnlyList<string> appliedOptions, IReadOnlyList<WicMetadataIssue> metadataIssues)
    {
        PixelFormat = pixelFormat;
        AppliedOptions = appliedOptions;
        MetadataIssues = metadataIssues;
    }

    /// <summary>
    /// The pixel format the encoder wrote, the pixels were converted when it differs from the format of the source
    /// </summary>
    public Guid PixelFormat { get; }

    /// <summary>
    /// The names of the encoder options which were set
    /// </summary>
    public IReadOnlyList<string> AppliedOptions { get; }

    /// <summary>
    /// The metadata which the encoder didn't accept
    /// </summary>
    public IReadOnlyList<WicMetadataIssue> MetadataIssues { get; }
}

/// <summary>
/// An option of an encoder, see <see cref="WicImage.GetEncoderOptions"/>
/// </summary>
public sealed class WicEncoderOption
{
    internal WicEncoderOption(string name, ushort varType)
    {
        Name = name;
        VarType = varType;
    }

    /// <summary>
    /// The name, e.g. "ImageQuality"
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The VARTYPE of the value, e.g. 4 (VT_R4) for a float or 11 (VT_BOOL) for a bool
    /// </summary>
    public ushort VarType { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} (VARTYPE {VarType})";
}
