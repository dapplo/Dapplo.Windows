// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Shell32;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;
// System.Drawing has its own StockIconId on .NET 8 and later
using StockIconId = Dapplo.Windows.Shell32.Enums.StockIconId;

namespace Dapplo.Windows.Icons;

/// <summary>
///     The Windows stock icons (the shell's own icons for error, warning, information, shield, folders, drives and more) in any size
/// </summary>
public static class StockIcons
{
    /// <summary>
    ///     The largest size <see cref="GetIcon{TIcon}"/> accepts, in pixels
    /// </summary>
    public const int MaxSize = 1024;

    /// <summary>
    ///     Get a stock icon in the requested size, with its alpha channel (a Bitmap is Format32bppArgb and can be saved as png with transparency).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     This asks SHGetStockIconInfo for the file and index of the icon, and extracts it with SHDefExtractIcon in the requested size, so
    ///     Windows picks (or scales) the best image of the icon for that size. All icon handles are destroyed before this returns.
    ///     </para>
    ///     <para>
    ///     SHGetStockIconInfo only applies <see cref="StockIconFlags.LinkOverlay"/> and <see cref="StockIconFlags.Selected"/> to an icon handle
    ///     in one of the system sizes, so here they are applied to the extracted icon: the link overlay is the <see cref="StockIconId.Link"/>
    ///     stock icon drawn over it in the same size, as the shell does, and selected blends the icon 50% with the system highlight color, as
    ///     an image list does. Other flags are ignored, the method chooses what it asks Windows for.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // The shield for a "needs administrator rights" button
    ///     using var shield = StockIcons.GetIcon&lt;Bitmap&gt;(StockIconId.Shield, 16);
    ///     // The error icon as the big image of a toast
    ///     using var error = StockIcons.GetIcon&lt;Bitmap&gt;(StockIconId.Error, 96);
    ///     error?.Save("error.png", ImageFormat.Png);
    ///     </code>
    /// </example>
    /// <typeparam name="TIcon">Bitmap or Icon (Dapplo.Windows.Wpf has ToBitmapSource() to convert these to a WPF BitmapSource)</typeparam>
    /// <param name="id">StockIconId of the icon</param>
    /// <param name="size">int with the width and height in pixels, 1 to <see cref="MaxSize"/></param>
    /// <param name="extraFlags">StockIconFlags, <see cref="StockIconFlags.LinkOverlay"/> and/or <see cref="StockIconFlags.Selected"/></param>
    /// <returns>TIcon of exactly <paramref name="size"/> x <paramref name="size"/> pixels, null when Windows has no icon for the id</returns>
    /// <exception cref="ArgumentOutOfRangeException">size is not between 1 and <see cref="MaxSize"/></exception>
    /// <exception cref="NotSupportedException">TIcon is not Bitmap or Icon</exception>
    public static TIcon GetIcon<TIcon>(StockIconId id, int size, StockIconFlags extraFlags = 0) where TIcon : class
    {
        IconHelper.ThrowIfUnsupportedIconType<TIcon>();
        if (size < 1 || size > MaxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"The size must be between 1 and {MaxSize} pixels.");
        }
        var linkOverlay = (extraFlags & StockIconFlags.LinkOverlay) != 0;
        var selected = (extraFlags & StockIconFlags.Selected) != 0;

        var iconHandle = ExtractStockIcon(id, size);
        if (iconHandle == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            // Without changes an Icon is a copy of the handle, which keeps everything (all of it is the requested size)
            if (typeof(TIcon) == typeof(Icon) && !linkOverlay && !selected)
            {
                return IconHelper.IconHandleTo<TIcon>(iconHandle);
            }
            var bitmap = IconHelper.IconHandleToArgbBitmap(iconHandle);
            if (bitmap is null)
            {
                return null;
            }
            try
            {
                if (linkOverlay)
                {
                    DrawLinkOverlay(bitmap, size);
                }
                if (selected)
                {
                    BlendWithHighlight(bitmap);
                }
                if (typeof(TIcon) == typeof(Bitmap))
                {
                    var result = bitmap as TIcon;
                    bitmap = null;
                    return result;
                }
                // Icon: GetHicon keeps the alpha channel
                var hIcon = bitmap.GetHicon();
                try
                {
                    return IconHelper.IconHandleTo<TIcon>(hIcon);
                }
                finally
                {
                    NativeIconMethods.DestroyIcon(hIcon);
                }
            }
            finally
            {
                bitmap?.Dispose();
            }
        }
        finally
        {
            NativeIconMethods.DestroyIcon(iconHandle);
        }
    }

    /// <summary>
    ///     The icon handle of a stock icon in the requested size, the caller destroys it; IntPtr.Zero when Windows has none for the id
    /// </summary>
    private static IntPtr ExtractStockIcon(StockIconId id, int size)
    {
        var info = StockIconInfo.Create();
        if (Shell32Api.SHGetStockIconInfo(id, StockIconFlags.IconLocation, ref info) != 0 || string.IsNullOrEmpty(info.Path))
        {
            return IntPtr.Zero;
        }
        var large = IntPtr.Zero;
        var small = IntPtr.Zero;
        try
        {
            // The small icon isn't needed, but SHDefExtractIcon wants a size for it
            var hResult = Shell32Api.SHDefExtractIcon(info.Path, info.IconIndex, 0, out large, out small, Shell32Api.MakeIconSize(size, 16));
            if (hResult != 0 || large == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            var result = large;
            large = IntPtr.Zero;
            return result;
        }
        finally
        {
            if (small != IntPtr.Zero)
            {
                NativeIconMethods.DestroyIcon(small);
            }
            if (large != IntPtr.Zero)
            {
                NativeIconMethods.DestroyIcon(large);
            }
        }
    }

    /// <summary>
    ///     Draw the shortcut overlay (the Link stock icon, which has the arrow in a corner of an otherwise transparent icon) over the bitmap
    /// </summary>
    private static void DrawLinkOverlay(Bitmap bitmap, int size)
    {
        var overlayHandle = ExtractStockIcon(StockIconId.Link, size);
        if (overlayHandle == IntPtr.Zero)
        {
            return;
        }
        try
        {
            using var overlay = IconHelper.IconHandleToArgbBitmap(overlayHandle);
            if (overlay is null)
            {
                return;
            }
            using var graphics = Graphics.FromImage(bitmap);
            graphics.DrawImage(overlay, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        }
        finally
        {
            NativeIconMethods.DestroyIcon(overlayHandle);
        }
    }

    /// <summary>
    ///     Blend the colors 50% with the system highlight color, the alpha channel stays (like ILD_SELECTED of an image list)
    /// </summary>
    private static unsafe void BlendWithHighlight(Bitmap bitmap)
    {
        var highlight = SystemColors.Highlight;
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < data.Height; y++)
            {
                var row = (byte*)data.Scan0 + y * data.Stride;
                for (var x = 0; x < data.Width; x++, row += 4)
                {
                    row[0] = (byte)((row[0] + highlight.B) / 2);
                    row[1] = (byte)((row[1] + highlight.G) / 2);
                    row[2] = (byte)((row[2] + highlight.R) / 2);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
