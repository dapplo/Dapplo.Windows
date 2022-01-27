// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.ImageSharpInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Xunit;
using Xunit.Abstractions;
using Color = SixLabors.ImageSharp.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Dapplo.Windows.Tests;

public class ImageSharpInteropTests
{
    public ImageSharpInteropTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test Bitmap with Format32bppArgb using a ImageSharp Mutate
    /// </summary>
    [WpfFact]
    private void Test_Bitmap_Format32bppArgb_ImageSharpInterop_Mutate()
    {
        using var bitmap = new Bitmap(1000, 1000, PixelFormat.Format32bppArgb);
        var testColor = Color.Aqua;

        bitmap.Mutate(x => x.Clear(testColor));
        var testPixelColor = bitmap.GetPixel(10, 10);

        Assert.Equal(testColor.ToHex(), $"{testPixelColor.R:X2}{testPixelColor.G:X2}{testPixelColor.B:X2}{testPixelColor.A:X2}");
    }

    /// <summary>
    ///     Test WriteableBitmap with Bgra32 using a ImageSharp Mutate
    /// </summary>
    [WpfFact]
    private void Test_WriteableBitmap_Bgra32_ImageSharpInterop_Mutate()
    {
        var bitmap = new WriteableBitmap(1000, 1000, 96f, 96f, PixelFormats.Bgra32, null);
        var testColor = Color.Aqua;

        bitmap.Mutate(x => x.Clear(testColor));

        using (var bitmapContext = bitmap.GetBitmapContext())
        {
            var testPixelColor = bitmap.GetPixel(10, 10);
            Assert.Equal(testColor.ToHex(), $"{testPixelColor.R:X2}{testPixelColor.G:X2}{testPixelColor.B:X2}{testPixelColor.A:X2}");
        }
    }

    /// <summary>
    ///     Test Bitmap with Format32bppArgb using a ImageSharp Mutate
    /// </summary>
    [WpfFact]
    private void Test_Bitmap_Format32bppArgb_ImageSharpInterop_Save()
    {
        using FileStream fis = File.OpenRead(@"C:\Users\robin\Downloads\8521206921.jpg");
        var bitmap = (Bitmap)System.Drawing.Image.FromStream(fis);

        using var bitmapWrapper = bitmap.Wrap();
        bitmap.Save("blub-gdi.png", ImageFormat.Png);
        using FileStream fs = File.OpenWrite("blub-is.png");
        bitmapWrapper.ImageWrapper.SaveAsPng(fs);
    }
}