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

    public static bool TryDecode(byte[] data, long maxPixelCount, out DibImage image)
    {
        if (maxPixelCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPixelCount), maxPixelCount, "The maximum pixel count must be positive.");
        }
        image = null;
        if (data == null || data.Length < CoreHeaderSize)
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

        uint[] palette = null;
        if (bitCount <= 8)
        {
            var maxColors = 1 << bitCount;
            var colors = colorsUsed == 0 || colorsUsed > maxColors ? maxColors : (int)colorsUsed;
            if (data.Length < offset + colors * paletteEntrySize)
            {
                return false;
            }
            palette = new uint[maxColors];
            for (var i = 0; i < colors; i++)
            {
                var entry = offset + i * paletteEntrySize;
                // RGBQUAD: blue, green, red, reserved; RGBTRIPLE: blue, green, red
                palette[i] = data[entry] | ((uint)data[entry + 1] << 8) | ((uint)data[entry + 2] << 16) | 0xFF000000;
            }
            offset += colors * paletteEntrySize;
        }

        // Rows are padded to 4 bytes. Compared by division, so the check can't overflow whatever the header says.
        var sourceStride = ((long)width * bitCount + 31) / 32 * 4;
        long available = data.Length - offset;
        if (available < 0 || sourceStride > available / height)
        {
            return false;
        }
        var stride = (int)sourceStride;

        var pixels = new byte[pixelCount * 4];
        var redChannel = new Channel(redMask);
        var greenChannel = new Channel(greenMask);
        var blueChannel = new Channel(blueMask);
        var alphaChannel = new Channel(alphaMask);
        var anyAlphaValue = false;
        var anyTransparent = false;

        for (var y = 0; y < height; y++)
        {
            var sourceRow = offset + (isTopDown ? y : height - 1 - y) * stride;
            var targetRow = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var target = targetRow + x * 4;
                byte b, g, r, a = 255;
                if (usesMasks)
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
                    var index = ReadIndex(data, sourceRow, x, bitCount);
                    var color = palette[index];
                    b = (byte)color;
                    g = (byte)(color >> 8);
                    r = (byte)(color >> 16);
                }
                pixels[target] = b;
                pixels[target + 1] = g;
                pixels[target + 2] = r;
                pixels[target + 3] = a;
            }
        }

        var hasAlpha = alphaMask != 0 && anyTransparent;
        if (alphaIsGuess && !anyAlphaValue)
        {
            // 32 bpp BI_RGB with an unused (zero) fourth byte: opaque
            for (var i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }
            hasAlpha = false;
        }
        image = new DibImage(width, height, pixels, hasAlpha);
        return true;
    }

    public static byte[] Encode(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha, bool isV5)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be positive.");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be positive.");
        }
        if (stride < width * 4)
        {
            throw new ArgumentOutOfRangeException(nameof(stride), stride, "The stride must be at least width * 4.");
        }
        if (bgra32.Length < (long)stride * (height - 1) + width * 4)
        {
            throw new ArgumentException("The pixel data is smaller than stride * height.", nameof(bgra32));
        }
        var headerSize = isV5 ? V5HeaderSize : InfoHeaderSize;
        var imageSize = width * 4 * height;
        var result = new byte[headerSize + imageSize];

        WriteUInt32(result, 0, (uint)headerSize);
        WriteInt32(result, 4, width);
        // Positive height: bottom-up, which every reader supports
        WriteInt32(result, 8, height);
        WriteUInt16(result, 12, 1);
        WriteUInt16(result, 14, 32);
        WriteUInt32(result, 16, isV5 ? BiBitfields : BiRgb);
        WriteUInt32(result, 20, (uint)imageSize);
        if (isV5)
        {
            WriteUInt32(result, 40, 0x00FF0000);
            WriteUInt32(result, 44, 0x0000FF00);
            WriteUInt32(result, 48, 0x000000FF);
            WriteUInt32(result, 52, 0xFF000000);
            WriteUInt32(result, 56, LcsSrgb);
            WriteUInt32(result, 108, LcsGmImages);
        }

        for (var y = 0; y < height; y++)
        {
            var source = bgra32.Slice(y * stride, width * 4);
            var targetRow = headerSize + (height - 1 - y) * width * 4;
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
                result[targetRow + x] = b;
                result[targetRow + x + 1] = g;
                result[targetRow + x + 2] = r;
                result[targetRow + x + 3] = a;
            }
        }
        return result;
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
    private static bool HasRepeatedMasks(byte[] data, int offset, uint red, uint green, uint blue)
    {
        if (data.Length < offset + 12 || (red | green | blue) == 0)
        {
            return false;
        }
        if (ReadUInt32(data, offset) == red && ReadUInt32(data, offset + 4) == green && ReadUInt32(data, offset + 8) == blue)
        {
            return true;
        }
        var reversed = new byte[12];
        for (var i = 0; i < 12; i++)
        {
            reversed[i] = data[offset + 11 - i];
        }
        return ReadUInt32(reversed, 0) == red && ReadUInt32(reversed, 4) == green && ReadUInt32(reversed, 8) == blue;
    }

    private static int ReadIndex(byte[] data, int rowOffset, int x, int bitCount)
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

    private static uint ReadUInt32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
    private static int ReadInt32(byte[] data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
    private static ushort ReadUInt16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
    private static void WriteUInt32(byte[] data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);
    private static void WriteInt32(byte[] data, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);
    private static void WriteUInt16(byte[] data, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, 2), value);
}
