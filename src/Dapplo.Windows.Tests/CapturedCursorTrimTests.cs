// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     CapturedCursor.Trim on synthetic cursors: an alpha cursor (32bpp ARGB, no mask) and a mask cursor (24bpp color and mask layers)
/// </summary>
public class CapturedCursorTrimTests
{
    /// <summary>
    ///     An alpha cursor of 32x32 with a visible block from (left, top) to (right, bottom), inclusive; the block has a gradient so pixels can be compared
    /// </summary>
    private static CapturedCursor AlphaCursor(int left, int top, int right, int bottom, NativePoint hotSpot)
    {
        var color = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                color.SetPixel(x, y, Color.FromArgb(x == left ? 128 : 255, x * 7 % 256, y * 11 % 256, 200));
            }
        }
        return new CapturedCursor { ColorLayer = color, HotSpot = hotSpot, Size = new NativeSize(32, 32) };
    }

    /// <summary>
    ///     A mask cursor of 32x32: everything transparent (mask white, color black) except a black-masked block with a color
    /// </summary>
    private static CapturedCursor MaskCursor(int left, int top, int right, int bottom, NativePoint hotSpot)
    {
        var color = new Bitmap(32, 32, PixelFormat.Format24bppRgb);
        var mask = new Bitmap(32, 32, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(color))
        {
            graphics.Clear(Color.Black);
        }
        using (var graphics = Graphics.FromImage(mask))
        {
            graphics.Clear(Color.White);
        }
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                mask.SetPixel(x, y, Color.Black);
                color.SetPixel(x, y, Color.FromArgb(x * 7 % 256, y * 11 % 256, 90));
            }
        }
        return new CapturedCursor { ColorLayer = color, MaskLayer = mask, HotSpot = hotSpot, Size = new NativeSize(32, 32) };
    }

    private static void AssertSamePixels(Bitmap source, Bitmap trimmed, int offsetX, int offsetY)
    {
        for (var y = 0; y < trimmed.Height; y++)
        {
            for (var x = 0; x < trimmed.Width; x++)
            {
                Assert.Equal(source.GetPixel(x + offsetX, y + offsetY), trimmed.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Trim_AlphaCursor_CropsToTheVisiblePixels()
    {
        using var cursor = AlphaCursor(4, 6, 15, 24, new NativePoint(5, 7));
        using var trimmed = cursor.Trim();

        Assert.Equal(new NativeSize(12, 19), trimmed.Size);
        Assert.Equal(new NativePoint(1, 1), trimmed.HotSpot);
        Assert.Null(trimmed.MaskLayer);
        Assert.Equal(12, trimmed.ColorLayer.Width);
        Assert.Equal(19, trimmed.ColorLayer.Height);
        Assert.Equal(PixelFormat.Format32bppArgb, trimmed.ColorLayer.PixelFormat);
        AssertSamePixels(cursor.ColorLayer, trimmed.ColorLayer, 4, 6);

        // The original is untouched and still owned by the caller
        Assert.Equal(new NativeSize(32, 32), cursor.Size);
        Assert.Equal(32, cursor.ColorLayer.Width);
        Assert.Equal(new NativePoint(5, 7), cursor.HotSpot);
    }

    /// <summary>
    ///     A pixel which inverts the screen (mask white, color white), like the I-beam has, is visible: it stays inside
    /// </summary>
    [Fact]
    public void Trim_MaskCursor_KeepsInvertingPixels()
    {
        using var cursor = MaskCursor(10, 10, 19, 19, new NativePoint(12, 12));
        // An "invert" pixel right of the block, and one above it
        cursor.ColorLayer.SetPixel(20, 15, Color.White);
        cursor.ColorLayer.SetPixel(14, 8, Color.FromArgb(30, 30, 30));
        using var trimmed = cursor.Trim();

        Assert.Equal(new NativeSize(11, 12), trimmed.Size);
        Assert.Equal(new NativePoint(2, 4), trimmed.HotSpot);
        Assert.Equal(PixelFormat.Format24bppRgb, trimmed.ColorLayer.PixelFormat);
        Assert.Equal(PixelFormat.Format24bppRgb, trimmed.MaskLayer.PixelFormat);
        Assert.Equal(trimmed.ColorLayer.Size, trimmed.MaskLayer.Size);
        AssertSamePixels(cursor.ColorLayer, trimmed.ColorLayer, 10, 8);
        AssertSamePixels(cursor.MaskLayer, trimmed.MaskLayer, 10, 8);
        // The inverting pixel is at the right edge
        Assert.Equal(Color.FromArgb(255, 255, 255, 255), trimmed.ColorLayer.GetPixel(10, 7));
        Assert.Equal(Color.FromArgb(255, 255, 255, 255), trimmed.MaskLayer.GetPixel(10, 7));
    }

    [Fact]
    public void Trim_AllTransparent_IsAClone()
    {
        using var alpha = AlphaCursor(1, 1, 0, 0, new NativePoint(3, 4));
        using var trimmedAlpha = alpha.Trim();
        Assert.Equal(alpha.Size, trimmedAlpha.Size);
        Assert.Equal(alpha.HotSpot, trimmedAlpha.HotSpot);
        Assert.Equal(32, trimmedAlpha.ColorLayer.Width);
        Assert.NotSame(alpha.ColorLayer, trimmedAlpha.ColorLayer);

        using var mask = MaskCursor(1, 1, 0, 0, new NativePoint(3, 4));
        using var trimmedMask = mask.Trim();
        Assert.Equal(mask.Size, trimmedMask.Size);
        Assert.Equal(mask.HotSpot, trimmedMask.HotSpot);
        Assert.Equal(32, trimmedMask.MaskLayer.Height);
    }

    [Fact]
    public void Trim_NoTransparentBorder_IsAClone()
    {
        using var cursor = AlphaCursor(0, 0, 31, 31, new NativePoint(3, 4));
        using var trimmed = cursor.Trim();
        Assert.Equal(cursor.Size, trimmed.Size);
        Assert.Equal(cursor.HotSpot, trimmed.HotSpot);
        Assert.NotSame(cursor.ColorLayer, trimmed.ColorLayer);
    }

    [Fact]
    public void Trim_WithoutLayers_IsAClone()
    {
        using var cursor = new CapturedCursor { HotSpot = new NativePoint(1, 2), Size = new NativeSize(32, 32) };
        using var trimmed = cursor.Trim();
        Assert.Null(trimmed.ColorLayer);
        Assert.Equal(cursor.HotSpot, trimmed.HotSpot);
    }

    /// <summary>
    ///     The hot spot in a transparent corner ends up negative, and drawing at "point minus HotSpot" gives the same image as the untrimmed cursor
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Trim_HotSpotInTheTransparentCorner_DrawsTheSame(bool maskCursor)
    {
        var hotSpot = new NativePoint(0, 0);
        using var cursor = maskCursor ? MaskCursor(8, 9, 19, 22, hotSpot) : AlphaCursor(8, 9, 19, 22, hotSpot);
        if (maskCursor)
        {
            // An inverting pixel at the bottom right
            cursor.ColorLayer.SetPixel(20, 23, Color.White);
        }
        using var trimmed = cursor.Trim();
        Assert.Equal(new NativePoint(-8, -9), trimmed.HotSpot);

        var point = new NativePoint(30, 25);
        using var expected = Background();
        using var actual = Background();
        CursorHelper.DrawCursorOnBitmap(expected, cursor, new NativePoint(point.X - cursor.HotSpot.X, point.Y - cursor.HotSpot.Y));
        CursorHelper.DrawCursorOnBitmap(actual, trimmed, new NativePoint(point.X - trimmed.HotSpot.X, point.Y - trimmed.HotSpot.Y));
        AssertSamePixels(expected, actual, 0, 0);
    }

    /// <summary>
    ///     A background with different colors, so inverting and blending show
    /// </summary>
    private static Bitmap Background()
    {
        var bitmap = new Bitmap(80, 80, PixelFormat.Format32bppArgb);
        for (var y = 0; y < 80; y++)
        {
            for (var x = 0; x < 80; x++)
            {
                bitmap.SetPixel(x, y, Color.FromArgb(255, x * 3, y * 3, (x + y) % 256));
            }
        }
        return bitmap;
    }
}
