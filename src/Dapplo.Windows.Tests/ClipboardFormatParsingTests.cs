// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// CF_HTML and DIB parsing and writing, without the clipboard
/// </summary>
public class ClipboardFormatParsingTests
{
    private static byte[] Sample(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Samples", "Clipboard", name));

    // ── CF_HTML ──────────────────────────────────────────────────────────────

    [Fact]
    public void Html_Create_OffsetsAreUtf8ByteOffsets()
    {
        const string fragment = "<p>Grüße, 日本語 and emoji 😀</p>";
        var bytes = ClipboardHtml.Create(fragment, new Uri("https://example.com/ä"));
        var text = Encoding.UTF8.GetString(bytes);

        int Offset(string key)
        {
            var start = text.IndexOf(key + ":", StringComparison.Ordinal) + key.Length + 1;
            return int.Parse(text.Substring(start, 10));
        }

        Assert.Equal(fragment, Encoding.UTF8.GetString(bytes, Offset("StartFragment"), Offset("EndFragment") - Offset("StartFragment")));
        Assert.StartsWith("<html>", Encoding.UTF8.GetString(bytes, Offset("StartHTML"), 6));
        Assert.Equal(bytes.Length, Offset("EndHTML"));
        Assert.Contains("SourceURL:https://example.com/%C3%A4\r\n", text);
    }

    [Fact]
    public void Html_RoundTrip_NonAscii()
    {
        const string fragment = "<b>Grüße</b> – ✓ 日本語";
        Assert.True(ClipboardHtml.TryParse(ClipboardHtml.Create(fragment, new Uri("https://example.com/")), out var html));
        Assert.Equal(fragment, html.Fragment);
        Assert.Contains(fragment, html.FullHtml);
        Assert.Equal(new Uri("https://example.com/"), html.SourceUrl);
        Assert.Equal("0.9", html.Version);
    }

    [Theory]
    [InlineData("html-chrome-style.bin", "<h1>Überschrift</h1><p>Hello <b>wörld</b> – ✓</p>", "https://example.com/page?a=1&b=2", "0.9")]
    [InlineData("html-firefox-style.bin", "<p>Hello <i>wörld</i></p>", "https://example.org/", "0.9")]
    [InlineData("html-word-style.bin", "\r\n\r\n<p class=MsoNormal>Grüße aus <b>Word</b></p>\r\n\r\n", "file:///C:/Users/test/Documents/Brief.docx", "1.0")]
    [InlineData("html-greenshot.bin", "\r\n<img border='0' src='file:///C:/Users/test/AppData/Local/Temp/capture.png' width='20' height='10'>\r\n", null, "0.9")]
    [InlineData("html-character-offsets.bin", "<p>Ärger über Öl</p>", null, "0.9")]
    [InlineData("html-v1-no-context.bin", "<span>Only a fragment</span>", null, "1.0")]
    public void Html_ParseSamples(string file, string expectedFragment, string expectedSourceUrl, string expectedVersion)
    {
        Assert.True(ClipboardHtml.TryParse(Sample(file), out var html));
        Assert.Equal(expectedFragment, html.Fragment);
        Assert.Equal(expectedSourceUrl, html.SourceUrl?.OriginalString);
        Assert.Equal(expectedVersion, html.Version);
        Assert.Contains(expectedFragment, html.FullHtml);
    }

    [Fact]
    public void Html_FullHtml_IsTheContext()
    {
        Assert.True(ClipboardHtml.TryParse(Sample("html-word-style.bin"), out var html));
        Assert.StartsWith("<html xmlns:v=", html.FullHtml);
        Assert.EndsWith("</html>", html.FullHtml);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Just some text")]
    [InlineData("<html><body>No header</body></html>")]
    public void Html_NotCfHtml_ReturnsFalse(string text)
    {
        Assert.False(ClipboardHtml.TryParse(Encoding.UTF8.GetBytes(text), out _));
    }

    // ── DIB ──────────────────────────────────────────────────────────────────

    // The reference image of the samples: B, G, R, A, top-down
    private static readonly byte[][] Reference =
    {
        new byte[] { 0, 0, 255, 255 }, new byte[] { 0, 255, 0, 128 }, new byte[] { 255, 0, 0, 0 },
        new byte[] { 255, 255, 255, 255 }, new byte[] { 0, 0, 0, 255 }, new byte[] { 128, 128, 128, 64 }
    };

    private static void AssertReference(DibImage image, bool withAlpha)
    {
        Assert.Equal(3, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(12, image.Stride);
        Assert.Equal(withAlpha, image.HasAlpha);
        for (var i = 0; i < 6; i++)
        {
            var expected = Reference[i];
            var actual = image.Pixels.Skip(i * 4).Take(4).ToArray();
            Assert.Equal(expected.Take(3).ToArray(), actual.Take(3).ToArray());
            Assert.Equal(withAlpha ? expected[3] : (byte)255, actual[3]);
        }
    }

    [Theory]
    [InlineData("dib-24bpp-bottomup.bin", false)]
    [InlineData("dib-32bpp-bitfields.bin", false)]
    [InlineData("dib-32bpp-rgb-topdown-alpha.bin", true)]
    [InlineData("dib-32bpp-rgb-zero-alpha.bin", false)]
    [InlineData("dibv5-bitfields-alpha.bin", true)]
    [InlineData("dibv4-bitfields-alpha.bin", true)]
    [InlineData("dibv5-greenshot.bin", true)]
    [InlineData("dib-8bpp-palette.bin", false)]
    public void Dib_DecodeSamples(string file, bool withAlpha)
    {
        Assert.True(TryDecodeChecked(Sample(file), out var image));
        AssertReference(image, withAlpha);
    }

    private static byte[] ReferencePixels(int stride)
    {
        var pixels = new byte[stride * 2];
        for (var i = 0; i < 6; i++)
        {
            Array.Copy(Reference[i], 0, pixels, (i / 3) * stride + (i % 3) * 4, 4);
        }
        return pixels;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Dib_RoundTrip_WithAlphaAndStride(bool v5)
    {
        // A stride larger than width * 4, like many imaging libraries use
        var pixels = ReferencePixels(16);
        var dib = v5 ? DibImage.CreateDibV5(pixels, 3, 2, 16, false) : DibImage.CreateDib(pixels, 3, 2, 16, false);
        Assert.Equal(v5 ? 124 : 40, BitConverter.ToInt32(dib, 0));
        Assert.Equal(2, BitConverter.ToInt32(dib, 8));
        Assert.Equal(v5 ? 3 : 0, BitConverter.ToInt32(dib, 16));
        Assert.True(TryDecodeChecked(dib, out var image));
        AssertReference(image, true);
    }

    [Fact]
    public void Dib_Premultiplied_IsWrittenStraight()
    {
        // 50% transparent red, premultiplied: R = 128
        var pixels = new byte[] { 0, 0, 128, 128, 0, 0, 0, 0 };
        Assert.True(TryDecodeChecked(DibImage.CreateDibV5(pixels, 2, 1, 8, true), out var image));
        Assert.Equal(new byte[] { 0, 0, 255, 128, 0, 0, 0, 0 }, image.Pixels);
    }

    [Fact]
    public void Dib_V5Header_IsSrgbWithAlphaMask()
    {
        var dib = DibImage.CreateDibV5(new byte[4], 1, 1, 4, false);
        Assert.Equal(0xFF000000u, BitConverter.ToUInt32(dib, 52));
        Assert.Equal(0x73524742u, BitConverter.ToUInt32(dib, 56));
        Assert.Equal(124 + 4, dib.Length);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 12, 0, 0, 0 })]
    public void Dib_Invalid_ReturnsFalse(byte[] data)
    {
        Assert.False(TryDecodeChecked(data, out _));
    }

    [Fact]
    public void Dib_Truncated_ReturnsFalse()
    {
        var sample = Sample("dibv5-bitfields-alpha.bin");
        Assert.False(TryDecodeChecked(sample.Take(sample.Length - 1).ToArray(), out _));
    }

    /// <summary>
    /// Crafted header fields must be rejected or ignored, never lead to reading outside the data
    /// (see the CF_DIBV5 out-of-bounds read reported for Greenshot's DibFileFormatHandler)
    /// </summary>
    [Theory]
    [InlineData(0, 0x10000000u, false)]  // biSize far beyond the data
    [InlineData(0, 0xFFFFFFFFu, false)]  // biSize negative as int
    [InlineData(0, 64u, false)]          // biSize which is no known header
    [InlineData(20, 0xFFFFFFFFu, true)]  // biSizeImage is ignored, the stride is calculated
    [InlineData(20, 1u, true)]
    [InlineData(4, 0x7FFFFFFFu, false)]  // width
    [InlineData(8, 0x80000000u, false)]  // height int.MinValue
    [InlineData(8, 0x7FFFFFFFu, false)]  // height larger than the data
    [InlineData(32, 0xFFFFFFFFu, true)]  // biClrUsed, no palette at 32 bpp
    public void Dib_CraftedHeader_NeverReadsOutsideTheData(int offset, uint value, bool decodes)
    {
        var sample = Sample("dibv5-greenshot.bin");
        BitConverter.GetBytes(value).CopyTo(sample, offset);
        Assert.Equal(decodes, TryDecodeChecked(sample, out var image));
        if (decodes)
        {
            AssertReference(image, true);
        }
    }

    [Fact]
    public void Dib_Palette_ColorsUsedLargerThanPossible_IsClamped()
    {
        var sample = Sample("dib-8bpp-palette.bin");
        // biClrUsed 0xFFFFFFFF: the palette would reach far beyond the data
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(sample, 32);
        Assert.False(TryDecodeChecked(sample, out _));
    }

    [Fact]
    public void Dib_FullWidthMask_ScalesWithoutOverflow()
    {
        // 32 bpp BI_BITFIELDS with a 32-bit red mask: the maximum value must scale to 255
        var dib = new byte[40 + 12 + 4];
        BitConverter.GetBytes(40).CopyTo(dib, 0);
        BitConverter.GetBytes(1).CopyTo(dib, 4);
        BitConverter.GetBytes(1).CopyTo(dib, 8);
        BitConverter.GetBytes((ushort)1).CopyTo(dib, 12);
        BitConverter.GetBytes((ushort)32).CopyTo(dib, 14);
        BitConverter.GetBytes(3).CopyTo(dib, 16);
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(dib, 40);
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(dib, 52);
        Assert.True(TryDecodeChecked(dib, out var image));
        Assert.Equal(255, image.Pixels[2]);
    }

    [Fact]
    public void Dib_Compressed_ReturnsFalse()
    {
        var sample = Sample("dib-24bpp-bottomup.bin");
        // BI_PNG
        BitConverter.GetBytes(5).CopyTo(sample, 16);
        Assert.False(TryDecodeChecked(sample, out _));
    }

    // ── Span decode (3.9): every decode in these tests also checks TryReadInfo and the decode into caller memory ──

    private static bool TryDecodeChecked(byte[] dib, out DibImage image) => TryDecodeChecked(dib, DibImage.DefaultMaxPixelCount, out image);

    /// <summary>
    /// DibImage.TryDecode, and the same answer from TryReadInfo and from the decode into caller memory: the same pixels with a tight
    /// and with a larger stride (the padding untouched), false for a destination which is too small
    /// </summary>
    private static bool TryDecodeChecked(byte[] dib, long maxPixelCount, out DibImage image)
    {
        var decoded = DibImage.TryDecode(dib, maxPixelCount, out image);
        Assert.Equal(decoded, DibImage.TryReadInfo(dib, maxPixelCount, out var width, out var height, out var hasAlpha));
        if (!decoded)
        {
            Assert.False(DibImage.TryDecode(dib, maxPixelCount, new byte[1024], 64));
            return false;
        }
        Assert.Equal(image.Width, width);
        Assert.Equal(image.Height, height);
        Assert.Equal(image.HasAlpha, hasAlpha);

        var tight = Enumerable.Repeat((byte)0xCD, width * height * 4).ToArray();
        Assert.True(DibImage.TryDecode(dib, maxPixelCount, tight, width * 4));
        Assert.Equal(image.Pixels, tight);

        var stride = width * 4 + 12;
        var padded = Enumerable.Repeat((byte)0xCD, stride * height).ToArray();
        Assert.True(DibImage.TryDecode(dib, maxPixelCount, padded, stride));
        for (var y = 0; y < height; y++)
        {
            Assert.True(image.Pixels.Skip(y * width * 4).Take(width * 4).SequenceEqual(padded.Skip(y * stride).Take(width * 4)), $"Row {y} differs");
            Assert.All(padded.Skip(y * stride + width * 4).Take(12), b => Assert.Equal(0xCD, b));
        }

        Assert.False(DibImage.TryDecode(dib, maxPixelCount, new byte[tight.Length - 1], width * 4));
        Assert.False(DibImage.TryDecode(dib, maxPixelCount, tight, width * 4 - 1));
        return true;
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Dib_SpanApi_MaxPixelCount_MustBePositive(long maxPixelCount)
    {
        var sample = Sample("dib-24bpp-bottomup.bin");
        Assert.Throws<ArgumentOutOfRangeException>(() => DibImage.TryReadInfo(sample, maxPixelCount, out _, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DibImage.TryDecode(sample, maxPixelCount, new byte[24], 12));
    }

    [Fact]
    public void Dib_SpanApi_DecodesFromASnapshotBuffer()
    {
        var dib = Sample("dibv5-bitfields-alpha.bin");
        var snapshot = (ClipboardSnapshot)Activator.CreateInstance(typeof(ClipboardSnapshot), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
            new object[] { 1u, IntPtr.Zero, new[] { "CF_DIBV5" }, new System.Collections.Generic.Dictionary<string, byte[]> { ["CF_DIBV5"] = dib }, new string[0] }, null);

        Assert.True(snapshot.TryGetStream("CF_DIBV5", out var stream));
        var memoryStream = Assert.IsType<MemoryStream>(stream);
        Assert.False(memoryStream.CanWrite);
        // The snapshot's own array, no copy
        Assert.True(memoryStream.TryGetBuffer(out var buffer));
        Assert.Same(dib, buffer.Array);
        Assert.Equal(dib.Length, buffer.Count);

        var data = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
        Assert.True(DibImage.TryReadInfo(data, DibImage.DefaultMaxPixelCount, out var width, out var height, out var hasAlpha));
        Assert.True(hasAlpha);
        var pixels = new byte[width * height * 4];
        Assert.True(DibImage.TryDecode(data, DibImage.DefaultMaxPixelCount, pixels, width * 4));
        Assert.True(DibImage.TryDecode(dib, out var image));
        Assert.Equal(image.Pixels, pixels);
    }

    // ── DIB size limit (3.2) ─────────────────────────────────────────────────

    /// <summary>
    /// A BITMAPINFOHEADER followed by <paramref name="dataBytes"/> zero bytes (palette and pixels)
    /// </summary>
    private static byte[] CreateInfoHeader(int width, int height, ushort bitCount, long dataBytes = 0)
    {
        var dib = new byte[40 + dataBytes];
        BitConverter.GetBytes(40).CopyTo(dib, 0);
        BitConverter.GetBytes(width).CopyTo(dib, 4);
        BitConverter.GetBytes(height).CopyTo(dib, 8);
        BitConverter.GetBytes((ushort)1).CopyTo(dib, 12);
        BitConverter.GetBytes(bitCount).CopyTo(dib, 14);
        return dib;
    }

    /// <summary>
    /// A BITMAPCOREHEADER (12 bytes, 16-bit width and height) followed by <paramref name="dataBytes"/> zero bytes
    /// </summary>
    private static byte[] CreateCoreHeader(ushort width, ushort height, ushort bitCount, int dataBytes = 0)
    {
        var dib = new byte[12 + dataBytes];
        BitConverter.GetBytes(12).CopyTo(dib, 0);
        BitConverter.GetBytes(width).CopyTo(dib, 4);
        BitConverter.GetBytes(height).CopyTo(dib, 6);
        BitConverter.GetBytes((ushort)1).CopyTo(dib, 8);
        BitConverter.GetBytes(bitCount).CopyTo(dib, 10);
        return dib;
    }

    private sealed class BytesSource : IClipboardDataSource
    {
        private readonly string _format;
        private readonly byte[] _bytes;

        public BytesSource(string format, byte[] bytes)
        {
            _format = format;
            _bytes = bytes;
        }

        public System.Collections.Generic.IReadOnlyCollection<string> Formats => new[] { _format };

        public bool HasFormat(string format) => format == _format;

        public bool TryGetStream(string format, out Stream stream)
        {
            stream = format == _format ? new MemoryStream(_bytes, false) : null;
            return stream != null;
        }
    }

    [Fact]
    public void Dib_DefaultMaxPixelCount_Is64Megapixels()
    {
        Assert.Equal(64L * 1024 * 1024, DibImage.DefaultMaxPixelCount);
        Assert.Equal(8192L * 8192, DibImage.DefaultMaxPixelCount);
    }

    [Theory]
    [InlineData("dib-24bpp-bottomup.bin")]
    [InlineData("dib-32bpp-rgb-topdown-alpha.bin")]
    public void Dib_MaxPixelCount_IsWidthTimesAbsoluteHeight(string file)
    {
        // 3 x 2 pixels, the top-down sample has a negative height
        var sample = Sample(file);
        Assert.True(TryDecodeChecked(sample, 6, out var image));
        Assert.Equal(6, image.Width * image.Height);
        Assert.False(TryDecodeChecked(sample, 5, out image));
        Assert.Null(image);

        var source = new BytesSource(StandardClipboardFormats.DeviceIndependentBitmap.AsString(), sample);
        Assert.True(source.TryGetAsDib(6, out _));
        Assert.False(source.TryGetAsDib(5, out _));
        Assert.True(source.TryGetAsDib(out _));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Dib_MaxPixelCount_MustBePositive(long maxPixelCount)
    {
        var sample = Sample("dib-24bpp-bottomup.bin");
        Assert.Throws<ArgumentOutOfRangeException>(() => TryDecodeChecked(sample, maxPixelCount, out _));
        var source = new BytesSource(StandardClipboardFormats.DeviceIndependentBitmap.AsString(), sample);
        Assert.Throws<ArgumentOutOfRangeException>(() => source.TryGetAsDib(maxPixelCount, out _));
    }

    /// <summary>
    /// Headers which claim huge bitmaps, without the data: rejected from the header, no exception (no OverflowException,
    /// no OutOfMemoryException), even without a pixel limit
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue, int.MaxValue, 32)]
    [InlineData(int.MaxValue, -int.MaxValue, 32)]
    [InlineData(int.MaxValue, 1, 32)]          // stride * height and width * 4 overflow an int
    [InlineData(65536, 65536, 32)]             // width * height overflows an int
    [InlineData(32768, 32768, 32)]             // width * height * 4 = 4 GiB overflows an int
    [InlineData(46341, 46341, 32)]             // width * height just above int.MaxValue
    [InlineData(0x20000000, 1, 32)]            // the pixels don't fit in one byte array
    [InlineData(100_000, 100_000, 1)]          // small data per pixel, huge image
    [InlineData(8193, 8192, 32)]               // just above 64 megapixels
    public void Dib_HugeDimensions_AreRejectedFromTheHeader(int width, int height, int bitCount)
    {
        var dib = CreateInfoHeader(width, height, (ushort)bitCount, 64);
#if NET
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
#endif
        Assert.False(TryDecodeChecked(dib, out var image));
        Assert.Null(image);
        Assert.False(TryDecodeChecked(dib, long.MaxValue, out image));
        Assert.Null(image);
#if NET
        // Nothing was allocated for the pixels
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - allocatedBefore < 64 * 1024);
#endif
    }

    [Fact]
    public void Dib_AboveTheDefaultLimit_WithCompleteData_IsRejected()
    {
        // 8193 x 8192 at 1 bpp: 8 MiB of data, which would be 256 MiB of pixels
        const int width = 8193;
        const int stride = (width + 31) / 32 * 4;
        var tooLarge = CreateInfoHeader(width, 8192, 1, 8 + (long)stride * 8192);
        Assert.False(TryDecodeChecked(tooLarge, out _));
        Assert.False(TryDecodeChecked(tooLarge, 8193L * 8192 - 1, out _));

        // The same bitmap with fewer rows is decoded
        var small = CreateInfoHeader(width, 2, 1, 8 + stride * 2);
        Assert.True(TryDecodeChecked(small, out var image));
        Assert.Equal(width, image.Width);
        Assert.Equal(2, image.Height);
    }

    [Fact]
    public void Dib_CoreHeader_24bpp_Decodes()
    {
        // 3 x 2, bottom-up, rows padded to 12 bytes
        var dib = CreateCoreHeader(3, 2, 24, 2 * 12);
        for (var i = 0; i < 6; i++)
        {
            var row = 1 - i / 3;
            Array.Copy(Reference[i], 0, dib, 12 + row * 12 + (i % 3) * 3, 3);
        }
        Assert.True(TryDecodeChecked(dib, out var image));
        AssertReference(image, false);
    }

    [Fact]
    public void Dib_CoreHeader_8bpp_HasRgbTriplePalette()
    {
        // 256 RGBTRIPLE palette entries (3 bytes), then one row of 2 pixels padded to 4 bytes
        var dib = CreateCoreHeader(2, 1, 8, 256 * 3 + 4);
        new byte[] { 10, 20, 30 }.CopyTo(dib, 12 + 1 * 3);
        new byte[] { 40, 50, 60 }.CopyTo(dib, 12 + 2 * 3);
        dib[12 + 256 * 3] = 1;
        dib[12 + 256 * 3 + 1] = 2;
        Assert.True(TryDecodeChecked(dib, out var image));
        Assert.Equal(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }, image.Pixels);
        Assert.False(image.HasAlpha);
    }

    [Fact]
    public void Dib_CoreHeader_SizesAreUnsigned16Bit()
    {
        // 65535 x 2 at 1 bpp: a signed 16-bit width would be -1
        const int stride = (65535 + 31) / 32 * 4;
        var dib = CreateCoreHeader(0xFFFF, 2, 1, 2 * 3 + 2 * stride);
        Assert.True(TryDecodeChecked(dib, 65535L * 2, out var image));
        Assert.Equal(65535, image.Width);
        Assert.Equal(2, image.Height);
        Assert.False(TryDecodeChecked(dib, 65535L * 2 - 1, out _));
    }

    [Theory]
    [InlineData(0xFFFF, 0xFFFF, 1)]
    [InlineData(0xFFFF, 0xFFFF, 24)]
    [InlineData(0x2001, 0x2000, 24)]  // just above 64 megapixels
    public void Dib_CoreHeader_HugeDimensions_AreRejected(int width, int height, int bitCount)
    {
        var dib = CreateCoreHeader((ushort)width, (ushort)height, (ushort)bitCount, 64);
        Assert.False(TryDecodeChecked(dib, out _));
        Assert.False(TryDecodeChecked(dib, long.MaxValue, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(32)]
    public void Dib_CoreHeader_UnsupportedBitCount_ReturnsFalse(int bitCount)
    {
        var dib = CreateCoreHeader(1, 1, (ushort)bitCount, 64);
        Assert.False(TryDecodeChecked(dib, out _));
    }

    [Fact]
    public void Dib_CoreHeader_Truncated_ReturnsFalse()
    {
        // The palette is missing
        Assert.False(TryDecodeChecked(CreateCoreHeader(1, 1, 8, 10), out _));
        // Only the header
        Assert.False(TryDecodeChecked(CreateCoreHeader(1, 1, 24), out _));
    }
}
