// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Buffers.Binary;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Reads and writes device independent bitmaps without System.Drawing
/// </summary>
internal static class DibCodec
{
    private const uint BiRgb = 0;
    private const uint BiBitfields = 3;
    private const uint BiAlphaBitfields = 6;
    private const int CoreHeaderSize = 12;
    private const int InfoHeaderSize = 40;
    private const int V5HeaderSize = 124;
    // LCS_sRGB = 'sRGB'
    private const uint LcsSrgb = 0x73524742;
    // LCS_GM_IMAGES
    private const uint LcsGmImages = 4;
    // The largest byte array the runtime allows (Array.MaxLength), the decoded pixels must fit in one
    private const long MaxArrayLength = 0x7FFFFFC7;

    /// <summary>
    /// What the header says about the pixels, validated against the length of the data
    /// </summary>
    private struct Header
    {
        public int Width;
        public int Height;
        public bool IsTopDown;
        public int BitCount;
        public bool UsesMasks;
        public bool AlphaIsGuess;
        public uint RedMask;
        public uint GreenMask;
        public uint BlueMask;
        public uint AlphaMask;
        public int PaletteOffset;
        public int PaletteColors;
        public int PaletteEntrySize;
        public int PixelOffset;
        public int Stride;
    }

    public static bool TryDecode(byte[] data, long maxPixelCount, out DibImage image)
    {
        ThrowWhenNotPositive(maxPixelCount);
        image = null;
        if (data == null || !TryParseHeader(data, maxPixelCount, out var header))
        {
            return false;
        }
        var pixels = new byte[(long)header.Width * header.Height * 4];
        DecodePixels(data, header, pixels, header.Width * 4, out var hasAlpha);
        image = new DibImage(header.Width, header.Height, pixels, hasAlpha);
        return true;
    }

    /// <summary>
    /// Validate the header and tell the size and whether the decoded image has alpha, without decoding (or allocating) the pixels
    /// </summary>
    public static bool TryReadInfo(ReadOnlySpan<byte> data, long maxPixelCount, out int width, out int height, out bool hasAlpha)
    {
        ThrowWhenNotPositive(maxPixelCount);
        width = height = 0;
        hasAlpha = false;
        if (!TryParseHeader(data, maxPixelCount, out var header))
        {
            return false;
        }
        width = header.Width;
        height = header.Height;
        hasAlpha = HasAlpha(data, header);
        return true;
    }

    /// <summary>
    /// Decode into caller-provided memory: top-down rows of BGRA32 with straight alpha, <paramref name="destinationStride"/> bytes apart
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> data, long maxPixelCount, Span<byte> destination, int destinationStride)
    {
        ThrowWhenNotPositive(maxPixelCount);
        if (!TryParseHeader(data, maxPixelCount, out var header))
        {
            return false;
        }
        if (destinationStride < (long)header.Width * 4 || destination.Length < (long)destinationStride * (header.Height - 1) + (long)header.Width * 4)
        {
            return false;
        }
        DecodePixels(data, header, destination, destinationStride, out _);
        return true;
    }

    private static void ThrowWhenNotPositive(long maxPixelCount)
    {
        if (maxPixelCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPixelCount), maxPixelCount, "The maximum pixel count must be positive.");
        }
    }

    private static bool TryParseHeader(ReadOnlySpan<byte> data, long maxPixelCount, out Header header)
    {
        header = default;
        if (data.Length < CoreHeaderSize)
        {
            return false;
        }
        var headerSize = ReadUInt32(data, 0);
        int width;
        int rawHeight;
        int bitCount;
        uint compression;
        uint colorsUsed;
        int paletteEntrySize;
        if (headerSize == CoreHeaderSize)
        {
            // BITMAPCOREHEADER: unsigned 16-bit width and height (always bottom-up), 1, 4, 8 or 24 bpp, RGBTRIPLE palette
            width = ReadUInt16(data, 4);
            rawHeight = ReadUInt16(data, 6);
            bitCount = ReadUInt16(data, 10);
            compression = BiRgb;
            colorsUsed = 0;
            paletteEntrySize = 3;
            if (bitCount != 1 && bitCount != 4 && bitCount != 8 && bitCount != 24)
            {
                return false;
            }
        }
        else
        {
            if (headerSize != 40 && headerSize != 52 && headerSize != 56 && headerSize != 108 && headerSize != 124)
            {
                return false;
            }
            if (data.Length < headerSize)
            {
                return false;
            }
            width = ReadInt32(data, 4);
            rawHeight = ReadInt32(data, 8);
            bitCount = ReadUInt16(data, 14);
            compression = ReadUInt32(data, 16);
            colorsUsed = ReadUInt32(data, 32);
            paletteEntrySize = 4;
        }
        if (width <= 0 || rawHeight == 0 || rawHeight == int.MinValue)
        {
            return false;
        }
        var isTopDown = rawHeight < 0;
        var height = Math.Abs(rawHeight);
        // Checked from the header, before anything is allocated. Both are below 2^31, so the product can't overflow a long.
        var pixelCount = (long)width * height;
        if (pixelCount > maxPixelCount || pixelCount > MaxArrayLength / 4)
        {
            return false;
        }

        var offset = (int)headerSize;
        uint redMask = 0, greenMask = 0, blueMask = 0, alphaMask = 0;
        var usesMasks = false;
        var alphaIsGuess = false;
        switch (compression)
        {
            case BiBitfields:
            case BiAlphaBitfields:
                if (bitCount != 16 && bitCount != 32)
                {
                    return false;
                }
                usesMasks = true;
                if (headerSize >= 52)
                {
                    redMask = ReadUInt32(data, 40);
                    greenMask = ReadUInt32(data, 44);
                    blueMask = ReadUInt32(data, 48);
                    if (headerSize >= 56)
                    {
                        alphaMask = ReadUInt32(data, 52);
                    }
                }
                else
                {
                    // BITMAPINFOHEADER: the masks follow the header
                    var maskBytes = compression == BiAlphaBitfields ? 16 : 12;
                    if (data.Length < offset + maskBytes)
                    {
                        return false;
                    }
                    redMask = ReadUInt32(data, offset);
                    greenMask = ReadUInt32(data, offset + 4);
                    blueMask = ReadUInt32(data, offset + 8);
                    if (compression == BiAlphaBitfields)
                    {
                        alphaMask = ReadUInt32(data, offset + 12);
                    }
                    offset += maskBytes;
                }
                // Masks repeated after a V4 / V5 header
                if (headerSize >= 108 && HasRepeatedMasks(data, offset, redMask, greenMask, blueMask))
                {
                    offset += 12;
                }
                break;
            case BiRgb:
                switch (bitCount)
                {
                    case 32:
                        usesMasks = true;
                        redMask = 0x00FF0000;
                        greenMask = 0x0000FF00;
                        blueMask = 0x000000FF;
                        alphaMask = 0xFF000000;
                        alphaIsGuess = true;
                        break;
                    case 16:
                        usesMasks = true;
                        redMask = 0x7C00;
                        greenMask = 0x03E0;
                        blueMask = 0x001F;
                        break;
                    case 24:
                    case 8:
                    case 4:
                    case 1:
                        break;
                    default:
                        return false;
                }
                break;
            default:
                // RLE, JPEG, PNG and CMYK variants are not supported
                return false;
        }

        var paletteOffset = offset;
        var paletteColors = 0;
        if (bitCount <= 8)
        {
            var maxColors = 1 << bitCount;
            paletteColors = colorsUsed == 0 || colorsUsed > maxColors ? maxColors : (int)colorsUsed;
            if (data.Length < offset + paletteColors * paletteEntrySize)
            {
                return false;
            }
            offset += paletteColors * paletteEntrySize;
        }

        // Rows are padded to 4 bytes. Compared by division, so the check can't overflow whatever the header says.
        var sourceStride = ((long)width * bitCount + 31) / 32 * 4;
        long available = data.Length - offset;
        if (available < 0 || sourceStride > available / height)
        {
            return false;
        }

        header = new Header
        {
            Width = width,
            Height = height,
            IsTopDown = isTopDown,
            BitCount = bitCount,
            UsesMasks = usesMasks,
            AlphaIsGuess = alphaIsGuess,
            RedMask = redMask,
            GreenMask = greenMask,
            BlueMask = blueMask,
            AlphaMask = alphaMask,
            PaletteOffset = paletteOffset,
            PaletteColors = paletteColors,
            PaletteEntrySize = paletteEntrySize,
            PixelOffset = offset,
            Stride = (int)sourceStride
        };
        return true;
    }

    /// <summary>
    /// Whether the decoded image has alpha, the same answer as DecodePixels gives: an alpha mask and a pixel which isn't opaque;
    /// for 32 bpp BI_RGB (alpha is a guess) also a pixel with a non-zero fourth byte
    /// </summary>
    private static bool HasAlpha(ReadOnlySpan<byte> data, in Header header)
    {
        if (header.AlphaMask == 0)
        {
            return false;
        }
        var alphaChannel = new Channel(header.AlphaMask);
        var anyAlphaValue = false;
        var anyTransparent = false;
        for (var y = 0; y < header.Height; y++)
        {
            var sourceRow = header.PixelOffset + y * header.Stride;
            for (var x = 0; x < header.Width; x++)
            {
                var value = header.BitCount == 32 ? ReadUInt32(data, sourceRow + x * 4) : ReadUInt16(data, sourceRow + x * 2);
                var a = alphaChannel.Extract(value);
                anyAlphaValue |= a != 0;
                anyTransparent |= a != 255;
                if (anyTransparent && (anyAlphaValue || !header.AlphaIsGuess))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Decode the pixels into top-down BGRA32 rows with straight alpha, the destination was checked by the caller
    /// </summary>
    private static void DecodePixels(ReadOnlySpan<byte> data, in Header header, Span<byte> destination, int destinationStride, out bool hasAlpha)
    {
        var width = header.Width;
        var height = header.Height;
        var bitCount = header.BitCount;
        var stride = header.Stride;

        // At most 256 entries; entries after the colors of the palette are black, as before
        Span<uint> palette = bitCount <= 8 ? stackalloc uint[256] : Span<uint>.Empty;
        if (bitCount <= 8)
        {
            palette.Clear();
            for (var i = 0; i < header.PaletteColors; i++)
            {
                var entry = header.PaletteOffset + i * header.PaletteEntrySize;
                // RGBQUAD: blue, green, red, reserved; RGBTRIPLE: blue, green, red
                palette[i] = data[entry] | ((uint)data[entry + 1] << 8) | ((uint)data[entry + 2] << 16) | 0xFF000000;
            }
        }

        var redChannel = new Channel(header.RedMask);
        var greenChannel = new Channel(header.GreenMask);
        var blueChannel = new Channel(header.BlueMask);
        var alphaChannel = new Channel(header.AlphaMask);
        var alphaMask = header.AlphaMask;
        var anyAlphaValue = false;
        var anyTransparent = false;

        for (var y = 0; y < height; y++)
        {
            var sourceRow = header.PixelOffset + (header.IsTopDown ? y : height - 1 - y) * stride;
            var target = destination.Slice(y * destinationStride, width * 4);
            for (var x = 0; x < width; x++)
            {
                byte b, g, r, a = 255;
                if (header.UsesMasks)
                {
                    var value = bitCount == 32 ? ReadUInt32(data, sourceRow + x * 4) : ReadUInt16(data, sourceRow + x * 2);
                    b = blueChannel.Extract(value);
                    g = greenChannel.Extract(value);
                    r = redChannel.Extract(value);
                    if (alphaMask != 0)
                    {
                        a = alphaChannel.Extract(value);
                        anyAlphaValue |= a != 0;
                        anyTransparent |= a != 255;
                    }
                }
                else if (bitCount == 24)
                {
                    var source = sourceRow + x * 3;
                    b = data[source];
                    g = data[source + 1];
                    r = data[source + 2];
                }
                else
                {
                    var color = palette[ReadIndex(data, sourceRow, x, bitCount)];
                    b = (byte)color;
                    g = (byte)(color >> 8);
                    r = (byte)(color >> 16);
                }
                target[x * 4] = b;
                target[x * 4 + 1] = g;
                target[x * 4 + 2] = r;
                target[x * 4 + 3] = a;
            }
        }

        hasAlpha = alphaMask != 0 && anyTransparent;
        if (header.AlphaIsGuess && !anyAlphaValue)
        {
            // 32 bpp BI_RGB with an unused (zero) fourth byte: opaque
            for (var y = 0; y < height; y++)
            {
                var target = destination.Slice(y * destinationStride, width * 4);
                for (var i = 3; i < target.Length; i += 4)
                {
                    target[i] = 255;
                }
            }
            hasAlpha = false;
        }
    }

    public static byte[] Encode(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha, bool isV5)
    {
        var result = new byte[GetEncodedSize(bgra32.Length, width, height, stride, isV5)];
        Encode(bgra32, width, height, stride, premultipliedAlpha, isV5, result);
        return result;
    }

    /// <summary>
    /// Validate the arguments and calculate the size of the encoded DIB (header and pixels)
    /// </summary>
    /// <param name="bgra32Length">int with the length of the pixel data</param>
    /// <param name="width">int with the width</param>
    /// <param name="height">int with the height</param>
    /// <param name="stride">int with the bytes per row of the pixel data</param>
    /// <param name="isV5">true for a BITMAPV5HEADER, false for a BITMAPINFOHEADER</param>
    /// <returns>int with the number of bytes</returns>
    public static int GetEncodedSize(int bgra32Length, int width, int height, int stride, bool isV5)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be positive.");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be positive.");
        }
        if (stride < (long)width * 4)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "The stride must be at least width * 4.");
        }
        if (bgra32Length < (long)stride * (height - 1) + (long)width * 4)
        {
            throw new ArgumentException("The pixel data is smaller than stride * height.", "bgra32");
        }
        var size = (isV5 ? V5HeaderSize : InfoHeaderSize) + (long)width * 4 * height;
        if (size > int.MaxValue)
        {
            throw new ArgumentException("The bitmap is too large for a DIB.", "bgra32");
        }
        return (int)size;
    }

    /// <summary>
    /// Encode BGRA32 pixels as a DIB into the destination, which must have at least <see cref="GetEncodedSize"/> bytes
    /// </summary>
    public static void Encode(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha, bool isV5, Span<byte> destination)
    {
        var size = GetEncodedSize(bgra32.Length, width, height, stride, isV5);
        if (destination.Length < size)
        {
            throw new ArgumentException($"The destination has {destination.Length} bytes, the DIB needs {size}.", nameof(destination));
        }
        var headerSize = isV5 ? V5HeaderSize : InfoHeaderSize;
        var imageSize = width * 4 * height;
        // The destination may not be zeroed (e.g. pooled memory), the unused header fields must be 0
        destination.Slice(0, headerSize).Clear();

        WriteUInt32(destination, 0, (uint)headerSize);
        WriteInt32(destination, 4, width);
        // Positive height: bottom-up, which every reader supports
        WriteInt32(destination, 8, height);
        WriteUInt16(destination, 12, 1);
        WriteUInt16(destination, 14, 32);
        WriteUInt32(destination, 16, isV5 ? BiBitfields : BiRgb);
        WriteUInt32(destination, 20, (uint)imageSize);
        if (isV5)
        {
            WriteUInt32(destination, 40, 0x00FF0000);
            WriteUInt32(destination, 44, 0x0000FF00);
            WriteUInt32(destination, 48, 0x000000FF);
            WriteUInt32(destination, 52, 0xFF000000);
            WriteUInt32(destination, 56, LcsSrgb);
            WriteUInt32(destination, 108, LcsGmImages);
        }

        for (var y = 0; y < height; y++)
        {
            var source = bgra32.Slice(y * stride, width * 4);
            var target = destination.Slice(headerSize + (height - 1 - y) * width * 4, width * 4);
            for (var x = 0; x < width * 4; x += 4)
            {
                var b = source[x];
                var g = source[x + 1];
                var r = source[x + 2];
                var a = source[x + 3];
                if (premultipliedAlpha && a != 255)
                {
                    b = Unpremultiply(b, a);
                    g = Unpremultiply(g, a);
                    r = Unpremultiply(r, a);
                }
                target[x] = b;
                target[x + 1] = g;
                target[x + 2] = r;
                target[x + 3] = a;
            }
        }
    }

    private static byte Unpremultiply(byte value, byte alpha)
    {
        if (alpha == 0)
        {
            return 0;
        }
        var straight = (value * 255 + alpha / 2) / alpha;
        return (byte)(straight > 255 ? 255 : straight);
    }

    /// <summary>
    /// Some writers repeat the masks after a BITMAPV4HEADER / BITMAPV5HEADER, Greenshot writes them with all 12 bytes reversed
    /// </summary>
    private static bool HasRepeatedMasks(ReadOnlySpan<byte> data, int offset, uint red, uint green, uint blue)
    {
        if (data.Length < offset + 12 || (red | green | blue) == 0)
        {
            return false;
        }
        if (ReadUInt32(data, offset) == red && ReadUInt32(data, offset + 4) == green && ReadUInt32(data, offset + 8) == blue)
        {
            return true;
        }
        Span<byte> reversed = stackalloc byte[12];
        for (var i = 0; i < 12; i++)
        {
            reversed[i] = data[offset + 11 - i];
        }
        return ReadUInt32(reversed, 0) == red && ReadUInt32(reversed, 4) == green && ReadUInt32(reversed, 8) == blue;
    }

    private static int ReadIndex(ReadOnlySpan<byte> data, int rowOffset, int x, int bitCount)
    {
        switch (bitCount)
        {
            case 8:
                return data[rowOffset + x];
            case 4:
                var nibbles = data[rowOffset + x / 2];
                return x % 2 == 0 ? nibbles >> 4 : nibbles & 0x0F;
            default:
                var bits = data[rowOffset + x / 8];
                return (bits >> (7 - x % 8)) & 1;
        }
    }

    private readonly struct Channel
    {
        private readonly uint _mask;
        private readonly int _shift;
        private readonly uint _maximum;

        public Channel(uint mask)
        {
            _mask = mask;
            _shift = 0;
            _maximum = 0;
            if (mask == 0)
            {
                return;
            }
            while (((mask >> _shift) & 1) == 0)
            {
                _shift++;
            }
            _maximum = mask >> _shift;
        }

        public byte Extract(uint value)
        {
            if (_mask == 0)
            {
                return 0;
            }
            var channel = (value & _mask) >> _shift;
            if (_maximum == 255)
            {
                return (byte)channel;
            }
            // ulong: with a mask of more than 24 bits, channel * 255 doesn't fit in a uint
            return (byte)(((ulong)channel * 255 + _maximum / 2) / _maximum);
        }
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    private static int ReadInt32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    private static void WriteUInt32(Span<byte> data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(offset, 4), value);
    private static void WriteInt32(Span<byte> data, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(data.Slice(offset, 4), value);
    private static void WriteUInt16(Span<byte> data, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(offset, 2), value);
}
