// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Xunit;
using Xunit.Abstractions;

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
        using (var process = Process.Start("notepad.exe"))
        {
            // Make sure it's started
            Assert.NotNull(process);
            // Wait until the process started it's message pump (listening for input)
            process.WaitForInputIdle();
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
    public void TestIcon_LoadIconMetric()
    {
        // IDI_APPLICATION is 32512
        var idiApplication = new IntPtr(32512);

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

    /// <summary>
    ///     Test LoadIconWithScaleDown with system stock icons
    /// </summary>
    [WpfFact]
    public void TestIcon_LoadIconWithScaleDown()
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
        var images = new List<System.Drawing.Bitmap>
        {
            new System.Drawing.Bitmap(16, 16),
            new System.Drawing.Bitmap(32, 32),
            new System.Drawing.Bitmap(48, 48),
            new System.Drawing.Bitmap(256, 256)
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
        using (var stream = new System.IO.MemoryStream())
        {
            IconFileWriter.WriteIconFile(stream, images);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Icon file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new System.IO.BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
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
        var bitmap = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.White);
        }

        var cursorData = new List<(System.Drawing.Image, System.Drawing.Point)>
        {
            (bitmap, new System.Drawing.Point(16, 16)) // Hotspot in the center
        };

        // Write to a memory stream
        using (var stream = new System.IO.MemoryStream())
        {
            IconFileWriter.WriteCursorFile(stream, cursorData);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Cursor file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new System.IO.BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
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
        using (var stream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(stream))
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
            using (var reader = new System.IO.BinaryReader(stream))
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
        var bitmap = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.Blue);
        }

        var images = new List<System.Drawing.Image> { bitmap };

        // Write using the old WriteIcon method (which now delegates to IconFileWriter)
        using (var stream = new System.IO.MemoryStream())
        {
            IconHelper.WriteIcon(stream, images);

            // Verify the stream has data
            Assert.True(stream.Length > 0, "Icon file should have data");

            // Verify the header
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var reader = new System.IO.BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true))
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
}