// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// Represents the ICONDIRENTRY structure in an ICO file.
/// See <a href="https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513">The format of icon resources</a>
/// and <a href="https://en.wikipedia.org/wiki/ICO_(file_format)">ICO file format</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IconDirEntry
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
    /// Offset of the image data from the beginning of the file
    /// </summary>
    public uint ImageOffset;

    /// <summary>
    /// Size of the ICONDIRENTRY structure in bytes
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// The maximum width and height of an image in an icon or cursor
    /// </summary>
    public const int MaxImageSize = 256;

    /// <summary>
    /// The width in pixels, a Width of 0 means 256
    /// </summary>
    public int PixelWidth => Width == 0 ? MaxImageSize : Width;

    /// <summary>
    /// The height in pixels, a Height of 0 means 256
    /// </summary>
    public int PixelHeight => Height == 0 ? MaxImageSize : Height;

    /// <summary>
    /// Convert a width or height in pixels to the byte used in the directory entry, where 256 is stored as 0
    /// </summary>
    /// <param name="size">int with the size in pixels, 1-256</param>
    /// <param name="paramName">string with the name of the parameter, for the exception</param>
    /// <returns>byte</returns>
    /// <exception cref="ArgumentOutOfRangeException">when the size is not in the range 1-256</exception>
    internal static byte ToDirectorySize(int size, string paramName)
    {
        if (size < 1 || size > MaxImageSize)
        {
            throw new ArgumentOutOfRangeException(paramName, size, $"Icon and cursor images must be between 1 and {MaxImageSize} pixels.");
        }
        return size == MaxImageSize ? (byte)0 : (byte)size;
    }

    /// <summary>
    /// Creates a new ICONDIRENTRY for an icon
    /// </summary>
    /// <param name="width">Width in pixels (1-256)</param>
    /// <exception cref="ArgumentOutOfRangeException">when width or height is not in the range 1-256</exception>
    /// <param name="height">Height in pixels (1-256)</param>
    /// <param name="bitCount">Bits per pixel (typically 32 for modern icons)</param>
    /// <param name="imageSize">Size of the image data in bytes</param>
    /// <param name="imageOffset">Offset to the image data</param>
    /// <returns>IconDirEntry structure</returns>
    public static IconDirEntry CreateForIcon(int width, int height, ushort bitCount, uint imageSize, uint imageOffset)
    {
        return new IconDirEntry
        {
            Width = IconDirEntry.ToDirectorySize(width, nameof(width)),
            Height = IconDirEntry.ToDirectorySize(height, nameof(height)),
            ColorCount = 0, // 0 for PNG and modern formats
            Reserved = 0,
            Planes = 0, // 0 or 1 for icons
            BitCount = bitCount,
            BytesInRes = imageSize,
            ImageOffset = imageOffset
        };
    }

    /// <summary>
    /// Creates a new ICONDIRENTRY for a cursor
    /// </summary>
    /// <param name="width">Width in pixels (1-256)</param>
    /// <exception cref="ArgumentOutOfRangeException">when width or height is not in the range 1-256</exception>
    /// <param name="height">Height in pixels (1-256)</param>
    /// <param name="hotspotX">Horizontal coordinate of the hotspot</param>
    /// <param name="hotspotY">Vertical coordinate of the hotspot</param>
    /// <param name="imageSize">Size of the image data in bytes</param>
    /// <param name="imageOffset">Offset to the image data</param>
    /// <returns>IconDirEntry structure</returns>
    /// <remarks>
    /// For cursors, the Planes and BitCount fields are repurposed to store hotspot coordinates:
    /// - Planes field stores the horizontal hotspot coordinate (X)
    /// - BitCount field stores the vertical hotspot coordinate (Y)
    /// </remarks>
    public static IconDirEntry CreateForCursor(int width, int height, ushort hotspotX, ushort hotspotY, uint imageSize, uint imageOffset)
    {
        return new IconDirEntry
        {
            Width = IconDirEntry.ToDirectorySize(width, nameof(width)),
            Height = IconDirEntry.ToDirectorySize(height, nameof(height)),
            ColorCount = 0,
            Reserved = 0,
            Planes = hotspotX, // For cursors, Planes is hotspot X
            BitCount = hotspotY, // For cursors, BitCount is hotspot Y
            BytesInRes = imageSize,
            ImageOffset = imageOffset
        };
    }
}
