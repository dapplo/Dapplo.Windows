// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Imaging;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Round trips through the WIC encoders and decoders, the Store codecs (JPEG XL, HEIF) are skipped when they aren't installed
/// </summary>
public class WicImageTests
{
    private static readonly LogSource Log = new LogSource();

    public WicImageTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    /// A test image with every byte of a row different and varying alpha, the bytes between rows (stride padding) are 0xCD
    /// </summary>
    private static byte[] CreatePixels(int width, int height, int bytesPerPixel, int stride, bool opaque = false)
    {
        var pixels = Enumerable.Repeat((byte)0xCD, stride * height).ToArray();
        var random = new Random(width * 31 + height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width * bytesPerPixel; x++)
            {
                pixels[y * stride + x] = (byte)random.Next(256);
            }
            if (opaque && bytesPerPixel == 4)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * stride + x * 4 + 3] = 255;
                }
            }
        }
        return pixels;
    }

    private static void AssertSamePixels(byte[] expected, int expectedStride, byte[] actual, int actualStride, int rowBytes, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < rowBytes; x++)
            {
                if (expected[y * expectedStride + x] != actual[y * actualStride + x])
                {
                    Assert.Fail($"Pixel byte {x} of row {y} differs: expected {expected[y * expectedStride + x]}, actual {actual[y * actualStride + x]}");
                }
            }
        }
    }

    private static byte[] EncodeFromMemory(WicEncodeOptions options, byte[] pixels, int width, int height, int stride, Guid pixelFormat, out WicEncodeResult result)
    {
        using var output = new MemoryStream();
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            result = WicImage.Encode(output, options, handle.AddrOfPinnedObject(), width, height, stride, pixelFormat);
        }
        finally
        {
            handle.Free();
        }
        return output.ToArray();
    }

    private static byte[] EncodeFromRows(WicEncodeOptions options, byte[] pixels, int width, int height, int stride, Guid pixelFormat, int bytesPerPixel)
    {
        using var output = new MemoryStream();
        WicImage.Encode(output, options, width, height, pixelFormat, (top, count, destination, destinationStride) =>
        {
            for (int row = 0; row < count; row++)
            {
                new ReadOnlySpan<byte>(pixels, (top + row) * stride, width * bytesPerPixel).CopyTo(destination.Slice(row * destinationStride));
            }
        });
        return output.ToArray();
    }

    private static byte[] DecodeToMemory(byte[] encoded, Guid pixelFormat, int stride, out WicFrameInfo info, bool applyOrientation = false)
    {
        info = WicImage.GetFrameInfo(new MemoryStream(encoded), applyOrientation);
        var pixels = new byte[stride * info.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            info = WicImage.Decode(new MemoryStream(encoded), pixelFormat, handle.AddrOfPinnedObject(), stride, applyOrientation);
        }
        finally
        {
            handle.Free();
        }
        return pixels;
    }

    private static bool HasEncoder(Guid containerFormat) => WicCodecs.GetEncoders().Any(e => e.ContainerFormat == containerFormat);
    private static bool HasDecoder(Guid containerFormat) => WicCodecs.GetDecoders().Any(e => e.ContainerFormat == containerFormat);

    /// <summary>
    /// A Store encoder can be registered but fail to initialize (WINCODEC_ERR_COMPONENTINITIALIZEFAILURE), e.g. on the CI runner or HEIF without the HEVC extension
    /// </summary>
    private static bool CanCreateEncoder(Guid containerFormat, out string reason)
    {
        reason = null;
        if (!HasEncoder(containerFormat))
        {
            reason = "The encoder is not installed";
            return false;
        }
        try
        {
            WicImage.GetEncoderOptions(containerFormat);
            return true;
        }
        catch (WicCodecNotFoundException ex)
        {
            reason = $"The encoder can't be used: {ex.Message}";
            return false;
        }
    }

    [Theory]
    [InlineData("png")]
    [InlineData("tiff")]
    public void RoundTrip_Bgra32_AllOverloads(string containerName)
    {
        var container = containerName == "png" ? WicContainerFormats.Png : WicContainerFormats.Tiff;
        const int width = 37, height = 23, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var options = new WicEncodeOptions { ContainerFormat = container };

        var fromMemory = EncodeFromMemory(options, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out var result);
        Assert.Equal(WicPixelFormats.Format32bppBgra, result.PixelFormat);
        var fromRows = EncodeFromRows(options, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, 4);

        foreach (var encoded in new[] { fromMemory, fromRows })
        {
            var decoded = DecodeToMemory(encoded, WicPixelFormats.Format32bppBgra, stride, out var info);
            Assert.Equal(width, info.Width);
            Assert.Equal(height, info.Height);
            Assert.Equal(container, info.ContainerFormat);
            AssertSamePixels(pixels, stride, decoded, stride, width * 4, height);

            DecodeToRowsAndCompare(encoded, pixels, stride, width, height);
        }
    }

    private static void DecodeToRowsAndCompare(byte[] encoded, byte[] pixels, int stride, int width, int height)
    {
        var info = WicImage.GetFrameInfo(new MemoryStream(encoded));
        var decoded = new byte[width * 4 * info.Height];
        WicImage.Decode(new MemoryStream(encoded), WicPixelFormats.Format32bppBgra, (top, count, source, sourceStride) =>
        {
            for (int row = 0; row < count; row++)
            {
                source.Slice(row * sourceStride, width * 4).CopyTo(new Span<byte>(decoded, (top + row) * width * 4, width * 4));
            }
        });
        AssertSamePixels(pixels, stride, decoded, width * 4, width * 4, height);
    }

    /// <summary>
    /// 24bpp with a width of 63 (189 bytes per row) and a stride larger than a row, like GDI+ padding
    /// </summary>
    [Fact]
    public void RoundTrip_Bgr24_OddWidthAndPaddedStride()
    {
        const int width = 63, height = 17, rowBytes = width * 3, stride = 200;
        var pixels = CreatePixels(width, height, 3, stride);
        var options = new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png };

        var fromMemory = EncodeFromMemory(options, pixels, width, height, stride, WicPixelFormats.Format24bppBgr, out _);
        var fromRows = EncodeFromRows(options, pixels, width, height, stride, WicPixelFormats.Format24bppBgr, 3);
        foreach (var encoded in new[] { fromMemory, fromRows })
        {
            // Decode with the GDI+ stride of 24bpp: rounded up to 4 bytes
            const int gdiStride = (rowBytes + 3) & ~3;
            var decoded = DecodeToMemory(encoded, WicPixelFormats.Format24bppBgr, gdiStride, out var info);
            Assert.Equal(width, info.Width);
            AssertSamePixels(pixels, stride, decoded, gdiStride, rowBytes, height);
        }
    }

    [Fact]
    public void RoundTrip_JpegXr_Lossless()
    {
        const int width = 33, height = 19, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var encoded = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Wmp, Lossless = true }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out var result);
        Assert.Contains("Lossless", result.AppliedOptions);
        var decoded = DecodeToMemory(encoded, WicPixelFormats.Format32bppBgra, stride, out var info);
        Assert.Equal(WicContainerFormats.Wmp, info.ContainerFormat);
        AssertSamePixels(pixels, stride, decoded, stride, width * 4, height);
    }

    [Fact]
    public void Encode_Jpeg_Quality_IsApplied()
    {
        const int width = 64, height = 64, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride, true);
        var low = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Jpeg, ImageQuality = 0.1f }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out var lowResult);
        var high = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Jpeg, ImageQuality = 1f }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out _);
        Assert.Contains("ImageQuality", lowResult.AppliedOptions);
        // JPEG has no alpha, WIC converted the pixels
        Assert.NotEqual(WicPixelFormats.Format32bppBgra, lowResult.PixelFormat);
        Assert.True(low.Length < high.Length, $"quality 0.1 gave {low.Length} bytes, quality 1 gave {high.Length}");
    }

    [Fact]
    public void GetEncoderOptions_Jpeg()
    {
        var options = WicImage.GetEncoderOptions(WicContainerFormats.Jpeg);
        Log.Info().WriteLine("JPEG encoder options: {0}", string.Join(", ", options));
        Assert.Contains(options, o => o.Name == "ImageQuality");
    }

    [Fact]
    public void JpegXl_LosslessRoundTrip()
    {
        Assert.SkipUnless(HasDecoder(WicContainerFormats.JpegXl), "The JPEG XL decoder is not installed");
        Assert.SkipUnless(CanCreateEncoder(WicContainerFormats.JpegXl, out var reason), reason ?? string.Empty);
        var encoderOptions = WicImage.GetEncoderOptions(WicContainerFormats.JpegXl);
        Log.Info().WriteLine("JPEG XL encoder options: {0}", string.Join(", ", encoderOptions));
        Assert.SkipUnless(encoderOptions.Any(o => o.Name == "Lossless"), "The JPEG XL encoder has no Lossless option");
        const int width = 41, height = 29, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var encoded = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.JpegXl, Lossless = true }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out _);
        var decoded = DecodeToMemory(encoded, WicPixelFormats.Format32bppBgra, stride, out var info);
        Assert.Equal(WicContainerFormats.JpegXl, info.ContainerFormat);
        AssertSamePixels(pixels, stride, decoded, stride, width * 4, height);
    }

    [Fact]
    public void Heif_EncodeDecode()
    {
        Assert.SkipUnless(HasDecoder(WicContainerFormats.Heif), "The HEIF decoder is not installed");
        Assert.SkipUnless(CanCreateEncoder(WicContainerFormats.Heif, out var reason), reason ?? string.Empty);
        const int width = 64, height = 48, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride, true);
        byte[] encoded;
        try
        {
            encoded = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Heif, ImageQuality = 0.9f }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out _);
        }
        catch (COMException ex)
        {
            // The HEIF encoder needs the HEVC video extension
            Assert.Skip($"The HEIF encoder failed: {ex.Message}");
            return;
        }
        var decoded = DecodeToMemory(encoded, WicPixelFormats.Format32bppBgra, stride, out var info);
        Assert.Equal(WicContainerFormats.Heif, info.ContainerFormat);
        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);
        Assert.Contains(decoded, b => b != 0);
    }

    [Fact]
    public void Encode_UnknownContainer_ThrowsCodecNotFound()
    {
        var pixels = new byte[4 * 4 * 4];
        var exception = Assert.Throws<WicCodecNotFoundException>(() =>
            EncodeFromMemory(new WicEncodeOptions { ContainerFormat = Guid.NewGuid() }, pixels, 4, 4, 16, WicPixelFormats.Format32bppBgra, out _));
        Log.Info().WriteLine("Expected: {0}", exception.Message);
        Assert.Throws<WicCodecNotFoundException>(() => WicImage.GetEncoderOptions(Guid.NewGuid()));
    }

    [Fact]
    public void Decode_UnknownData_ThrowsCodecNotFound()
    {
        var garbage = Enumerable.Range(0, 1000).Select(i => (byte)(i * 37)).ToArray();
        Assert.Throws<WicCodecNotFoundException>(() => WicImage.GetFrameInfo(new MemoryStream(garbage)));
    }

    [Fact]
    public void Encode_RowCallbackException_ReachesCaller()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            WicImage.Encode(new MemoryStream(), new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, 8, 8, WicPixelFormats.Format32bppBgra,
                (top, count, destination, stride) => throw new InvalidOperationException("from the callback")));
        Assert.Equal("from the callback", exception.Message);
    }

    /// <summary>
    /// Streams which can't seek, and a decode from the middle of a larger stream
    /// </summary>
    [Fact]
    public void Streams_NonSeekableAndOffset()
    {
        const int width = 9, height = 7, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var encoded = EncodeFromMemory(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, out _);

        // Encode into a stream which can't seek
        using var forwardOnly = new ForwardOnlyStream(new MemoryStream());
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            WicImage.Encode(forwardOnly, new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, handle.AddrOfPinnedObject(), width, height, stride, WicPixelFormats.Format32bppBgra);
        }
        finally
        {
            handle.Free();
        }
        Assert.Equal(encoded, forwardOnly.Inner.ToArray());

        // Decode from a stream which can't seek
        var info = WicImage.GetFrameInfo(new ForwardOnlyStream(new MemoryStream(encoded)));
        Assert.Equal(width, info.Width);

        // Decode from an offset in a larger stream, the position is restored by GetFrameInfo
        var container = new MemoryStream();
        container.Write(new byte[123], 0, 123);
        container.Write(encoded, 0, encoded.Length);
        container.Position = 123;
        info = WicImage.GetFrameInfo(container);
        Assert.Equal(123, container.Position);
        Assert.Equal(height, info.Height);

        // Encode at an offset, the stream ends after the image
        var output = new MemoryStream();
        output.Write(new byte[10], 0, 10);
        EncodeAt(output, pixels, width, height, stride);
        Assert.Equal(10 + encoded.Length, output.Position);
        Assert.Equal(encoded, output.ToArray().Skip(10).ToArray());
    }

    private static void EncodeAt(Stream output, byte[] pixels, int width, int height, int stride)
    {
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            WicImage.Encode(output, new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, handle.AddrOfPinnedObject(), width, height, stride, WicPixelFormats.Format32bppBgra);
        }
        finally
        {
            handle.Free();
        }
    }

    [StaFact]
    public void RoundTrip_FromSta()
    {
        const int width = 5, height = 5, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var encoded = EncodeFromRows(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, pixels, width, height, stride, WicPixelFormats.Format32bppBgra, 4);
        var decoded = DecodeToMemory(encoded, WicPixelFormats.Format32bppBgra, stride, out _);
        AssertSamePixels(pixels, stride, decoded, stride, width * 4, height);
    }

    /// <summary>
    /// The COM stream copies from and to the buffer of a MemoryStream directly, other streams are read and written in chunks
    /// </summary>
    [Fact]
    public void RoundTrip_ThroughStreamsWhichAreNoMemoryStream()
    {
        const int width = 300, height = 200, stride = width * 4;
        var pixels = CreatePixels(width, height, 4, stride);
        var options = new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png };

        var target = new SeekableStream(new MemoryStream());
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            WicImage.Encode(target, options, handle.AddrOfPinnedObject(), width, height, stride, WicPixelFormats.Format32bppBgra);
        }
        finally
        {
            handle.Free();
        }
        var encoded = target.Inner.ToArray();
        Assert.True(encoded.Length > 0);

        foreach (var source in new Stream[] { new SeekableStream(new MemoryStream(encoded)), new MemoryStream(encoded, 0, encoded.Length, false, true) })
        {
            var decoded = new byte[stride * height];
            WicImage.Decode(source, WicPixelFormats.Format32bppBgra, (top, count, rows, rowStride) =>
            {
                for (int row = 0; row < count; row++)
                {
                    rows.Slice(row * rowStride, width * 4).CopyTo(new Span<byte>(decoded, (top + row) * stride, width * 4));
                }
            });
            AssertSamePixels(pixels, stride, decoded, stride, width * 4, height);
        }
    }

    /// <summary>
    /// A seekable stream which is not a MemoryStream
    /// </summary>
    private sealed class SeekableStream : Stream
    {
        public SeekableStream(MemoryStream inner) => Inner = inner;
        public MemoryStream Inner { get; }
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => Inner.Length;
        public override long Position { get => Inner.Position; set => Inner.Position = value; }
        public override void Flush() => Inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
        public override void SetLength(long value) => Inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);
    }

    /// <summary>
    /// A stream which can't seek or tell its length
    /// </summary>
    private sealed class ForwardOnlyStream : Stream
    {
        public ForwardOnlyStream(MemoryStream inner) => Inner = inner;
        public MemoryStream Inner { get; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => Inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);
    }
}
