// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Structs.PixelFormats;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Xunit;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Generic;
using Dapplo.Log.XUnit;

namespace Dapplo.Windows.Tests;

public class IconTests
{
    public IconTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test getting an Icon for a top level window
    /// </summary>
    [WpfFact]
    public void TestIcon_GetIcon()
    {
        // Start a process to test against
        using (var process = Process.Start("charmap.exe"))
        {
            // Make sure it's started
            Assert.NotNull(process);
            // Wait until the process started it's message pump (listening for input)
            if (!process.WaitForInputIdle(2000))
            {
                Assert.Fail("Test-Process didn't get ready.");
                return;
            }

            User32Api.SetWindowText(process.MainWindowHandle, "TestIcon_GetIcon");

            var window = InteropWindowQuery.GetTopLevelWindows().First();
            var icon = window.GetIcon<BitmapSource>();
            Assert.NotNull(icon);

            // Kill the process
            process.Kill();
        }
    }

    /// <summary>
    ///     Test getting an Icon for the desktop, which doesn't have one
    /// </summary>
    [WpfFact]
    public void TestIcon_GetIcon_Null()
    {
        var window = InteropWindowQuery.GetDesktopWindow();
        var icon = window.GetIcon<BitmapSource>();

        Assert.Null(icon);
    }

    /// <summary>
    ///     Test getting system-preferred icon sizes
    /// </summary>
    [Fact]
    public void TestIcon_GetSystemIconSizes()
    {
        // Test small icon metrics
        var smallWidth = IconHelper.GetSmallIconWidth();
        var smallHeight = IconHelper.GetSmallIconHeight();
        Assert.True(smallWidth > 0, "Small icon width should be greater than 0");
        Assert.True(smallHeight > 0, "Small icon height should be greater than 0");

        // Test standard/large icon metrics
        var standardWidth = IconHelper.GetStandardIconWidth();
        var standardHeight = IconHelper.GetStandardIconHeight();
        Assert.True(standardWidth > 0, "Standard icon width should be greater than 0");
        Assert.True(standardHeight > 0, "Standard icon height should be greater than 0");

        // Standard icons should typically be larger than or equal to small icons
        Assert.True(standardWidth >= smallWidth, "Standard icon width should be >= small icon width");
        Assert.True(standardHeight >= smallHeight, "Standard icon height should be >= small icon height");

        // Test icon spacing metrics
        var spacingWidth = IconHelper.GetIconSpacingWidth();
        var spacingHeight = IconHelper.GetIconSpacingHeight();
        Assert.True(spacingWidth > 0, "Icon spacing width should be greater than 0");
        Assert.True(spacingHeight > 0, "Icon spacing height should be greater than 0");

        // Icon spacing should be greater than or equal to standard icon size
        Assert.True(spacingWidth >= standardWidth, "Icon spacing width should be >= standard icon width");
        Assert.True(spacingHeight >= standardHeight, "Icon spacing height should be >= standard icon height");
    }

    //[Fact]
    private void TestIcon_V6Loaded()
    {
        try
        {
            var idiApplication = new IntPtr(32512);
            IconHelper.LoadIconWithSystemMetrics<BitmapSource>(IntPtr.Zero, idiApplication, Icons.Enums.IconMetricSize.SmallIcon);
        } catch {
            // Just to make sure it's loaded
        }
        var comctl32 = Process.GetCurrentProcess()
            .Modules
            .OfType<ProcessModule>()
            .FirstOrDefault(m => m.ModuleName.Equals("comctl32.dll", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(comctl32);
        Assert.True(comctl32.FileVersionInfo.FileMajorPart >= 6, "comctl32.dll version should be 6 or higher");
    }

    /// <summary>
    ///     Test getting system icon size using the helper method
    /// </summary>
    [Fact]
    public void TestIcon_GetSystemIconSize()
    {
        var smallSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.SmallIcon);
        Assert.True(smallSize.Width > 0, "Small icon size width should be greater than 0");
        Assert.True(smallSize.Height > 0, "Small icon size height should be greater than 0");

        var standardSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.StandardIcon);
        Assert.True(standardSize.Width > 0, "Standard icon size width should be greater than 0");
        Assert.True(standardSize.Height > 0, "Standard icon size height should be greater than 0");

        // Standard should be >= small
        Assert.True(standardSize.Width >= smallSize.Width, "Standard size width should be >= small size width");
        Assert.True(standardSize.Height >= smallSize.Height, "Standard size height should be >= small size height");
    }

    /// <summary>
    ///     Test LoadIconMetric with system stock icons
    /// </summary>
    [WpfFact]
    private void TestIcon_LoadIconMetric()
    {
        var idiApplication = new IntPtr(32512);

        try
        {
            // Load small icon using LoadIconMetric
            var smallIcon = IconHelper.LoadIconWithSystemMetrics<BitmapSource>(IntPtr.Zero, idiApplication, Icons.Enums.IconMetricSize.SmallIcon);
            Assert.NotNull(smallIcon);

            var expectedSmallSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.SmallIcon);
            Assert.Equal(expectedSmallSize.Width, smallIcon.PixelWidth);
            Assert.Equal(expectedSmallSize.Height, smallIcon.PixelHeight);

            // Load standard icon using LoadIconMetric
            var standardIcon = IconHelper.LoadIconWithSystemMetrics<BitmapSource>(IntPtr.Zero, idiApplication, Icons.Enums.IconMetricSize.StandardIcon);
            Assert.NotNull(standardIcon);

            var expectedStandardSize = IconHelper.GetSystemIconSize(Icons.Enums.IconMetricSize.StandardIcon);
            Assert.Equal(expectedStandardSize.Width, standardIcon.PixelWidth);
            Assert.Equal(expectedStandardSize.Height, standardIcon.PixelHeight);
        }
        catch (EntryPointNotFoundException)
        {
            // Skip test on systems without LoadIconMetric API
        }
    }

    /// <summary>
    ///     Test LoadIconWithScaleDown with system stock icons
    /// </summary>
    //[WpfFact]
    private void TestIcon_LoadIconWithScaleDown()
    {
        // IDI_QUESTION is 32514
        var idiQuestion = new IntPtr(32514);

        // Load icon at a specific size
        const int targetSize = 24;
        var icon = IconHelper.LoadIconWithScaleDown<BitmapSource>(IntPtr.Zero, idiQuestion, targetSize, targetSize);
        Assert.NotNull(icon);

        // The icon should be scaled to the requested size
        Assert.Equal(targetSize, icon.PixelWidth);
        Assert.Equal(targetSize, icon.PixelHeight);
    }

    /// <summary>
    ///     Test creating an icon file with multiple sizes using IconFileWriter
    /// </summary>
    [Fact]
    public void TestIconFileWriter_WriteIconFile()
    {
        // Create test bitmaps of different sizes
        var images = new List<Bitmap>
        {
            new Bitmap(16, 16),
            new Bitmap(32, 32),
            new Bitmap(48, 48),
            new Bitmap(256, 256)
        };

        // Fill each bitmap with a different color for visual verification
        using (var g1 = System.Drawing.Graphics.FromImage(images[0]))
            g1.Clear(System.Drawing.Color.Red);
        using (var g2 = System.Drawing.Graphics.FromImage(images[1]))
            g2.Clear(System.Drawing.Color.Green);
        using (var g3 = System.Drawing.Graphics.FromImage(images[2]))
            g3.Clear(System.Drawing.Color.Blue);
        using (var g4 = System.Drawing.Graphics.FromImage(images[3]))
            g4.Clear(System.Drawing.Color.Yellow);

        // Write to a memory stream
        using (var stream = new MemoryStream())
        {
            IconFileWriter.WriteIconFile(stream, images);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Icon file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
            {
                var reserved = reader.ReadUInt16();
                var type = reader.ReadUInt16();
                var count = reader.ReadUInt16();

                Assert.Equal(0, reserved);
                Assert.Equal(1, type); // 1 = icon
                Assert.Equal(4, count); // 4 images
            }
        }

        // Clean up
        foreach (var image in images)
        {
            image.Dispose();
        }
    }

    /// <summary>
    ///     Test creating a cursor file with hotspot information
    /// </summary>
    [Fact]
    public void TestIconFileWriter_WriteCursorFile()
    {
        // Create test bitmap for cursor
        var bitmap = new Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        var cursorData = new List<(Image, Point)>
        {
            (bitmap, new Point(16, 16)) // Hotspot in the center
        };

        // Write to a memory stream
        using (var stream = new MemoryStream())
        {
            IconFileWriter.WriteCursorFile(stream, cursorData);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Cursor file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
            {
                var reserved = reader.ReadUInt16();
                var type = reader.ReadUInt16();
                var count = reader.ReadUInt16();

                Assert.Equal(0, reserved);
                Assert.Equal(2, type); // 2 = cursor
                Assert.Equal(1, count);
            }
        }

        bitmap.Dispose();
    }

    /// <summary>
    ///     Test IconDir structure creation
    /// </summary>
    [Fact]
    public void TestIconDir_CreateIcon()
    {
        var iconDir = Icons.Structs.IconDir.CreateIcon(3);

        Assert.Equal(0, iconDir.Reserved);
        Assert.Equal(1, iconDir.Type);
        Assert.Equal(3, iconDir.Count);
    }

    /// <summary>
    ///     Test IconDir structure creation for cursor
    /// </summary>
    [Fact]
    public void TestIconDir_CreateCursor()
    {
        var iconDir = Icons.Structs.IconDir.CreateCursor(2);

        Assert.Equal(0, iconDir.Reserved);
        Assert.Equal(2, iconDir.Type);
        Assert.Equal(2, iconDir.Count);
    }

    /// <summary>
    ///     Test IconDirEntry structure creation
    /// </summary>
    [Fact]
    public void TestIconDirEntry_CreateForIcon()
    {
        var entry = Icons.Structs.IconDirEntry.CreateForIcon(32, 32, 32, 1024, 128);

        Assert.Equal(32, entry.Width);
        Assert.Equal(32, entry.Height);
        Assert.Equal(0, entry.ColorCount);
        Assert.Equal(0, entry.Reserved);
        Assert.Equal(0, entry.Planes);
        Assert.Equal(32, entry.BitCount);
        Assert.Equal(1024u, entry.BytesInRes);
        Assert.Equal(128u, entry.ImageOffset);
    }

    /// <summary>
    ///     Test IconDirEntry structure creation with 256x256 size (should use 0)
    /// </summary>
    [Fact]
    public void TestIconDirEntry_Create256x256()
    {
        var entry = Icons.Structs.IconDirEntry.CreateForIcon(256, 256, 32, 2048, 256);

        Assert.Equal(0, entry.Width);  // 0 represents 256
        Assert.Equal(0, entry.Height); // 0 represents 256
    }

    /// <summary>
    ///     Test GrpIconDir structure creation
    /// </summary>
    [Fact]
    public void TestGrpIconDir_CreateIcon()
    {
        var grpIconDir = Icons.Structs.GrpIconDir.CreateIcon(5);

        Assert.Equal(0, grpIconDir.Reserved);
        Assert.Equal(1, grpIconDir.Type);
        Assert.Equal(5, grpIconDir.Count);
    }

    /// <summary>
    ///     Test GrpIconDirEntry structure creation
    /// </summary>
    [Fact]
    public void TestGrpIconDirEntry_CreateForIcon()
    {
        var entry = Icons.Structs.GrpIconDirEntry.CreateForIcon(48, 48, 32, 4096, 1);

        Assert.Equal(48, entry.Width);
        Assert.Equal(48, entry.Height);
        Assert.Equal(0, entry.ColorCount);
        Assert.Equal(0, entry.Reserved);
        Assert.Equal(0, entry.Planes);
        Assert.Equal(32, entry.BitCount);
        Assert.Equal(4096u, entry.BytesInRes);
        Assert.Equal(1, entry.Id);
    }

    /// <summary>
    ///     Test writing GrpIconDir and GrpIconDirEntry structures
    /// </summary>
    [Fact]
    public void TestIconFileWriter_WriteGrpIconStructures()
    {
        // Test writing group icon structures for PE resources
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            // Write group icon directory
            var grpIconDir = Icons.Structs.GrpIconDir.CreateIcon(2);
            IconFileWriter.WriteGrpIconDir(writer, grpIconDir);

            // Write group icon directory entries
            var entry1 = Icons.Structs.GrpIconDirEntry.CreateForIcon(16, 16, 32, 512, 1);
            var entry2 = Icons.Structs.GrpIconDirEntry.CreateForIcon(32, 32, 32, 2048, 2);
            IconFileWriter.WriteGrpIconDirEntry(writer, entry1);
            IconFileWriter.WriteGrpIconDirEntry(writer, entry2);

            writer.Flush();

            // Verify the written data
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream))
            {
                // Read GRPICONDIR
                var reserved = reader.ReadUInt16();
                var type = reader.ReadUInt16();
                var count = reader.ReadUInt16();
                Assert.Equal(0, reserved);
                Assert.Equal(1, type);
                Assert.Equal(2, count);

                // Read first GRPICONDIRENTRY
                var width1 = reader.ReadByte();
                var height1 = reader.ReadByte();
                reader.ReadByte(); // color count
                reader.ReadByte(); // reserved
                reader.ReadUInt16(); // planes
                reader.ReadUInt16(); // bitcount
                reader.ReadUInt32(); // bytes in res
                var id1 = reader.ReadUInt16();
                Assert.Equal(16, width1);
                Assert.Equal(16, height1);
                Assert.Equal(1, id1);

                // Read second GRPICONDIRENTRY
                var width2 = reader.ReadByte();
                var height2 = reader.ReadByte();
                reader.ReadByte(); // color count
                reader.ReadByte(); // reserved
                reader.ReadUInt16(); // planes
                reader.ReadUInt16(); // bitcount
                reader.ReadUInt32(); // bytes in res
                var id2 = reader.ReadUInt16();
                Assert.Equal(32, width2);
                Assert.Equal(32, height2);
                Assert.Equal(2, id2);
            }
        }
    }

    /// <summary>
    ///     Test backward compatibility - existing WriteIcon method
    /// </summary>
    [Fact]
    public void TestIconHelper_WriteIcon_BackwardCompatibility()
    {
        // Create test bitmap
        var bitmap = new Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.Blue);
        }

        var images = new List<Image> { bitmap };

        // Write using the old WriteIcon method (which now delegates to IconFileWriter)
        using (var stream = new MemoryStream())
        {
            IconHelper.WriteIcon(stream, images);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Icon file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
            {
                var reserved = reader.ReadUInt16();
                var type = reader.ReadUInt16();
                var count = reader.ReadUInt16();

                Assert.Equal(0, reserved);
                Assert.Equal(1, type); // 1 = icon
                Assert.Equal(1, count);
            }
        }

        bitmap.Dispose();
    }

    /// <summary>
    ///     Test TryGetCurrentCursor method
    /// </summary>
    [Fact]
    public void TestCursorHelper_TryGetCurrentCursor()
    {
        CapturedCursor capturedCursor;
        var result = CursorHelper.TryGetCurrentCursor(out capturedCursor);
        
        // In some environments (CI, headless), cursor may not be available
        // So we just verify the method returns a boolean and doesn't crash
        if (result)
        {
            Assert.NotNull(capturedCursor);
            capturedCursor.Dispose();
        }
    }

    /// <summary>
    ///     Test DrawCursorOnBitmap with a modern alpha cursor
    /// </summary>
    [Fact]
    public void TestCursorHelper_DrawCursorOnBitmap_ModernCursor()
    {
        // Create a target bitmap
        var targetBitmap = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(targetBitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        // Create a cursor with alpha channel (modern cursor, no mask)
        var cursorBitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(cursorBitmap))
        {
            g.Clear(System.Drawing.Color.Transparent);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(128, 255, 0, 0)))
            {
                g.FillEllipse(brush, 0, 0, 32, 32);
            }
        }

        var cursor = new CapturedCursor
        {
            ColorLayer = cursorBitmap,
            MaskLayer = null, // Modern cursor
            HotSpot = new NativePoint(16, 16),
            Size = new NativeSize(32, 32)
        };

        // Draw cursor at position (10, 10)
        CursorHelper.DrawCursorOnBitmap(targetBitmap, cursor, new NativePoint(10, 10));

        // Verify that the cursor was drawn
        // The center of the cursor should have some red color
        var centerColor = targetBitmap.GetPixel(26, 26); // 10 + 16 (center of cursor)
        Assert.True(centerColor.R > 100, "Center of cursor should have red color");
        
        // Corners should still be white (cursor is transparent there)
        var cornerColor = targetBitmap.GetPixel(0, 0);
        Assert.Equal(255, cornerColor.R);
        Assert.Equal(255, cornerColor.G);
        Assert.Equal(255, cornerColor.B);

        cursor.Dispose();
        targetBitmap.Dispose();
    }

    /// <summary>
    ///     Test DrawCursorOnBitmap with a legacy cursor with mask
    /// </summary>
    [Fact]
    public void TestCursorHelper_DrawCursorOnBitmap_LegacyCursor()
    {
        // Create a target bitmap
        var targetBitmap = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(targetBitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        // Create a cursor color layer
        var cursorBitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(cursorBitmap))
        {
            g.Clear(System.Drawing.Color.Black);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
            {
                g.FillRectangle(brush, 8, 8, 16, 16);
            }
        }

        // Create a mask layer
        var maskBitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(maskBitmap))
        {
            g.Clear(System.Drawing.Color.Black); // Black = transparent area
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
            {
                g.FillRectangle(brush, 8, 8, 16, 16); // White = opaque area for XOR
            }
        }

        var cursor = new CapturedCursor
        {
            ColorLayer = cursorBitmap,
            MaskLayer = maskBitmap, // Legacy cursor with mask
            HotSpot = new NativePoint(16, 16),
            Size = new NativeSize(32, 32)
        };

        // Draw cursor at position (20, 20)
        CursorHelper.DrawCursorOnBitmap(targetBitmap, cursor, new NativePoint(20, 20));

        // Verify that the cursor was drawn
        // The center of the cursor (where the white square is) should be modified
        var centerColor = targetBitmap.GetPixel(36, 36); // 20 + 16 (center of white square)
        // With white mask and white color on white background: (255 & 255) ^ 255 = 0 (black - inverted!)
        Assert.True(centerColor.R < 255 || centerColor.G < 255 || centerColor.B < 255, 
            "Center should be affected by cursor drawing (XOR should invert white to black)");

        cursor.Dispose();
        targetBitmap.Dispose();
    }

    /// <summary>
    ///     Test DrawCursorOnBitmap with scaling
    /// </summary>
    [Fact]
    public void TestCursorHelper_DrawCursorOnBitmap_WithScaling()
    {
        // Create a target bitmap
        var targetBitmap = new Bitmap(200, 200, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(targetBitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        // Create a small cursor
        var cursorBitmap = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(cursorBitmap))
        {
            g.Clear(System.Drawing.Color.Transparent);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 255)))
            {
                g.FillRectangle(brush, 0, 0, 16, 16);
            }
        }

        var cursor = new CapturedCursor
        {
            ColorLayer = cursorBitmap,
            MaskLayer = null,
            HotSpot = new NativePoint(8, 8),
            Size = new NativeSize(16, 16)
        };

        // Draw cursor at position (50, 50) scaled to 64x64
        CursorHelper.DrawCursorOnBitmap(targetBitmap, cursor, new NativePoint(50, 50), new NativeSize(64, 64));

        // Verify that the cursor was drawn and scaled
        var centerColor = targetBitmap.GetPixel(82, 82); // 50 + 32 (center of scaled cursor)
        Assert.True(centerColor.B > 200, "Center of cursor should have blue color");

        cursor.Dispose();
        targetBitmap.Dispose();
    }

    /// <summary>
    ///     Test DrawCursorOnBitmap on 24-bit RGB bitmap
    /// </summary>
    [Fact]
    public void TestCursorHelper_DrawCursorOnBitmap_24bppTarget()
    {
        // Create a 24-bit target bitmap
        var targetBitmap = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using (var g = System.Drawing.Graphics.FromImage(targetBitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        // Create a cursor with alpha channel
        var cursorBitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = System.Drawing.Graphics.FromImage(cursorBitmap))
        {
            g.Clear(System.Drawing.Color.Transparent);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 255, 0)))
            {
                g.FillEllipse(brush, 0, 0, 32, 32);
            }
        }

        var cursor = new CapturedCursor
        {
            ColorLayer = cursorBitmap,
            MaskLayer = null,
            HotSpot = new NativePoint(16, 16),
            Size = new NativeSize(32, 32)
        };

        // Draw cursor at position (10, 10)
        CursorHelper.DrawCursorOnBitmap(targetBitmap, cursor, new NativePoint(10, 10));

        // Verify that the cursor was drawn
        var centerColor = targetBitmap.GetPixel(26, 26);
        Assert.True(centerColor.G > 200, "Center of cursor should have green color");

        cursor.Dispose();
        targetBitmap.Dispose();
    }

}