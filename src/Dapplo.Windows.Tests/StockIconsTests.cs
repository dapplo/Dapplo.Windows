// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Shell32;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;
using Dapplo.Windows.User32;
using Xunit;
// System.Drawing has its own StockIconId on .NET 8 and later
using StockIconId = Dapplo.Windows.Shell32.Enums.StockIconId;

namespace Dapplo.Windows.Tests;

/// <summary>
///     The Windows stock icons: Shell32Api.SHGetStockIconInfo / SHDefExtractIcon and StockIcons.GetIcon
/// </summary>
public class StockIconsTests
{
    private const int E_INVALIDARG = unchecked((int)0x80070057);

    public StockIconsTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    public void StockIconInfo_Create_SetsTheSize()
    {
        var info = StockIconInfo.Create();
        Assert.Equal((uint)Marshal.SizeOf(typeof(StockIconInfo)), info.Size);
        // The ByValTStr of 260 characters is in the size, so the structure is passed with its path buffer
        Assert.True(info.Size > 520);
    }

    [Fact]
    public void SHGetStockIconInfo_WithoutSize_IsInvalid_WithCreate_GivesTheLocation()
    {
        var withoutSize = new StockIconInfo();
        Assert.Equal(E_INVALIDARG, Shell32Api.SHGetStockIconInfo(StockIconId.Error, StockIconFlags.IconLocation, ref withoutSize));

        var info = StockIconInfo.Create();
        Assert.Equal(0, Shell32Api.SHGetStockIconInfo(StockIconId.Error, StockIconFlags.IconLocation, ref info));
        Assert.False(string.IsNullOrEmpty(info.Path));
        Assert.Equal(IntPtr.Zero, info.IconHandle);
    }

    [Fact]
    public void SHGetStockIconInfo_Icon_ReturnsAHandleToDestroy()
    {
        var info = StockIconInfo.Create();
        Assert.Equal(0, Shell32Api.SHGetStockIconInfo(StockIconId.Warning, StockIconFlags.Icon | StockIconFlags.SmallIcon, ref info));
        Assert.NotEqual(IntPtr.Zero, info.IconHandle);
        Assert.True(NativeIconMethods.DestroyIcon(info.IconHandle));
    }

    [Fact]
    public void SHDefExtractIcon_ReturnsBothSizes()
    {
        var info = StockIconInfo.Create();
        Assert.Equal(0, Shell32Api.SHGetStockIconInfo(StockIconId.Info, StockIconFlags.IconLocation, ref info));
        Assert.Equal(0, Shell32Api.SHDefExtractIcon(info.Path, info.IconIndex, 0, out var large, out var small, Shell32Api.MakeIconSize(48, 16)));
        try
        {
            Assert.NotEqual(IntPtr.Zero, large);
            Assert.NotEqual(IntPtr.Zero, small);
            using var largeIcon = Icon.FromHandle(large);
            using var smallIcon = Icon.FromHandle(small);
            Assert.Equal(48, largeIcon.Width);
            Assert.Equal(16, smallIcon.Width);
        }
        finally
        {
            NativeIconMethods.DestroyIcon(large);
            NativeIconMethods.DestroyIcon(small);
        }
    }

    [Fact]
    public void MakeIconSize_LargeInTheLowWord_SmallInTheHighWord()
    {
        Assert.Equal(0x00100060u, Shell32Api.MakeIconSize(96, 16));
        Assert.Equal(0xFFFF0001u, Shell32Api.MakeIconSize(1, 65535));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shell32Api.MakeIconSize(0, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => Shell32Api.MakeIconSize(96, 65536));
    }

    /// <summary>
    ///     Exactly the requested size, transparent corners, and the alpha channel is kept: the anti-aliased edges are partly transparent
    /// </summary>
    [Theory]
    [InlineData(StockIconId.Error, 16)]
    [InlineData(StockIconId.Error, 32)]
    [InlineData(StockIconId.Error, 96)]
    [InlineData(StockIconId.Error, 256)]
    [InlineData(StockIconId.Warning, 16)]
    [InlineData(StockIconId.Warning, 32)]
    [InlineData(StockIconId.Warning, 96)]
    [InlineData(StockIconId.Warning, 256)]
    public void GetIcon_Bitmap_HasTheSizeAndAnAlphaChannel(StockIconId id, int size)
    {
        using var bitmap = StockIcons.GetIcon<Bitmap>(id, size);
        Assert.NotNull(bitmap);
        Assert.Equal(size, bitmap.Width);
        Assert.Equal(size, bitmap.Height);
        Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);

        Assert.Equal(0, bitmap.GetPixel(0, 0).A);
        Assert.Equal(0, bitmap.GetPixel(size - 1, 0).A);
        Assert.Equal(0, bitmap.GetPixel(0, size - 1).A);
        Assert.Equal(0, bitmap.GetPixel(size - 1, size - 1).A);

        var opaque = 0;
        var partial = 0;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var alpha = bitmap.GetPixel(x, y).A;
                if (alpha == 255)
                {
                    opaque++;
                }
                else if (alpha > 0)
                {
                    partial++;
                }
            }
        }
        Assert.True(opaque > size, $"Only {opaque} opaque pixels");
        Assert.True(partial > 0, "No partly transparent pixels: the alpha channel was lost");
    }

    [Fact]
    public void GetIcon_Icon_HasTheSize()
    {
        using var icon = StockIcons.GetIcon<Icon>(StockIconId.Shield, 96);
        Assert.NotNull(icon);
        Assert.Equal(96, icon.Width);
        Assert.Equal(96, icon.Height);
    }

    [Fact]
    public void GetIcon_SavedAsPng_KeepsTheTransparency()
    {
        using var bitmap = StockIcons.GetIcon<Bitmap>(StockIconId.Error, 96);
        using var stream = new System.IO.MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        using var loaded = new Bitmap(stream);
        Assert.Equal(0, loaded.GetPixel(0, 0).A);
        Assert.Equal(bitmap.GetPixel(48, 48), loaded.GetPixel(48, 48));
    }

    [Theory]
    [InlineData((StockIconId)1000)]
    [InlineData(StockIconId.Invalid)]
    public void GetIcon_UnknownId_ReturnsNull(StockIconId id)
    {
        Assert.Null(StockIcons.GetIcon<Bitmap>(id, 32));
        Assert.Null(StockIcons.GetIcon<Icon>(id, 32));
    }

    [Fact]
    public void GetIcon_InvalidSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StockIcons.GetIcon<Bitmap>(StockIconId.Error, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StockIcons.GetIcon<Bitmap>(StockIconId.Error, StockIcons.MaxSize + 1));
        Assert.Throws<NotSupportedException>(() => StockIcons.GetIcon<string>(StockIconId.Error, 32));
    }

    /// <summary>
    ///     The link overlay and the selected state change pixels, the size and transparent corners stay
    /// </summary>
    [Fact]
    public void GetIcon_LinkOverlayAndSelected_ChangeTheIcon()
    {
        using var plain = StockIcons.GetIcon<Bitmap>(StockIconId.Folder, 48);
        using var link = StockIcons.GetIcon<Bitmap>(StockIconId.Folder, 48, StockIconFlags.LinkOverlay);
        using var selected = StockIcons.GetIcon<Bitmap>(StockIconId.Folder, 48, StockIconFlags.Selected);
        Assert.Equal(48, link.Width);
        Assert.Equal(48, selected.Width);
        Assert.True(Differs(plain, link), "The link overlay changed nothing");
        Assert.True(Differs(plain, selected), "Selected changed nothing");
        using var linkIcon = StockIcons.GetIcon<Icon>(StockIconId.Folder, 48, StockIconFlags.LinkOverlay);
        Assert.Equal(48, linkIcon.Width);
    }

    private static bool Differs(Bitmap first, Bitmap second)
    {
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    ///     Every icon handle (USER objects) and every bitmap of GetIconInfoEx (GDI objects) is destroyed
    /// </summary>
    [Fact]
    public void GetIcon_ManyCalls_LeakNoHandles()
    {
        // Warm up: the first calls load the icon resources and caches
        for (var i = 0; i < 5; i++)
        {
            StockIcons.GetIcon<Bitmap>(StockIconId.Error, 96)?.Dispose();
            StockIcons.GetIcon<Icon>(StockIconId.Warning, 32)?.Dispose();
            StockIcons.GetIcon<Bitmap>(StockIconId.Folder, 48, StockIconFlags.LinkOverlay | StockIconFlags.Selected)?.Dispose();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var gdiBefore = User32Api.GetGuiResourcesGdiCount();
        var userBefore = User32Api.GetGuiResourcesUserCount();

        for (var i = 0; i < 200; i++)
        {
            StockIcons.GetIcon<Bitmap>(StockIconId.Error, 96)?.Dispose();
            StockIcons.GetIcon<Icon>(StockIconId.Warning, 32)?.Dispose();
            StockIcons.GetIcon<Bitmap>(StockIconId.Folder, 48, StockIconFlags.LinkOverlay | StockIconFlags.Selected)?.Dispose();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var gdiAfter = User32Api.GetGuiResourcesGdiCount();
        var userAfter = User32Api.GetGuiResourcesUserCount();

        // A leak per call would be hundreds of handles, a few may come from caches
        Assert.True(gdiAfter <= gdiBefore + 10, $"GDI objects: {gdiBefore} before, {gdiAfter} after 600 calls");
        Assert.True(userAfter <= userBefore + 10, $"USER objects: {userBefore} before, {userAfter} after 600 calls");
    }
}
