// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs.PixelFormats;
using Xunit;
using Dapplo.Log.XUnit;

namespace Dapplo.Windows.Tests;

public class BitmapAccessorTests
{
    public BitmapAccessorTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test BitmapAccessor with 32-bit ARGB format
    /// </summary>
    [Fact]
    public void TestBitmapAccessor_32bppArgb()
    {
        var bitmap = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        
        using (var accessor = new BitmapAccessor<Bgra32>(bitmap, readOnly: false))
        {
            Assert.Equal(10, accessor.Width);
            Assert.Equal(10, accessor.Height);
            Assert.Equal(System.Drawing.Imaging.PixelFormat.Format32bppArgb, accessor.PixelFormat);
            
            // Test getting a row span
            var row = accessor.GetRowSpan(0);
            Assert.Equal(10, row.Length); // 10 pixels of type Bgra32
            
            // Test setting a pixel value using typed access
            row[0] = new Bgra32(255, 0, 0, 255); // Red pixel with full alpha
        }
        
        // Verify the pixel was set correctly
        var color = bitmap.GetPixel(0, 0);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
        
        bitmap.Dispose();
    }

    /// <summary>
    ///     Test BitmapAccessor with 24-bit RGB format
    /// </summary>
    [Fact]
    public void TestBitmapAccessor_24bppRgb()
    {
        var bitmap = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        
        using (var accessor = new BitmapAccessor<Bgr24>(bitmap, readOnly: false))
        {
            Assert.Equal(10, accessor.Width);
            Assert.Equal(10, accessor.Height);
            Assert.Equal(System.Drawing.Imaging.PixelFormat.Format24bppRgb, accessor.PixelFormat);
            
            // Test getting a row span
            var row = accessor.GetRowSpan(5);
            Assert.Equal(10, row.Length); // 10 pixels of type Bgr24
        }
        
        bitmap.Dispose();
    }

    /// <summary>
    ///     Test BitmapAccessor ProcessRows method
    /// </summary>
    [Fact]
    public void TestBitmapAccessor_ProcessRows()
    {
        var bitmap = new Bitmap(5, 5, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        
        using (var accessor = new BitmapAccessor<Bgra32>(bitmap, readOnly: false))
        {
            int rowsProcessed = 0;
            accessor.ProcessRows((y, row) =>
            {
                rowsProcessed++;
                // Set all pixels in this row to red using typed access
                for (int x = 0; x < 5; x++)
                {
                    row[x] = new Bgra32(255, 0, 0, 255); // Red with full alpha
                }
            });
            
            Assert.Equal(5, rowsProcessed);
        }
        
        // Verify all pixels are red
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                var color = bitmap.GetPixel(x, y);
                Assert.Equal(255, color.R);
                Assert.Equal(0, color.G);
                Assert.Equal(0, color.B);
            }
        }
        
        bitmap.Dispose();
    }

    /// <summary>
    ///     Test BitmapAccessor with unsupported format throws exception
    /// </summary>
    [Fact]
    public void TestBitmapAccessor_UnsupportedFormat_ThrowsException()
    {
        var bitmap = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
        
        Assert.Throws<NotSupportedException>(() =>
        {
            var accessor = new BitmapAccessor<Bgra32>(bitmap, readOnly: true);
        });

        bitmap.Dispose();
    }
}
