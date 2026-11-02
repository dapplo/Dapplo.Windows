// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The WIC container format GUIDs (GUID_ContainerFormat* of wincodec.h), compare with <see cref="WicCodecInfo.ContainerFormat"/> or use for <see cref="WicEncodeOptions.ContainerFormat"/>.
/// The codecs for HEIF (also AVIF), WebP, Raw and JPEG XL are Store extensions, which might not be installed: check with <see cref="WicCodecs"/>.
/// </summary>
public static class WicContainerFormats
{
    /// <summary>GUID_ContainerFormatBmp</summary>
    public static readonly Guid Bmp = new Guid("0AF1D87E-FCFE-4188-BDEB-A7906471CBE3");

    /// <summary>GUID_ContainerFormatPng</summary>
    public static readonly Guid Png = new Guid("1B7CFAF4-713F-473C-BBCD-6137425FAEAF");

    /// <summary>GUID_ContainerFormatIco</summary>
    public static readonly Guid Ico = new Guid("A3A860C4-338F-4C17-919A-FBA4B5628F21");

    /// <summary>GUID_ContainerFormatJpeg</summary>
    public static readonly Guid Jpeg = new Guid("19E4A5AA-5662-4FC5-A0C0-1758028E1057");

    /// <summary>GUID_ContainerFormatTiff</summary>
    public static readonly Guid Tiff = new Guid("163BCC30-E2E9-4F0B-961D-A3E9FDB788A3");

    /// <summary>GUID_ContainerFormatGif</summary>
    public static readonly Guid Gif = new Guid("1F8A5601-7D4D-4CBD-9C82-1BC8D4EEB9A5");

    /// <summary>GUID_ContainerFormatWmp: JPEG XR (HD Photo, Windows Media Photo)</summary>
    public static readonly Guid Wmp = new Guid("57A37CAA-367A-4540-916B-F183C5093A4B");

    /// <summary>GUID_ContainerFormatDds</summary>
    public static readonly Guid Dds = new Guid("9967CB95-2E85-4AC8-8CA2-83D7CCD425C9");

    /// <summary>GUID_ContainerFormatAdng: DNG</summary>
    public static readonly Guid Adng = new Guid("F3FF6D0D-38C0-41C4-B1FE-1F3824F17B84");

    /// <summary>GUID_ContainerFormatHeif: HEIF, also AVIF (Store extension)</summary>
    public static readonly Guid Heif = new Guid("E1E62521-6787-405B-A339-500715B5763F");

    /// <summary>GUID_ContainerFormatWebp (Store extension)</summary>
    public static readonly Guid Webp = new Guid("E094B0E2-67F2-45B3-B0EA-115337CA7CF3");

    /// <summary>GUID_ContainerFormatRaw (Store extension)</summary>
    public static readonly Guid Raw = new Guid("FE99CE60-F19C-433C-A3AE-00ACEFA9CA21");

    /// <summary>GUID_ContainerFormatJpegXL (Store extension)</summary>
    public static readonly Guid JpegXl = new Guid("FEC14E3F-427A-4736-AAE6-27ED84F69322");
}
