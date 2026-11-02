// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Imaging;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// WicFrameInfo.HasResolution per container, and palettes for WicImage.Encode.
/// The files are made in the tests: encoded with WIC, the resolution fields are then patched in the bytes.
/// </summary>
public class WicResolutionTests
{
    private static readonly LogSource Log = new LogSource();

    public WicResolutionTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    private const int Width = 5;
    private const int Height = 3;

    private static byte[] Encode(Guid container, double dpi = 96, Action<WicMetadataWriter> metadata = null)
    {
        var pixels = new byte[Width * Height * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (i % 4) == 3 ? (byte)255 : (byte)(i * 17);
        }
        var options = new WicEncodeOptions { ContainerFormat = container, DpiX = dpi, DpiY = dpi, Metadata = metadata };
        using var output = new MemoryStream();
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            WicImage.Encode(output, options, handle.AddrOfPinnedObject(), Width, Height, Width * 4, WicPixelFormats.Format32bppBgra);
        }
        finally
        {
            handle.Free();
        }
        return output.ToArray();
    }

    private static WicFrameInfo Info(byte[] file)
    {
        var info = WicImage.GetFrameInfo(new MemoryStream(file));
        Log.Info().WriteLine("{0}: HasResolution {1}, {2} x {3} DPI", info.ContainerFormat, info.HasResolution, info.DpiX, info.DpiY);
        return info;
    }

    private static bool HasDecoder(Guid containerFormat) => WicCodecs.GetDecoders().Any(e => e.ContainerFormat == containerFormat);

    private static bool CanCreateEncoder(Guid containerFormat)
    {
        if (!WicCodecs.GetEncoders().Any(e => e.ContainerFormat == containerFormat))
        {
            return false;
        }
        try
        {
            WicImage.GetEncoderOptions(containerFormat);
            return true;
        }
        catch (WicCodecNotFoundException)
        {
            return false;
        }
    }

    #region PNG

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (int k = 0; k < 8; k++)
        {
            c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c;
    }).ToArray();

    private static uint Crc32(byte[] data, int offset, int length)
    {
        var crc = 0xFFFFFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static void WriteBigEndian(List<byte> target, uint value)
    {
        target.Add((byte)(value >> 24));
        target.Add((byte)(value >> 16));
        target.Add((byte)(value >> 8));
        target.Add((byte)value);
    }

    private static uint ReadBigEndian(byte[] data, int offset) => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);

    /// <summary>
    /// Remove every pHYs chunk and, when pixelsPerUnit is given, add one after IHDR with that value and unit
    /// </summary>
    private static byte[] WithPhys(byte[] png, uint? pixelsPerUnit, byte unit)
    {
        var result = new List<byte>(png.Take(8));
        var offset = 8;
        while (offset < png.Length)
        {
            var length = (int)ReadBigEndian(png, offset);
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var chunkLength = 12 + length;
            if (type != "pHYs")
            {
                result.AddRange(png.Skip(offset).Take(chunkLength));
            }
            if (type == "IHDR" && pixelsPerUnit.HasValue)
            {
                var chunk = new List<byte>();
                WriteBigEndian(chunk, 9);
                chunk.AddRange(Encoding.ASCII.GetBytes("pHYs"));
                WriteBigEndian(chunk, pixelsPerUnit.Value);
                WriteBigEndian(chunk, pixelsPerUnit.Value);
                chunk.Add(unit);
                var bytes = chunk.ToArray();
                WriteBigEndian(chunk, Crc32(bytes, 4, bytes.Length - 4));
                result.AddRange(chunk);
            }
            offset += chunkLength;
        }
        return result.ToArray();
    }

    [Fact]
    public void Png_Phys72_HasResolution()
    {
        // 72 DPI = 2835 pixels per meter
        var info = Info(WithPhys(Encode(WicContainerFormats.Png), 2835, 1));
        Assert.True(info.HasResolution);
        Assert.InRange(info.DpiX, 71.9, 72.1);
    }

    [Fact]
    public void Png_EncodedByWic_HasResolution()
    {
        var file = Encode(WicContainerFormats.Png, 72);
        var info = Info(file);
        // The WIC encoder writes pHYs for the resolution
        Assert.Equal(file.AsSpan().IndexOf(Encoding.ASCII.GetBytes("pHYs")) >= 0, info.HasResolution);
    }

    [Fact]
    public void Png_WithoutPhys_NoResolution()
    {
        var info = Info(WithPhys(Encode(WicContainerFormats.Png, 72), null, 0));
        Assert.False(info.HasResolution);
        Assert.Equal(96, info.DpiX, 1);
    }

    [Fact]
    public void Png_PhysAspectRatioOnly_NoResolution()
    {
        var info = Info(WithPhys(Encode(WicContainerFormats.Png), 1, 0));
        Assert.False(info.HasResolution);
    }

    #endregion

    #region JPEG

    /// <summary>
    /// Remove the APP0 (and when wanted the APP1) segments and add a JFIF APP0 with the units and density after SOI
    /// </summary>
    private static byte[] WithJfif(byte[] jpeg, byte units, ushort density, bool removeApp1)
    {
        Assert.True(jpeg[0] == 0xFF && jpeg[1] == 0xD8, "Not a JPEG");
        var result = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        result.AddRange(Encoding.ASCII.GetBytes("JFIF\0"));
        result.AddRange(new byte[] { 1, 1, units, (byte)(density >> 8), (byte)density, (byte)(density >> 8), (byte)density, 0, 0 });
        var offset = 2;
        while (offset + 4 <= jpeg.Length && jpeg[offset] == 0xFF)
        {
            var marker = jpeg[offset + 1];
            if (marker == 0xDA)
            {
                break;
            }
            var length = jpeg[offset + 2] << 8 | jpeg[offset + 3];
            if (marker != 0xE0 && !(removeApp1 && marker == 0xE1))
            {
                result.AddRange(jpeg.Skip(offset).Take(2 + length));
            }
            offset += 2 + length;
        }
        // The scan and everything after it
        result.AddRange(jpeg.Skip(offset));
        return result.ToArray();
    }

    [Fact]
    public void Jpeg_JfifInch96_HasResolution()
    {
        var info = Info(WithJfif(Encode(WicContainerFormats.Jpeg), 1, 96, true));
        Assert.True(info.HasResolution);
        Assert.Equal(96, info.DpiX, 1);
    }

    [Fact]
    public void Jpeg_JfifNoUnitsWithoutExif_NoResolution()
    {
        var info = Info(WithJfif(Encode(WicContainerFormats.Jpeg), 0, 1, true));
        Assert.False(info.HasResolution);
    }

    [Fact]
    public void Jpeg_ExifXResolution72Only_HasResolution()
    {
        var file = Encode(WicContainerFormats.Jpeg, 72, writer =>
        {
            Assert.True(writer.Set("/app1/ifd/{ushort=282}", new WicRational(72, 1)));
            Assert.True(writer.Set("/app1/ifd/{ushort=283}", new WicRational(72, 1)));
        });
        var info = Info(WithJfif(file, 0, 1, false));
        Assert.True(info.Metadata.TryGet("/app1/ifd/{ushort=282}", out _), "The EXIF XResolution is missing");
        Assert.False(info.Metadata.TryGet("/app1/ifd/{ushort=296}", out _), "The test needs a file without ResolutionUnit");
        Assert.True(info.HasResolution);
        Assert.Equal(72, info.DpiX, 1);
    }

    [Fact]
    public void Jpeg_ExifResolutionUnitNone_NoResolution()
    {
        var file = Encode(WicContainerFormats.Jpeg, 72, writer =>
        {
            Assert.True(writer.Set("/app1/ifd/{ushort=282}", new WicRational(72, 1)));
            Assert.True(writer.Set("/app1/ifd/{ushort=296}", (ushort)1));
        });
        Assert.False(Info(WithJfif(file, 0, 1, false)).HasResolution);
    }

    #endregion

    [Fact]
    public void Tiff_XResolution_HasResolution()
    {
        var info = Info(Encode(WicContainerFormats.Tiff, 150));
        Assert.True(info.Metadata.TryGet("/ifd/{ushort=282}", out _));
        Assert.True(info.HasResolution);
        Assert.Equal(150, info.DpiX, 1);
    }

    [Fact]
    public void Bmp_PixelsPerMeter()
    {
        var file = Encode(WicContainerFormats.Bmp);
        var pixelsPerMeter = BitConverter.ToInt32(file, 38);
        Log.Info().WriteLine("The WIC BMP encoder wrote {0} pixels per meter", pixelsPerMeter);
        Assert.InRange(pixelsPerMeter, 3779, 3780);
        var info = Info(file);
        Assert.True(info.HasResolution);
        Assert.Equal(96, info.DpiX, 0);

        // biXPelsPerMeter and biYPelsPerMeter 0
        Array.Clear(file, 38, 8);
        var withoutResolution = Info(file);
        Assert.False(withoutResolution.HasResolution);
    }

    [Fact]
    public void Gif_NoResolution()
    {
        Assert.False(Info(Encode(WicContainerFormats.Gif)).HasResolution);
    }

    [Fact]
    public void JpegXr_HasResolution()
    {
        Assert.SkipUnless(CanCreateEncoder(WicContainerFormats.Wmp), "The JPEG XR encoder is not available");
        var info = Info(Encode(WicContainerFormats.Wmp, 120));
        Assert.True(info.HasResolution);
        Assert.Equal(120, info.DpiX, 1);
    }

    /// <summary>
    /// A 1x1 lossless WebP (VP8L) without EXIF
    /// </summary>
    private static readonly byte[] WebpWithoutExif = Convert.FromBase64String("UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==");

    [Fact]
    public void Webp_WithoutExif_NoResolution()
    {
        Assert.SkipUnless(HasDecoder(WicContainerFormats.Webp), "The WebP decoder is not installed");
        WicFrameInfo info;
        try
        {
            info = Info(WebpWithoutExif);
        }
        catch (WicCodecNotFoundException ex)
        {
            Assert.Skip($"The WebP decoder can't be used: {ex.Message}");
            return;
        }
        Assert.Equal(WicContainerFormats.Webp, info.ContainerFormat);
        Assert.False(info.HasResolution);
        Assert.True(info.DpiX > 0);
    }

    [Fact]
    public void Decode_ReportsHasResolution()
    {
        var file = WithPhys(Encode(WicContainerFormats.Png), null, 0);
        var pixels = new byte[Width * Height * 4];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var info = WicImage.Decode(new MemoryStream(file), WicPixelFormats.Format32bppBgra, handle.AddrOfPinnedObject(), Width * 4);
            Assert.False(info.HasResolution);
        }
        finally
        {
            handle.Free();
        }
        var rowsInfo = WicImage.Decode(new MemoryStream(WithPhys(file, 3780, 1)), WicPixelFormats.Format32bppBgra, (int top, int count, ReadOnlySpan<byte> rows, int stride) => { });
        Assert.True(rowsInfo.HasResolution);
    }

    [Fact]
    public void HasResolution_StreamAtOffset_PositionUnchanged()
    {
        var file = Encode(WicContainerFormats.Bmp);
        using var stream = new MemoryStream();
        stream.Write(new byte[17], 0, 17);
        stream.Write(file, 0, file.Length);
        stream.Position = 17;
        var info = WicImage.GetFrameInfo(stream);
        Assert.True(info.HasResolution);
        Assert.Equal(17, stream.Position);
    }

    #region Palette

    private static byte[] DecodeBgra(byte[] file, int width, int height)
    {
        var decoded = new byte[width * height * 4];
        var handle = GCHandle.Alloc(decoded, GCHandleType.Pinned);
        try
        {
            WicImage.Decode(new MemoryStream(file), WicPixelFormats.Format32bppBgra, handle.AddrOfPinnedObject(), width * 4);
        }
        finally
        {
            handle.Free();
        }
        return decoded;
    }

    private static byte[] EncodePixels(WicEncodeOptions options, byte[] pixels, int width, int height, int stride, Guid pixelFormat, out WicEncodeResult result)
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

    /// <summary>
    /// 32bpp pixels with at most 256 colors (and a transparent pixel) are written to GIF with exactly these colors
    /// </summary>
    [Fact]
    public void Gif_Bgra32With256Colors_IdenticalPixels()
    {
        const int width = 37;
        const int height = 9;
        var random = new Random(4711);
        var colors = Enumerable.Range(0, 255).Select(_ => (uint)random.Next(0x1000000) | 0xFF000000).Append(0u).ToArray();
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            BitConverter.GetBytes(colors[(i * 7) % colors.Length]).CopyTo(pixels, i * 4);
        }
        var file = EncodePixels(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Gif }, pixels, width, height, width * 4, WicPixelFormats.Format32bppBgra, out var result);
        Assert.Equal(WicPixelFormats.Format8bppIndexed, result.PixelFormat);
        Assert.Equal(pixels, DecodeBgra(file, width, height));
    }

    /// <summary>
    /// 8bpp indexed pixels with the palette of the options
    /// </summary>
    [Theory]
    [InlineData("gif")]
    [InlineData("png")]
    [InlineData("bmp")]
    public void Encode_Indexed8WithPalette(string containerName)
    {
        var container = containerName switch
        {
            "gif" => WicContainerFormats.Gif,
            "png" => WicContainerFormats.Png,
            _ => WicContainerFormats.Bmp
        };
        const int width = 7;
        const int height = 3;
        const int stride = 8;
        var palette = new uint[] { 0xFF000000, 0xFFFF0000, 0xFF00FF00, 0xFF0000FF, 0xFFFFFFFF, 0xFF123456 };
        var indexes = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                indexes[y * stride + x] = (byte)((x + y) % palette.Length);
            }
        }
        var file = EncodePixels(new WicEncodeOptions { ContainerFormat = container, Palette = palette }, indexes, width, height, stride, WicPixelFormats.Format8bppIndexed, out var result);
        Log.Info().WriteLine("{0}: {1}", containerName, result.PixelFormat);
        var decoded = DecodeBgra(file, width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Assert.Equal(palette[indexes[y * stride + x]], BitConverter.ToUInt32(decoded, (y * width + x) * 4));
            }
        }
    }

    /// <summary>
    /// A palette for 32bpp pixels: the encoder writes an indexed format with the nearest colors
    /// </summary>
    [Fact]
    public void Encode_Bgra32WithPalette_NearestColor()
    {
        const int width = 4;
        const int height = 2;
        var palette = new uint[] { 0xFF000000, 0xFFFFFFFF, 0xFFFF0000 };
        var source = new uint[] { 0xFF010101, 0xFFFEFEFE, 0xFFF00000, 0xFF000000, 0xFFFFFFFF, 0xFFFF0101, 0xFF020000, 0xFFEEEEEE };
        var expected = new uint[] { 0xFF000000, 0xFFFFFFFF, 0xFFFF0000, 0xFF000000, 0xFFFFFFFF, 0xFFFF0000, 0xFF000000, 0xFFFFFFFF };
        var pixels = source.SelectMany(BitConverter.GetBytes).ToArray();
        var file = EncodePixels(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png, Palette = palette }, pixels, width, height, width * 4, WicPixelFormats.Format32bppBgra, out var result);
        Assert.Equal(WicPixelFormats.Format8bppIndexed, result.PixelFormat);
        var decoded = DecodeBgra(file, width, height);
        Assert.Equal(expected, Enumerable.Range(0, width * height).Select(i => BitConverter.ToUInt32(decoded, i * 4)).ToArray());
    }

    [Fact]
    public void Encode_IndexedWithoutPalette_Throws()
    {
        var indexes = new byte[4];
        Assert.Throws<ArgumentException>(() => EncodePixels(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Gif }, indexes, 2, 2, 2, WicPixelFormats.Format8bppIndexed, out _));
        Assert.Throws<ArgumentException>(() => EncodePixels(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Gif, Palette = new uint[257] }, indexes, 2, 2, 2, WicPixelFormats.Format8bppIndexed, out _));
    }

    /// <summary>
    /// More than 256 colors: WIC makes the palette, with and without dithering
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Gif_ManyColors_Encodes(bool dither)
    {
        const int width = 64;
        const int height = 64;
        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                pixels[offset] = (byte)(x * 4);
                pixels[offset + 1] = (byte)(y * 4);
                pixels[offset + 2] = (byte)((x + y) * 2);
                pixels[offset + 3] = 255;
            }
        }
        var file = EncodePixels(new WicEncodeOptions { ContainerFormat = WicContainerFormats.Gif, Dither = dither }, pixels, width, height, width * 4, WicPixelFormats.Format32bppBgra, out _);
        var info = WicImage.GetFrameInfo(new MemoryStream(file));
        Assert.Equal(width, info.Width);
        var decoded = DecodeBgra(file, width, height);
        Assert.All(Enumerable.Range(0, width * height), i => Assert.Equal(255, decoded[i * 4 + 3]));
    }

    #endregion
}
