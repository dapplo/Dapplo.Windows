// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Imaging;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Metadata through the WIC metadata query language, with the built-in codecs (the paths are documented in doc/articles/imaging.md)
/// </summary>
public class WicMetadataTests
{
    private static readonly LogSource Log = new LogSource();

    public WicMetadataTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    public static IEnumerable<object[]> MetadataContainers => new[]
    {
        new object[] { "jpeg" }, new object[] { "tiff" }, new object[] { "png" }, new object[] { "wmp" }
    };

    private static Guid Container(string name) => name switch
    {
        "jpeg" => WicContainerFormats.Jpeg,
        "tiff" => WicContainerFormats.Tiff,
        "png" => WicContainerFormats.Png,
        "wmp" => WicContainerFormats.Wmp,
        "gif" => WicContainerFormats.Gif,
        "bmp" => WicContainerFormats.Bmp,
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>
    /// A 3x2 image, the blue channel of pixel i is i * 40 so the position of each pixel can be found after a rotation
    /// </summary>
    private static byte[] CreatePixels()
    {
        var pixels = new byte[3 * 2 * 4];
        for (int i = 0; i < 6; i++)
        {
            pixels[i * 4] = (byte)(i * 40);
            pixels[i * 4 + 1] = 100;
            pixels[i * 4 + 2] = 200;
            pixels[i * 4 + 3] = 255;
        }
        return pixels;
    }

    private static byte[] Encode(Guid container, Action<WicMetadataWriter> metadata, out WicEncodeResult result, byte[] pixels = null, int width = 3, int height = 2)
    {
        pixels ??= CreatePixels();
        using var output = new MemoryStream();
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            result = WicImage.Encode(output, new WicEncodeOptions { ContainerFormat = container, Metadata = metadata }, handle.AddrOfPinnedObject(), width, height, width * 4, WicPixelFormats.Format32bppBgra);
        }
        finally
        {
            handle.Free();
        }
        foreach (var issue in result.MetadataIssues)
        {
            Log.Info().WriteLine("Metadata issue: {0}", issue);
        }
        return output.ToArray();
    }

    private static void AssertValue(WicMetadata metadata, string query, object expected)
    {
        Assert.True(metadata.TryGet(query, out var value), $"{query} is missing, the frame has: {string.Join(", ", metadata.Items.Select(i => i.Key))}");
        Assert.Equal(expected, value);
    }

    [Theory]
    [MemberData(nameof(MetadataContainers))]
    public void Metadata_Paths_RoundTrip(string containerName)
    {
        var container = Container(containerName);
        var isPng = container == WicContainerFormats.Png;
        var encoded = Encode(container, writer =>
        {
            Assert.True(writer.SupportsQueries);
            Assert.Equal(container, writer.ContainerFormat);
            // The EXIF ASCII tags are VT_LPSTR, PNG text is a VT_LPWSTR
            Assert.True(writer.Set(WicMetadataPaths.Software(container), isPng ? "Greenshot" : WicMetadataValue.AnsiString("Greenshot")));
            Assert.True(writer.Set(isPng ? WicMetadataPaths.PngText("Description", 1) : WicMetadataPaths.Description(container), isPng ? "A capture" : WicMetadataValue.AnsiString("A capture")));
            Assert.True(writer.Set(WicMetadataPaths.XmpProperty(container, "dc:title"), "XMP title"));
            if (!isPng)
            {
                Assert.True(writer.Set(WicMetadataPaths.DateTimeOriginal(container), WicMetadataValue.AnsiString("2026:11:02 12:34:56")));
                Assert.True(writer.Set(WicMetadataPaths.ExifSubTag(container, 33434), new WicRational(1, 250)));
            }
        }, out var result);
        Assert.Empty(result.MetadataIssues);

        var metadata = WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata;
        foreach (var item in metadata.Items)
        {
            Log.Debug().WriteLine("{0} = {1}", item.Key, item.Value);
        }
        AssertValue(metadata, WicMetadataPaths.Software(container), "Greenshot");
        AssertValue(metadata, isPng ? WicMetadataPaths.PngText("Description", 1) : WicMetadataPaths.Description(container), "A capture");
        // A plain string makes a simple XMP property, the policy System.Title writes the language alternative (see Metadata_Policies)
        AssertValue(metadata, WicMetadataPaths.XmpProperty(container, "dc:title"), "XMP title");
        if (!isPng)
        {
            AssertValue(metadata, WicMetadataPaths.DateTimeOriginal(container), "2026:11:02 12:34:56");
            Assert.True(metadata.TryGetRational(WicMetadataPaths.ExifSubTag(container, 33434), out var exposure));
            Assert.Equal(new WicRational(1, 250), exposure);
        }
    }

    [Fact]
    public void Metadata_TagPathsAreAliases()
    {
        var encoded = Encode(WicContainerFormats.Jpeg, writer => writer.Set("/app1/ifd/exif/{ushort=36867}", WicMetadataValue.AnsiString("2026:01:01 00:00:00")), out _);
        var metadata = WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata;
        // WIC enumerates the blocks by their tags, the friendly names work the same
        AssertValue(metadata, "/app1/{ushort=0}/{ushort=34665}/{ushort=36867}", "2026:01:01 00:00:00");
        AssertValue(metadata, "/APP1/IFD/EXIF/{ushort=36867}", "2026:01:01 00:00:00");
        Assert.Contains(metadata.Items, i => i.Key == "/app1/ifd/exif" && i.Value is WicMetadataBlock block && block.Format == WicMetadataFormats.Exif);
    }

    /// <summary>
    /// The photo metadata policies write every location the container has, e.g. EXIF and XMP
    /// </summary>
    [Theory]
    [InlineData("jpeg")]
    [InlineData("tiff")]
    [InlineData("wmp")]
    public void Metadata_Policies(string containerName)
    {
        var container = Container(containerName);
        var encoded = Encode(container, writer =>
        {
            Assert.True(writer.Set("System.ApplicationName", "Greenshot"));
            Assert.True(writer.Set("System.Title", "Title"));
        }, out _);
        var metadata = WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata;
        AssertValue(metadata, WicMetadataPaths.Software(container), "Greenshot");
        AssertValue(metadata, WicMetadataPaths.XmpProperty(container, "xmp:CreatorTool"), "Greenshot");
        AssertValue(metadata, WicMetadataPaths.Description(container), "Title");
        AssertValue(metadata, WicMetadataPaths.XmpProperty(container, "dc:title/x-default"), "Title");
    }

    [Theory]
    [MemberData(nameof(MetadataContainers))]
    public void ColorProfile_RoundTrip(string containerName)
    {
        var iccPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "drivers", "color", "sRGB Color Space Profile.icm");
        Assert.SkipUnless(File.Exists(iccPath), "The sRGB profile of Windows is missing");
        var icc = File.ReadAllBytes(iccPath);
        var encoded = Encode(Container(containerName), writer => Assert.True(writer.SetColorProfile(icc)), out var result);
        Assert.Empty(result.MetadataIssues);
        Assert.Equal(icc, WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata.ColorProfile);
    }

    [Fact]
    public void ColorProfile_NoneIsNull()
    {
        var encoded = Encode(WicContainerFormats.Png, null, out _);
        Assert.Null(WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata.ColorProfile);
    }

    /// <summary>
    /// Every EXIF orientation, decoded with applyOrientation: the result must be the upright image.
    /// The 3x2 source has the pixels 0 1 2 / 3 4 5, the expected rows are the source indices of the upright image.
    /// </summary>
    [Theory]
    [InlineData(1, 3, "0,1,2,3,4,5")]
    [InlineData(2, 3, "2,1,0,5,4,3")]
    [InlineData(3, 3, "5,4,3,2,1,0")]
    [InlineData(4, 3, "3,4,5,0,1,2")]
    [InlineData(5, 2, "0,3,1,4,2,5")]
    [InlineData(6, 2, "3,0,4,1,5,2")]
    [InlineData(7, 2, "5,2,4,1,3,0")]
    [InlineData(8, 2, "2,5,1,4,0,3")]
    public void Orientation_Applied(int orientation, int expectedWidth, string expectedIndices)
    {
        var encoded = Encode(WicContainerFormats.Tiff, writer => Assert.True(writer.Set(WicMetadataPaths.Orientation(WicContainerFormats.Tiff), (ushort)orientation)), out _);

        var plain = WicImage.GetFrameInfo(new MemoryStream(encoded));
        Assert.Equal(orientation, plain.Metadata.Orientation);
        Assert.Equal(3, plain.Width);
        Assert.False(plain.OrientationApplied);

        var info = WicImage.GetFrameInfo(new MemoryStream(encoded), true);
        Assert.Equal(expectedWidth, info.Width);
        Assert.Equal(6 / expectedWidth, info.Height);
        Assert.Equal(orientation > 1, info.OrientationApplied);
        Assert.Equal(orientation, info.Metadata.Orientation);

        var pixels = new byte[6 * 4];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            WicImage.Decode(new MemoryStream(encoded), WicPixelFormats.Format32bppBgra, handle.AddrOfPinnedObject(), info.Width * 4, true);
        }
        finally
        {
            handle.Free();
        }
        Assert.Equal(expectedIndices, string.Join(",", Enumerable.Range(0, 6).Select(p => pixels[p * 4] / 40)));
    }

    [Fact]
    public void Orientation_Jpeg_FromPolicy()
    {
        var encoded = Encode(WicContainerFormats.Jpeg, writer => Assert.True(writer.Set(WicMetadataPaths.PolicyOrientation, (ushort)6)), out _);
        var info = WicImage.GetFrameInfo(new MemoryStream(encoded), true);
        Assert.Equal(6, info.Metadata.Orientation);
        Assert.Equal(2, info.Width);
        Assert.Equal(3, info.Height);
    }

    /// <summary>
    /// What an encoder doesn't support is reported, it doesn't fail the encoding
    /// </summary>
    [Fact]
    public void Metadata_Unsupported_IsReported()
    {
        var encoded = Encode(WicContainerFormats.Png, writer =>
        {
            Assert.False(writer.Set("/ifd/{ushort=305}", "not in a PNG"));
            Assert.True(writer.Set(WicMetadataPaths.Software(WicContainerFormats.Png), "Greenshot"));
        }, out var result);
        var issue = Assert.Single(result.MetadataIssues);
        Assert.Equal("/ifd/{ushort=305}", issue.Query);
        Assert.NotEqual(0, issue.ErrorCode);
        AssertValue(WicImage.GetFrameInfo(new MemoryStream(encoded)).Metadata, "/tEXt/{str=Software}", "Greenshot");

        // BMP has no metadata at all
        Encode(WicContainerFormats.Bmp, writer =>
        {
            Assert.False(writer.SupportsQueries);
            Assert.False(writer.Set("System.ApplicationName", "Greenshot"));
            Assert.False(writer.SetColorProfile(new byte[] { 1, 2, 3 }));
        }, out result);
        Assert.Equal(2, result.MetadataIssues.Count);
    }

    [Fact]
    public void MetadataWriter_InvalidUse()
    {
        WicMetadataWriter kept = null;
        Encode(WicContainerFormats.Png, writer =>
        {
            kept = writer;
            Assert.Throws<ArgumentException>(() => writer.Set("/tEXt/{str=Software}", new object()));
        }, out _);
        Assert.Throws<InvalidOperationException>(() => kept.Set("/tEXt/{str=Software}", "too late"));
    }

    /// <summary>
    /// The XMP block of a decoded image, carried over to a new image as raw bytes
    /// </summary>
    [Theory]
    [MemberData(nameof(MetadataContainers))]
    public void RawBlock_Xmp_CarriedOver(string containerName)
    {
        var container = Container(containerName);
        var original = Encode(container, writer => Assert.True(writer.Set(WicMetadataPaths.XmpProperty(container, "dc:title"), "Raw title")), out _);
        var rawBlocks = WicImage.GetFrameInfo(new MemoryStream(original)).Metadata.RawBlocks;
        var xmp = Assert.Single(rawBlocks, b => b.Format == WicMetadataFormats.Xmp);
        Assert.Equal(WicMetadataPaths.Xmp(container), xmp.Path);
        Assert.Contains("Raw title", System.Text.Encoding.UTF8.GetString(xmp.Data));

        var copy = Encode(container, writer => Assert.True(writer.AddRawBlock(xmp)), out var result);
        Assert.Empty(result.MetadataIssues);
        AssertValue(WicImage.GetFrameInfo(new MemoryStream(copy)).Metadata, WicMetadataPaths.XmpProperty(container, "dc:title"), "Raw title");
    }

    /// <summary>
    /// The APP1 (EXIF) block of a JPEG, carried over to a new JPEG as raw bytes
    /// </summary>
    [Fact]
    public void RawBlock_JpegApp1_CarriedOver()
    {
        var software = WicMetadataPaths.Software(WicContainerFormats.Jpeg);
        var original = Encode(WicContainerFormats.Jpeg, writer => Assert.True(writer.Set(software, WicMetadataValue.AnsiString("Raw software"))), out _);
        var app1 = Assert.Single(WicImage.GetFrameInfo(new MemoryStream(original)).Metadata.RawBlocks, b => b.Format == WicMetadataFormats.App1);
        Assert.Equal("/app1", app1.Path);

        var copy = Encode(WicContainerFormats.Jpeg, writer => Assert.True(writer.AddRawBlock(app1)), out var result);
        Assert.Empty(result.MetadataIssues);
        AssertValue(WicImage.GetFrameInfo(new MemoryStream(copy)).Metadata, software, "Raw software");
    }

    /// <summary>
    /// GIF needs a palette, the transparent pixel stays transparent
    /// </summary>
    [Fact]
    public void Gif_WithTransparency()
    {
        var pixels = CreatePixels();
        pixels[3] = 0; // first pixel transparent
        var encoded = Encode(WicContainerFormats.Gif, null, out var result, pixels);
        Assert.NotEqual(WicPixelFormats.Format32bppBgra, result.PixelFormat);
        var decoded = new byte[pixels.Length];
        var handle = GCHandle.Alloc(decoded, GCHandleType.Pinned);
        try
        {
            WicImage.Decode(new MemoryStream(encoded), WicPixelFormats.Format32bppBgra, handle.AddrOfPinnedObject(), 12);
        }
        finally
        {
            handle.Free();
        }
        Assert.Equal(0, decoded[3]);
        for (int i = 1; i < 6; i++)
        {
            Assert.Equal(255, decoded[i * 4 + 3]);
            Assert.Equal(pixels[i * 4], decoded[i * 4]);
        }
    }

    [Fact]
    public void Rational_Packing()
    {
        var rational = new WicRational(1, 250);
        Assert.Equal(1UL | (250UL << 32), rational.ToPacked());
        Assert.Equal(rational, WicRational.FromPacked(rational.ToPacked()));
        Assert.Equal(0.004, rational.ToDouble(), 6);
        Assert.True(double.IsNaN(new WicRational(1, 0).ToDouble()));

        var signed = new WicSignedRational(-1, 3);
        Assert.Equal(signed, WicSignedRational.FromPacked(signed.ToPacked()));
        Assert.Equal(-1.0 / 3, signed.ToDouble(), 6);
    }
}
