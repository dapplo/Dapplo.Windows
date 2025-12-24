// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// Represents the GRPICONDIRENTRY structure used in resource files.
/// This differs from ICONDIRENTRY in that it uses a resource ID instead of a file offset.
/// See <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The format of icon resources</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GrpIconDirEntry
{
    /// <summary>
    /// Width of the image in pixels (0-255, 0 means 256)
    /// </summary>
    public byte Width;

    /// <summary>
    /// Height of the image in pixels (0-255, 0 means 256)
    /// </summary>
    public byte Height;

    /// <summary>
    /// Number of colors in the palette (0 means no palette)
    /// </summary>
    public byte ColorCount;

    /// <summary>
    /// Reserved, must be 0
    /// </summary>
    public byte Reserved;

    /// <summary>
    /// Color planes (0 or 1 for icons)
    /// For cursors, this is the horizontal coordinate of the hotspot
    /// </summary>
    public ushort Planes;

    /// <summary>
    /// Bits per pixel
    /// For cursors, this is the vertical coordinate of the hotspot
    /// </summary>
    public ushort BitCount;

    /// <summary>
    /// Size of the image data in bytes
    /// </summary>
    public uint BytesInRes;

    /// <summary>
    /// Resource ID of the icon image (instead of file offset used in ICONDIRENTRY)
    /// </summary>
    public ushort Id;

    /// <summary>
    /// Size of the GRPICONDIRENTRY structure in bytes
    /// </summary>
    public const int Size = 14;

    /// <summary>
    /// Creates a new GRPICONDIRENTRY for an icon resource
    /// </summary>
    /// <param name="width">Width in pixels (1-256)</param>
    /// <param name="height">Height in pixels (1-256)</param>
    /// <param name="bitCount">Bits per pixel (typically 32 for modern icons)</param>
    /// <param name="imageSize">Size of the image data in bytes</param>
    /// <param name="resourceId">Resource ID of the icon image</param>
    /// <returns>GrpIconDirEntry structure</returns>
    public static GrpIconDirEntry CreateForIcon(int width, int height, ushort bitCount, uint imageSize, ushort resourceId)
    {
        return new GrpIconDirEntry
        {
            Width = width == 256 ? (byte)0 : (byte)width,
            Height = height == 256 ? (byte)0 : (byte)height,
            ColorCount = 0, // 0 for PNG and modern formats
            Reserved = 0,
            Planes = 0, // 0 or 1 for icons
            BitCount = bitCount,
            BytesInRes = imageSize,
            Id = resourceId
        };
    }

    /// <summary>
    /// Creates a new GRPICONDIRENTRY for a cursor resource
    /// </summary>
    /// <param name="width">Width in pixels (1-256)</param>
    /// <param name="height">Height in pixels (1-256)</param>
    /// <param name="hotspotX">Horizontal coordinate of the hotspot</param>
    /// <param name="hotspotY">Vertical coordinate of the hotspot</param>
    /// <param name="bitCount">Bits per pixel</param>
    /// <param name="imageSize">Size of the image data in bytes</param>
    /// <param name="resourceId">Resource ID of the cursor image</param>
    /// <returns>GrpIconDirEntry structure</returns>
    public static GrpIconDirEntry CreateForCursor(int width, int height, ushort hotspotX, ushort hotspotY, ushort bitCount, uint imageSize, ushort resourceId)
    {
        return new GrpIconDirEntry
        {
            Width = width == 256 ? (byte)0 : (byte)width,
            Height = height == 256 ? (byte)0 : (byte)height,
            ColorCount = 0,
            Reserved = 0,
            Planes = hotspotX, // For cursors, Planes is hotspot X
            BitCount = hotspotY, // For cursors, BitCount is hotspot Y
            BytesInRes = imageSize,
            Id = resourceId
        };
    }
}
