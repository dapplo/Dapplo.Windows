// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Structs.PixelFormats;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Icons.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the sweep fixes in Common, Gdi32 and Icons (cursor capture, alpha blending, DIB headers, ICO/CUR writer, GDI+ blur)
/// </summary>
public class S2SweepTests
{
    // ---------- Alpha blending (C-12) ----------

    [Fact]
    public void Bgra32_AlphaBlend_OntoOpaqueTarget()
    {
        var target = new Bgra32(255, 255, 255);
        Bgra32.AlphaBlend(ref target, new Bgra32(255, 0, 0, 128));
        Assert.Equal(new Bgra32(255, 127, 127, 255), target);
    }

    [Fact]
    public void Bgra32_AlphaBlend_OntoTransparentTarget_KeepsSourceAlpha()
    {
        var target = new Bgra32(0, 0, 0, 0);
        var source = new Bgra32(10, 20, 30, 40);
        Bgra32.AlphaBlend(ref target, source);
        Assert.Equal(source, target);
    }

    [Fact]
    public void Bgra32_AlphaBlend_PorterDuffOver_SemiTransparentTarget()
    {
        var target = new Bgra32(0, 0, 255, 128);
        Bgra32.AlphaBlend(ref target, new Bgra32(255, 0, 0, 128));
        // a = 0.502 + 0.502 * 0.498 = 0.752, r = 0.502 / 0.752 = 0.667, b = 0.25 / 0.752 = 0.333
        Assert.Equal(192, target.A);
        Assert.Equal(170, target.R);
        Assert.Equal(0, target.G);
        Assert.Equal(85, target.B);
    }

    [Fact]
    public void Bgra32_AlphaBlendPremultiplied()
    {
        var source = new Bgra32(255, 0, 0, 128).ToPremultiplied();
        Assert.Equal(new Bgra32(128, 0, 0, 128), source);
        var target = new Bgra32(0, 0, 255, 255);
        Bgra32.AlphaBlendPremultiplied(ref target, source);
        Assert.Equal(new Bgra32(128, 0, 127, 255), target);
    }

    [Fact]
    public void Bgra32_PremultiplyRoundTrip()
    {
        var straight = new Bgra32(200, 100, 50, 200);
        var roundTrip = straight.ToPremultiplied().ToStraight();
        Assert.InRange(roundTrip.R, 199, 201);
        Assert.InRange(roundTrip.G, 99, 101);
        Assert.InRange(roundTrip.B, 49, 51);
        Assert.Equal(200, roundTrip.A);
        Assert.Equal(default, new Bgra32(1, 2, 3, 0).ToStraight());
    }

    [Fact]
    public void DrawCursorOnBitmap_PremultipliedCursor_IsNotDarkened()
    {
        using var target = CreateFilledBitmap(20, 20, PixelFormat.Format32bppArgb, Color.White);
        using var cursorLayer = CreateFilledBitmap(8, 8, PixelFormat.Format32bppPArgb, Color.FromArgb(128, 255, 0, 0));
        using var cursor = new CapturedCursor { ColorLayer = new Bitmap(cursorLayer), Size = new NativeSize(8, 8) };
        cursor.ColorLayer.Dispose();
        cursor.ColorLayer = cursorLayer.Clone(new Rectangle(0, 0, 8, 8), PixelFormat.Format32bppPArgb);

        CursorHelper.DrawCursorOnBitmap(target, cursor, new NativePoint(4, 4));

        var pixel = target.GetPixel(6, 6);
        Assert.Equal(255, pixel.A);
        Assert.InRange(pixel.R, 253, 255);
        Assert.InRange(pixel.G, 125, 129);
        Assert.InRange(pixel.B, 125, 129);
        Assert.Equal(Color.White.ToArgb(), target.GetPixel(2, 2).ToArgb());
    }

    [Fact]
    public void DrawCursorOnBitmap_TransparentTarget_KeepsCursorAlpha()
    {
        using var target = CreateFilledBitmap(20, 20, PixelFormat.Format32bppArgb, Color.Transparent);
        using var cursor = new CapturedCursor
        {
            ColorLayer = CreateFilledBitmap(8, 8, PixelFormat.Format32bppArgb, Color.FromArgb(128, 0, 0, 255)),
            Size = new NativeSize(8, 8)
        };

        CursorHelper.DrawCursorOnBitmap(target, cursor, new NativePoint(0, 0));

        var pixel = target.GetPixel(3, 3);
        Assert.Equal(128, pixel.A);
        Assert.Equal(255, pixel.B);
        Assert.Equal(0, target.GetPixel(10, 10).A);
    }

    [Fact]
    public void DrawCursorOnBitmap_PremultipliedTarget()
    {
        using var target = CreateFilledBitmap(20, 20, PixelFormat.Format32bppPArgb, Color.White);
        using var cursor = new CapturedCursor
        {
            ColorLayer = CreateFilledBitmap(8, 8, PixelFormat.Format32bppArgb, Color.FromArgb(128, 255, 0, 0)),
            Size = new NativeSize(8, 8)
        };

        CursorHelper.DrawCursorOnBitmap(target, cursor, new NativePoint(0, 0));

        var pixel = target.GetPixel(3, 3);
        Assert.Equal(255, pixel.A);
        Assert.InRange(pixel.R, 253, 255);
        Assert.InRange(pixel.G, 125, 129);
    }

    [Fact]
    public void DrawCursorOnBitmap_MaskedCursor_DifferentLayerFormats()
    {
        using var target = CreateFilledBitmap(20, 20, PixelFormat.Format24bppRgb, Color.White);
        // AND mask black = cut out, XOR color red
        using var cursor = new CapturedCursor
        {
            ColorLayer = CreateFilledBitmap(4, 4, PixelFormat.Format32bppArgb, Color.Red),
            MaskLayer = CreateFilledBitmap(4, 4, PixelFormat.Format24bppRgb, Color.Black),
            Size = new NativeSize(4, 4)
        };

        CursorHelper.DrawCursorOnBitmap(target, cursor, new NativePoint(2, 2), new NativeSize(8, 8));

        Assert.Equal(Color.Red.ToArgb(), target.GetPixel(9, 9).ToArgb());
        Assert.Equal(Color.White.ToArgb(), target.GetPixel(11, 11).ToArgb());
    }

    // ---------- DrawCursorOnGraphics (C-10) ----------

    [Fact]
    public void DrawCursorOnGraphics_AlphaCursor_NonSquareUsesSourceHeight()
    {
        using var target = CreateFilledBitmap(64, 64, PixelFormat.Format32bppArgb, Color.White);
        // 16x32 cursor: top half red, bottom half blue
        var layer = new Bitmap(16, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(layer))
        {
            g.Clear(Color.Red);
            using var brush = new SolidBrush(Color.Blue);
            g.FillRectangle(brush, 0, 16, 16, 16);
        }
        using var cursor = new CapturedCursor { ColorLayer = layer, Size = new NativeSize(16, 32) };
        using (var graphics = Graphics.FromImage(target))
        {
            CursorHelper.DrawCursorOnGraphics(graphics, cursor, new NativePoint(10, 10));
        }
        var bottom = target.GetPixel(18, 38);
        Assert.True(bottom.B > 200 && bottom.R < 50, $"Expected blue at the bottom of the cursor, got {bottom}");
        var top = target.GetPixel(18, 14);
        Assert.True(top.R > 200 && top.B < 50, $"Expected red at the top of the cursor, got {top}");
    }

    [Fact]
    public void DrawCursorOnGraphics_MaskedCursor_UsesTransform()
    {
        using var target = CreateFilledBitmap(64, 64, PixelFormat.Format24bppRgb, Color.White);
        using var cursor = new CapturedCursor
        {
            ColorLayer = CreateFilledBitmap(8, 16, PixelFormat.Format24bppRgb, Color.Red),
            MaskLayer = CreateFilledBitmap(8, 16, PixelFormat.Format24bppRgb, Color.Black),
            Size = new NativeSize(8, 16)
        };
        using (var graphics = Graphics.FromImage(target))
        {
            graphics.TranslateTransform(10, 10);
            CursorHelper.DrawCursorOnGraphics(graphics, cursor, new NativePoint(5, 5));
            // The HDC must have been released, otherwise GDI+ throws "Object is currently in use elsewhere"
            graphics.ResetTransform();
        }
        var inside = target.GetPixel(17, 29);
        Assert.True(inside.R > 200 && inside.G < 50 && inside.B < 50, $"Expected red at the translated position, got {inside}");
        Assert.Equal(Color.White.ToArgb(), target.GetPixel(7, 7).ToArgb());
        Assert.Equal(Color.White.ToArgb(), target.GetPixel(17, 33).ToArgb());
    }

    [Fact]
    [Trait("Category", "Interactive")]
    public void TryGetCurrentCursor_SizeMatchesLayers()
    {
        if (!CursorHelper.TryGetCurrentCursor(out var cursor))
        {
            return;
        }
        using (cursor)
        {
            Assert.NotNull(cursor.ColorLayer);
            Assert.Equal(cursor.Size.Width, cursor.ColorLayer.Width);
            Assert.Equal(cursor.Size.Height, cursor.ColorLayer.Height);
            if (cursor.MaskLayer != null)
            {
                Assert.Equal(cursor.Size.Width, cursor.MaskLayer.Width);
                Assert.Equal(cursor.Size.Height, cursor.MaskLayer.Height);
            }
            else
            {
                Assert.Equal(PixelFormat.Format32bppArgb, cursor.ColorLayer.PixelFormat);
            }
            Assert.InRange(cursor.HotSpot.X, 0, cursor.Size.Width - 1);
            Assert.InRange(cursor.HotSpot.Y, 0, cursor.Size.Height - 1);
        }
    }

    // ---------- DIB headers (C-19, C-30, C-31) ----------

    [Fact]
    public void BitmapInfoHeader_SizeImage_UsesDwordPaddingAndAbsHeight()
    {
        Assert.Equal(24u, BitmapInfoHeader.Create(3, -2, 24).SizeImage);
        Assert.Equal(20u, BitmapInfoHeader.Create(10, -5, 1).SizeImage);
        Assert.Equal(400u, BitmapInfoHeader.Create(10, -10, 32).SizeImage);
        Assert.Equal(12, BitmapInfoHeader.CalculateStride(3, 24));
        Assert.Equal(8, BitmapInfoHeader.CalculateStride(10, 4));
        Assert.Equal(4, BitmapInfoHeader.CalculateStride(8, 4));
    }

    [Fact]
    public void BitmapV4V5Header_TopDown_DoesNotOverflow()
    {
        Assert.Equal(400u, BitmapV5Header.Create(10, -10, 32).SizeImage);
        Assert.Equal(400u, BitmapV4Header.Create(10, -10, 32).SizeImage);
        Assert.Equal(24u, BitmapV5Header.Create(3, 2, 24).SizeImage);
    }

    [Fact]
    public void BitmapHeaders_OffsetToPixels()
    {
        var infoHeader8 = BitmapInfoHeader.Create(4, 4, 8);
        Assert.Equal(40u + 256 * 4, infoHeader8.OffsetToPixels);

        var bitfields = BitmapInfoHeader.Create(4, 4, 32);
        bitfields.Compression = BitmapCompressionMethods.BI_BITFIELDS;
        Assert.Equal(52u, bitfields.OffsetToPixels);

        var v5 = BitmapV5Header.Create(4, 4, 32);
        v5.Compression = BitmapCompressionMethods.BI_BITFIELDS;
        Assert.Equal(124u, v5.OffsetToPixels);

        var used = BitmapInfoHeader.Create(4, 4, 8);
        used.ColorsUsed = 16;
        Assert.Equal(40u + 16 * 4, used.OffsetToPixels);
    }

    [Fact]
    public void BitmapFileHeader_Create_IncludesColorTableAndMasks()
    {
        var infoHeader8 = BitmapInfoHeader.Create(4, 4, 8);
        var fileHeader = BitmapFileHeader.Create(infoHeader8);
        Assert.Equal(14 + 40 + 1024, fileHeader.OffsetToBitmapBits);
        Assert.Equal(14 + 40 + 1024 + 16, fileHeader.Size);

        var bitfields = BitmapInfoHeader.Create(4, 4, 32);
        bitfields.Compression = BitmapCompressionMethods.BI_BITFIELDS;
        fileHeader = BitmapFileHeader.Create(bitfields);
        Assert.Equal(14 + 52, fileHeader.OffsetToBitmapBits);
        Assert.Equal(14 + 52 + 64, fileHeader.Size);

        var v5 = BitmapV5Header.Create(4, -4, 32);
        v5.SizeImage = 0;
        fileHeader = BitmapFileHeader.Create(v5);
        Assert.Equal(14 + 124, fileHeader.OffsetToBitmapBits);
        Assert.Equal(14 + 124 + 64, fileHeader.Size);
    }

    [Fact]
    public void ColorSpace_Values()
    {
        Assert.Equal(0x4C494E4Bu, (uint)ColorSpace.PROFILE_LINKED);
        Assert.Equal(0x4D424544u, (uint)ColorSpace.PROFILE_EMBEDDED);
        Assert.Equal(0x73524742u, (uint)ColorSpace.LCS_sRGB);
        Assert.Equal(ColorSpaceIntent.LCS_GM_IMAGES, BitmapV5Header.Create(1, 1, 32).Intent);
    }

    [Fact]
    public void BitfieldColorMask_Layout()
    {
        var mask = BitfieldColorMask.Rgb888;
        Assert.Equal(12, Marshal.SizeOf(typeof(BitfieldColorMask)));
        var buffer = Marshal.AllocHGlobal(12);
        try
        {
            Marshal.StructureToPtr(mask, buffer, false);
            Assert.Equal(0x00FF0000, Marshal.ReadInt32(buffer, 0));
            Assert.Equal(0x0000FF00, Marshal.ReadInt32(buffer, 4));
            Assert.Equal(0x000000FF, Marshal.ReadInt32(buffer, 8));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    // ---------- ICO / CUR (C-20, C-21) ----------

    [Fact]
    public void IconDirEntry_ValidatesSize()
    {
        Assert.Equal(0, IconDirEntry.CreateForIcon(256, 256, 32, 0, 0).Width);
        Assert.Equal(48, IconDirEntry.CreateForIcon(48, 48, 32, 0, 0).Width);
        Assert.Equal(256, IconDirEntry.CreateForIcon(256, 16, 32, 0, 0).PixelWidth);
        Assert.Throws<ArgumentOutOfRangeException>(() => IconDirEntry.CreateForIcon(257, 16, 32, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => IconDirEntry.CreateForIcon(0, 16, 32, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GrpIconDirEntry.CreateForCursor(300, 16, 0, 0, 0, 1));
    }

    [Fact]
    public void IconFileWriter_LargeImages_AreScaledAndRoundTrip()
    {
        using var large = CreateFilledBitmap(512, 512, PixelFormat.Format32bppArgb, Color.FromArgb(128, 0, 255, 0));
        using var wide = CreateFilledBitmap(300, 150, PixelFormat.Format32bppArgb, Color.Red);
        using var small = CreateFilledBitmap(16, 16, PixelFormat.Format32bppArgb, Color.Blue);
        using var stream = new MemoryStream();
        IconFileWriter.WriteIconFile(stream, new Image[] { small, wide, large });

        var bytes = stream.ToArray();
        Assert.Equal(3, BitConverter.ToUInt16(bytes, 4));
        var entries = ReadEntries(bytes);
        Assert.Equal(16, entries[0].PixelWidth);
        Assert.Equal(256, entries[1].PixelWidth);
        Assert.Equal(128, entries[1].PixelHeight);
        Assert.Equal(256, entries[2].PixelWidth);
        Assert.Equal(256, entries[2].PixelHeight);

        // The reader finds the 256x256 image
        stream.Seek(0, SeekOrigin.Begin);
        using var extracted = stream.ExtractVistaIcon();
        Assert.NotNull(extracted);
        Assert.Equal(256, extracted.Width);
        Assert.Equal(256, extracted.Height);
        var pixel = extracted.GetPixel(128, 128);
        Assert.InRange(pixel.A, 126, 130);
        Assert.Equal(255, pixel.G);

        // The bitmap doesn't depend on the (disposed) stream
        stream.Dispose();
        using var copy = new MemoryStream();
        extracted.Save(copy, ImageFormat.Png);
        Assert.True(copy.Length > 0);
    }

    [Fact]
    public void IconFileWriter_Cursor_HotspotIsScaledAndValidated()
    {
        using var large = CreateFilledBitmap(512, 512, PixelFormat.Format32bppArgb, Color.Black);
        using var stream = new MemoryStream();
        IconFileWriter.WriteCursorFile(stream, new[] { ((Image)large, new Point(100, 200)) });
        var entries = ReadEntries(stream.ToArray());
        Assert.Equal(256, entries[0].PixelWidth);
        // For cursors Planes is the hotspot X and BitCount the hotspot Y
        Assert.Equal(50, entries[0].Planes);
        Assert.Equal(100, entries[0].BitCount);

        using var other = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => IconFileWriter.WriteCursorFile(other, new[] { ((Image)large, new Point(-1, 0)) }));
    }

    [Fact]
    public void ExtractVistaIcon_CorruptData_ReturnsNull()
    {
        var bytes = new byte[] { 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 32, 0, 0xFF, 0xFF, 0, 0, 22, 0, 0, 0 };
        using var stream = new MemoryStream(bytes);
        Assert.Null(stream.ExtractVistaIcon());
    }

    // ---------- Common structs (C-27, C-29, C-32, C-35) ----------

    [Fact]
    public void NativeRect_Union_IgnoresEmpty()
    {
        var rect = new NativeRect(100, 100, 10, 10);
        Assert.Equal(rect, rect.Union(NativeRect.Empty));
        Assert.Equal(rect, NativeRect.Empty.Union(rect));
        Assert.Equal(new NativeRect(100, 100, 20, 20), rect.Union(new NativeRect(110, 110, 10, 10)));
        var rectFloat = new NativeRectFloat(100, 100, 10, 10);
        Assert.Equal(rectFloat, rectFloat.Union(NativeRectFloat.Empty));
    }

    [Fact]
    public void IsEmpty_NegativeAndLargeSizes()
    {
        Assert.True(new NativeSize(-1, 5).IsEmpty);
        Assert.True(new NativeSize(0, 5).IsEmpty);
        Assert.False(new NativeSize(65536, 65536).IsEmpty);
        Assert.True(new NativeRect(0, 0, -5, 5).IsEmpty);
        Assert.False(new NativeRect(0, 0, 65536, 65536).IsEmpty);
        Assert.True(new NativeSizeFloat(-1f, 5f).IsEmpty);
        Assert.True(new NativeRectFloat(0, 0, float.NaN, 5).IsEmpty);
    }

    [Fact]
    public void FloatStructs_EqualsAndHashCode()
    {
        var nanPoint = new NativePointFloat(float.NaN, 1);
        Assert.True(nanPoint.Equals(nanPoint));
        var set = new HashSet<NativePointFloat> { nanPoint };
        Assert.Contains(nanPoint, set);

        var zero = new NativePointFloat(0f, 0f);
        var negativeZero = new NativePointFloat(-0f, -0f);
        Assert.True(zero.Equals(negativeZero));
        Assert.Equal(zero.GetHashCode(), negativeZero.GetHashCode());

        Assert.False(new NativeSizeFloat(1f, 1f).Equals(new NativeSizeFloat(1.0001f, 1f)));

        var rect = new NativeRectFloat(1, 2, 3, 4);
        Assert.True(rect.Equals(new NativeRectFloat(1, 2, 3, 4)));
        Assert.Equal(rect.GetHashCode(), new NativeRectFloat(1, 2, 3, 4).GetHashCode());
        Assert.NotEqual(rect.GetHashCode(), new NativeRectFloat(2, 2, 3, 4).GetHashCode());
    }

    [Fact]
    public void Win32_GetHResult()
    {
        Assert.Equal(HResult.S_OK, Win32.GetHResult(Win32Error.Success));
        Assert.Equal(unchecked((int)0x80070005), (int)Win32.GetHResult(Win32Error.AccessDenied));
        Assert.Equal(HResult.E_FAIL, Win32.GetHResult(unchecked((Win32Error)0x80004005)));
    }

    // ---------- SafeHandles (C-26) ----------

    [Fact]
    public void SafeSelectObjectHandle_KeepsDcAliveAndRestores()
    {
        using var bitmap = CreateFilledBitmap(10, 10, PixelFormat.Format24bppRgb, Color.White);
        using var graphics = Graphics.FromImage(bitmap);
        using (var hdc = SafeGraphicsDcHandle.FromGraphics(graphics))
        {
            var compatibleDc = Gdi32Api.CreateCompatibleDC(hdc);
            using var hBitmap = new SafeHBitmapHandle(bitmap.GetHbitmap());
            var selected = compatibleDc.SelectObject(hBitmap);
            Assert.False(selected.IsInvalid);
            // Disposing the DC first must not release it while the old object is still to be restored
            compatibleDc.Dispose();
            Assert.False(compatibleDc.IsClosed);
            selected.Dispose();
            Assert.True(compatibleDc.IsClosed);
        }
        // The HDC must be released when the SafeGraphicsDcHandle is disposed, otherwise GDI+ throws
        graphics.Clear(Color.Black);
    }

    // ---------- GDI+ blur (C-22) ----------

    [Fact]
    public void GdiPlusApi_NativeHandlesAreAvailable()
    {
        Assert.True(GdiPlusApi.AreNativeHandlesAvailable());
    }

    [Fact]
    public void GdiPlusApi_ApplyBlur_And_DrawWithBlur()
    {
        if (!GdiPlusApi.IsBlurPossible(20))
        {
            // Only on systems without GDI+ effects
            return;
        }
        using var bitmap = CreateFilledBitmap(64, 64, PixelFormat.Format32bppArgb, Color.White);
        using (var g = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.Black))
        {
            g.FillRectangle(brush, 0, 0, 32, 64);
        }
        Assert.True(GdiPlusApi.ApplyBlur(bitmap, new Rectangle(0, 0, 64, 64), 20, false));
        var edge = bitmap.GetPixel(32, 32);
        Assert.InRange(edge.R, 1, 254);

        using var target = CreateFilledBitmap(64, 64, PixelFormat.Format32bppArgb, Color.White);
        using var graphics = Graphics.FromImage(target);
        using var matrix = new Matrix();
        using var imageAttributes = new ImageAttributes();
        Assert.True(GdiPlusApi.DrawWithBlur(graphics, bitmap, new Rectangle(0, 0, 64, 64), matrix, imageAttributes, 20, false));
    }

    // ---------- Helpers ----------

    private static Bitmap CreateFilledBitmap(int width, int height, PixelFormat pixelFormat, Color color)
    {
        var bitmap = new Bitmap(width, height, pixelFormat);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, 0, 0, width, height);
        return bitmap;
    }

    private static List<IconDirEntry> ReadEntries(byte[] bytes)
    {
        var count = BitConverter.ToUInt16(bytes, 4);
        var entries = new List<IconDirEntry>();
        for (int i = 0; i < count; i++)
        {
            var offset = 6 + i * IconDirEntry.Size;
            entries.Add(new IconDirEntry
            {
                Width = bytes[offset],
                Height = bytes[offset + 1],
                ColorCount = bytes[offset + 2],
                Reserved = bytes[offset + 3],
                Planes = BitConverter.ToUInt16(bytes, offset + 4),
                BitCount = BitConverter.ToUInt16(bytes, offset + 6),
                BytesInRes = BitConverter.ToUInt32(bytes, offset + 8),
                ImageOffset = BitConverter.ToUInt32(bytes, offset + 12)
            });
        }
        return entries;
    }
}
