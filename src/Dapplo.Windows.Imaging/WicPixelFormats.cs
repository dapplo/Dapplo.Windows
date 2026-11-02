// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The WIC pixel format GUIDs (GUID_WICPixelFormat* of wincodec.h) which are used most.
/// Any other WIC pixel format GUID can be used too.
/// </summary>
public static class WicPixelFormats
{
    /// <summary>
    /// GUID_WICPixelFormat32bppBGRA: B, G, R, A bytes, straight alpha. The same layout as GDI+ Format32bppArgb and ImageSharp Bgra32.
    /// </summary>
    public static readonly Guid Format32bppBgra = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC90F");

    /// <summary>
    /// GUID_WICPixelFormat32bppPBGRA: B, G, R, A bytes, premultiplied alpha. The same layout as GDI+ Format32bppPArgb.
    /// </summary>
    public static readonly Guid Format32bppPbgra = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC910");

    /// <summary>
    /// GUID_WICPixelFormat32bppBGR: B, G, R bytes and an unused byte. The same layout as GDI+ Format32bppRgb.
    /// </summary>
    public static readonly Guid Format32bppBgr = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC90E");

    /// <summary>
    /// GUID_WICPixelFormat24bppBGR: B, G, R bytes. The same layout as GDI+ Format24bppRgb and ImageSharp Bgr24.
    /// </summary>
    public static readonly Guid Format24bppBgr = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC90C");

    /// <summary>
    /// GUID_WICPixelFormat24bppRGB: R, G, B bytes. The same layout as ImageSharp Rgb24.
    /// </summary>
    public static readonly Guid Format24bppRgb = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC90D");

    /// <summary>
    /// GUID_WICPixelFormat32bppRGBA: R, G, B, A bytes, straight alpha. The same layout as ImageSharp Rgba32.
    /// </summary>
    public static readonly Guid Format32bppRgba = new Guid("F5C7AD2D-6A8D-43DD-A7A8-A29935261AE9");

    /// <summary>
    /// GUID_WICPixelFormat64bppRGBA: 16 bit R, G, B, A, straight alpha.
    /// </summary>
    public static readonly Guid Format64bppRgba = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC916");

    /// <summary>
    /// GUID_WICPixelFormat64bppRGBAHalf: 16 bit floating point (half) R, G, B, A, for HDR.
    /// </summary>
    public static readonly Guid Format64bppRgbaHalf = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC93A");

    /// <summary>
    /// GUID_WICPixelFormat128bppRGBAFloat: 32 bit floating point R, G, B, A, for HDR.
    /// </summary>
    public static readonly Guid Format128bppRgbaFloat = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC919");
}
