// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Encoding DIBs into a caller-provided span (the path which writes directly into the clipboard memory) gives the same bytes as
/// <see cref="DibImage.CreateDib"/> / <see cref="DibImage.CreateDibV5"/>. These don't use the clipboard.
/// </summary>
public class DibEncodeTests
{
    private delegate void EncodeIntoSpan(ReadOnlySpan<byte> bgra32, int width, int height, int stride, bool premultipliedAlpha, bool isV5, Span<byte> destination);

    private const int Width = 7;
    private const int Height = 5;

    // The internal DibCodec, bound to a delegate: reflection can't pass spans, a delegate can
    private static readonly Type DibCodec = typeof(DibImage).Assembly.GetType("Dapplo.Windows.Clipboard.Internals.DibCodec", true);

    private static readonly EncodeIntoSpan EncodeInto = (EncodeIntoSpan)Delegate.CreateDelegate(typeof(EncodeIntoSpan),
        DibCodec.GetMethod("Encode", BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(ReadOnlySpan<byte>), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(Span<byte>) }, null));

    private static int GetEncodedSize(int length, int width, int height, int stride, bool isV5) =>
        (int)DibCodec.GetMethod("GetEncodedSize", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { length, width, height, stride, isV5 });

    /// <summary>
    /// Pixels with transparent, half transparent and opaque pixels; for premultiplied alpha every color is at most its alpha.
    /// The padding of a larger stride is filled with garbage, it must not end up in the DIB.
    /// </summary>
    internal static byte[] CreatePixels(int stride, bool premultipliedAlpha)
    {
        var pixels = Enumerable.Repeat((byte)0xAB, stride * Height).ToArray();
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var i = y * stride + x * 4;
                var alpha = (byte)(x == 0 ? 0 : x == Width - 1 ? 255 : 60 + y * 30 + x);
                byte Color(int value) => premultipliedAlpha ? (byte)(value * alpha / 255) : (byte)value;
                pixels[i] = Color(x * 30);
                pixels[i + 1] = Color(y * 50);
                pixels[i + 2] = Color(200);
                pixels[i + 3] = alpha;
            }
        }
        return pixels;
    }

    /// <summary>
    /// A separate, straightforward encoder: header fields, bottom-up rows, straight alpha
    /// </summary>
    internal static byte[] Reference(byte[] pixels, int width, int height, int stride, bool premultipliedAlpha, bool isV5)
    {
        var headerSize = isV5 ? 124 : 40;
        var result = new byte[headerSize + width * height * 4];
        void Put(int offset, uint value) => BitConverter.GetBytes(value).CopyTo(result, offset);
        Put(0, (uint)headerSize);
        Put(4, (uint)width);
        Put(8, (uint)height);
        result[12] = 1;
        result[14] = 32;
        Put(16, isV5 ? 3u : 0u);
        Put(20, (uint)(width * height * 4));
        if (isV5)
        {
            Put(40, 0x00FF0000);
            Put(44, 0x0000FF00);
            Put(48, 0x000000FF);
            Put(52, 0xFF000000);
            Put(56, 0x73524742);
            Put(108, 4);
        }
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var source = y * stride + x * 4;
                var target = headerSize + ((height - 1 - y) * width + x) * 4;
                var alpha = pixels[source + 3];
                for (var c = 0; c < 3; c++)
                {
                    int value = pixels[source + c];
                    if (premultipliedAlpha && alpha != 255)
                    {
                        value = alpha == 0 ? 0 : Math.Min(255, (value * 255 + alpha / 2) / alpha);
                    }
                    result[target + c] = (byte)value;
                }
                result[target + 3] = alpha;
            }
        }
        return result;
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 12)]
    [InlineData(false, true, 12)]
    [InlineData(true, false, 12)]
    [InlineData(true, true, 12)]
    public void EncodeIntoSpan_IsByteIdenticalToCreateDib(bool premultipliedAlpha, bool isV5, int extraStride)
    {
        var stride = Width * 4 + extraStride;
        var pixels = CreatePixels(stride, premultipliedAlpha);
        var expected = isV5
            ? DibImage.CreateDibV5(pixels, Width, Height, stride, premultipliedAlpha)
            : DibImage.CreateDib(pixels, Width, Height, stride, premultipliedAlpha);
        var size = GetEncodedSize(pixels.Length, Width, Height, stride, isV5);
        Assert.Equal(expected.Length, size);
        // Not zeroed and larger than needed, like reused memory: the header must still be complete and nothing after it touched
        var destination = Enumerable.Repeat((byte)0xCD, size + 16).ToArray();

        EncodeInto(pixels, Width, Height, stride, premultipliedAlpha, isV5, destination);

        Assert.Equal(expected, destination.Take(size).ToArray());
        Assert.Equal(Reference(pixels, Width, Height, stride, premultipliedAlpha, isV5), expected);
        Assert.All(destination.Skip(size), b => Assert.Equal(0xCD, b));
    }

    [Fact]
    public void EncodeIntoSpan_DestinationTooSmall_Throws()
    {
        var pixels = CreatePixels(Width * 4, false);
        var size = GetEncodedSize(pixels.Length, Width, Height, Width * 4, true);
        Assert.Throws<ArgumentException>(() => EncodeInto(pixels, Width, Height, Width * 4, false, true, new byte[size - 1]));
    }

    [Fact]
    public void AddDib_ReadOnlyMemory_ChecksTheArgumentsNow_AndAddsBothFormats()
    {
        var pixels = new ReadOnlyMemory<byte>(CreatePixels(Width * 4, false));

        var contents = new ClipboardContents().AddDib(pixels, Width, Height, Width * 4, false);

        Assert.Equal(new[] { (uint)StandardClipboardFormats.DeviceIndependentBitmapV5, (uint)StandardClipboardFormats.DeviceIndependentBitmap }, contents.FormatIds);
        Assert.Equal(new[] { (uint)StandardClipboardFormats.DeviceIndependentBitmap },
            new ClipboardContents().AddDib(pixels, Width, Height, Width * 4, false, DibFormats.Dib).FormatIds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClipboardContents().AddDib(pixels, Width, Height, Width * 4 - 1, false));
        Assert.Throws<ArgumentException>(() => new ClipboardContents().AddDib(pixels.Slice(4), Width, Height, Width * 4, false));
    }

    [Fact]
    public void AddDib_ByteArray_StillEncodesNow()
    {
        var pixels = CreatePixels(Width * 4, false);
        // The byte array overload keeps the eager behavior: the array can be reused right away
        var contents = new ClipboardContents().AddDib(pixels, Width, Height, Width * 4, false);
        Array.Clear(pixels, 0, pixels.Length);
        Assert.Equal(2, contents.FormatIds.Count);
    }
}
