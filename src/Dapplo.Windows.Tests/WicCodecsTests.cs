// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Imaging;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the WIC codec discovery, these only read and work on every Windows 10 1809+ machine (also the CI runner)
/// </summary>
public class WicCodecsTests
{
    private static readonly LogSource Log = new LogSource();

    public WicCodecsTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    private static void LogCodecs(IEnumerable<WicCodecInfo> codecs)
    {
        foreach (var codec in codecs)
        {
            Log.Info().WriteLine("{0}: {1} by {2} v{3}, container {4}, vendor {5}, {6}, extensions {7}, mime types {8}",
                codec.Kind, codec.FriendlyName, codec.Author, codec.Version, codec.ContainerFormat, codec.VendorGuid, codec.SigningStatus,
                string.Join(" ", codec.FileExtensions), string.Join(" ", codec.MimeTypes));
        }
    }

    private static void AssertContains(IReadOnlyList<WicCodecInfo> codecs, string extension, string mimeType)
    {
        Assert.Contains(codecs, c => c.FileExtensions.Contains(extension) && c.MimeTypes.Contains(mimeType));
    }

    private static void AssertWellFormed(IReadOnlyList<WicCodecInfo> codecs, WicCodecKind kind)
    {
        Assert.NotEmpty(codecs);
        foreach (var codec in codecs)
        {
            Assert.Equal(kind, codec.Kind);
            Assert.NotEqual(Guid.Empty, codec.Clsid);
            Assert.NotNull(codec.FriendlyName);
            Assert.All(codec.FileExtensions, extension =>
            {
                Assert.StartsWith(".", extension);
                Assert.True(extension.Length > 1);
                Assert.Equal(extension.Trim().ToLowerInvariant(), extension);
            });
            Assert.All(codec.MimeTypes, mimeType =>
            {
                Assert.NotEmpty(mimeType);
                Assert.Equal(mimeType.Trim().ToLowerInvariant(), mimeType);
            });
            Assert.Equal(codec.FileExtensions.Count, codec.FileExtensions.Distinct().Count());
            Assert.Equal(codec.MimeTypes.Count, codec.MimeTypes.Distinct().Count());
        }
    }

    [Fact]
    public void GetDecoders_ContainsBuiltInDecoders()
    {
        var decoders = WicCodecs.GetDecoders();
        LogCodecs(decoders);
        AssertWellFormed(decoders, WicCodecKind.Decoder);
        AssertContains(decoders, ".png", "image/png");
        AssertContains(decoders, ".jpg", "image/jpeg");
        AssertContains(decoders, ".gif", "image/gif");
        AssertContains(decoders, ".bmp", "image/bmp");
        AssertContains(decoders, ".tiff", "image/tiff");
        var png = decoders.First(c => c.FileExtensions.Contains(".png"));
        Assert.True(png.IsFromMicrosoft);
        Assert.True(png.SigningStatus.HasFlag(WicSigningStatus.Signed));
    }

    [Fact]
    public void GetEncoders_ContainsBuiltInEncoders()
    {
        var encoders = WicCodecs.GetEncoders();
        LogCodecs(encoders);
        AssertWellFormed(encoders, WicCodecKind.Encoder);
        AssertContains(encoders, ".png", "image/png");
        AssertContains(encoders, ".jpg", "image/jpeg");
    }

    [Fact]
    public void GetDecoders_Refresh()
    {
        var decoders = WicCodecs.GetDecoders();
        var refreshed = WicCodecs.GetDecoders(true);
        AssertWellFormed(refreshed, WicCodecKind.Decoder);
        Assert.Equal(decoders.Select(c => c.Clsid).OrderBy(g => g), refreshed.Select(c => c.Clsid).OrderBy(g => g));
    }

    [StaFact]
    public void GetDecoders_FromSta()
    {
        Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
        AssertContains(WicCodecs.GetDecoders(), ".png", "image/png");
    }

    [Fact]
    public void GetDecoders_FromMta()
    {
        IReadOnlyList<WicCodecInfo> decoders = null;
        Exception error = null;
        var thread = new Thread(() =>
        {
            try
            {
                decoders = WicCodecs.GetDecoders();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();
        Assert.Null(error);
        AssertContains(decoders, ".png", "image/png");
    }

    /// <summary>
    /// The HEIF Image Extensions from the Store add the decoder for HEIF and AVIF, skipped when it isn't installed
    /// </summary>
    [Fact]
    public void GetDecoders_HeifExtension()
    {
        var heif = WicCodecs.GetDecoders().FirstOrDefault(c => c.FileExtensions.Contains(".heic"));
        Assert.SkipWhen(heif == null, "The HEIF Image Extensions are not installed");
        Log.Info().WriteLine("HEIF decoder: {0}", heif);
        Assert.Contains(".heic", heif.FileExtensions);
        Assert.Contains(".avif", heif.FileExtensions);
    }
}
