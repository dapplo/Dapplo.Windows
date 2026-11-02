// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The WIC metadata format GUIDs (GUID_MetadataFormat* of wincodecsdk.h) of the blocks which are used most, see <see cref="WicMetadataRawBlock.Format"/>.
/// </summary>
public static class WicMetadataFormats
{
    /// <summary>GUID_MetadataFormatApp1: the JPEG APP1 segment with the EXIF data</summary>
    public static readonly Guid App1 = new Guid("8FD3DFC3-F951-492B-817F-69C2E6D9A5B0");

    /// <summary>GUID_MetadataFormatIfd: a TIFF image file directory</summary>
    public static readonly Guid Ifd = new Guid("537396C6-2D8A-4BB6-9BF8-2F0A8E2A3ADF");

    /// <summary>GUID_MetadataFormatExif: the EXIF sub IFD</summary>
    public static readonly Guid Exif = new Guid("1C3C4F9D-B84A-467D-9493-36CFBD59EA57");

    /// <summary>GUID_MetadataFormatGps: the GPS sub IFD</summary>
    public static readonly Guid Gps = new Guid("7134AB8A-9351-44AD-AF62-448DB6B502EC");

    /// <summary>GUID_MetadataFormatXMP: an XMP packet</summary>
    public static readonly Guid Xmp = new Guid("BB5ACC38-F216-4CEC-A6C5-5F6E739763A9");

    /// <summary>GUID_MetadataFormatIPTC</summary>
    public static readonly Guid Iptc = new Guid("4FAB0914-E129-4087-A1D1-BC812D45A7B5");

    /// <summary>GUID_MetadataFormatApp13: the JPEG APP13 segment (Photoshop, IPTC)</summary>
    public static readonly Guid App13 = new Guid("326556A2-F502-4354-9CC0-8E3F48EAF6B5");

    /// <summary>GUID_MetadataFormatChunktEXt: a PNG tEXt chunk</summary>
    public static readonly Guid PngText = new Guid("568D8936-C0A9-4923-905D-DF2B38238FBC");

    /// <summary>GUID_MetadataFormatChunkiTXt: a PNG iTXt chunk</summary>
    public static readonly Guid PngInternationalText = new Guid("C2BEC729-0B68-4B77-AA0E-6295A6AC1814");

    /// <summary>GUID_MetadataFormatChunktIME: the PNG tIME chunk</summary>
    public static readonly Guid PngTime = new Guid("6B00AE2D-E24B-460A-98B6-878BD03072FD");

    /// <summary>GUID_MetadataFormatHeif: the HEIF properties</summary>
    public static readonly Guid Heif = new Guid("817EF3E1-1288-45F4-A852-260D9E7CCE83");

    /// <summary>GUID_MetadataFormatUnknown: a block WIC has no reader for, the bytes are kept as they are</summary>
    public static readonly Guid Unknown = new Guid("A45E592F-9078-4A7C-ADB5-4EDC4FD61B1F");
}
