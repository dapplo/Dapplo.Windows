// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Structs.PixelFormats;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Icons.Enums;
using Dapplo.Windows.Icons.Structs;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Helper methods for using cursor information
/// </summary>
public static class CursorHelper
{
    /// <summary>
    /// Gets the base size of the mouse cursor, in pixels, as configured by the user in the system settings.
    /// </summary>
    /// <remarks>This method reads the 'CursorBaseSize' value from the Windows Registry under 'Control
    /// Panel\Cursors'. If the value is not found or an error occurs while accessing the registry, a default size of 32
    /// pixels is returned.</remarks>
    /// <returns>The size of the mouse cursor in pixels. Returns 32 if the value cannot be retrieved from the system settings.</returns>
    public static int GetCursorBaseSize()
    {
        // Reads the "Make mouse pointer bigger" slider value from Registry
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
            {
                if (key?.GetValue("CursorBaseSize") is int size)
                {
                    return size;
                }
            }
        }
        catch
        {
            // Empty by design
        }
        return 32; // Default
    }


    /// <summary>
    /// Attempts to retrieve information about the current cursor and capture its visual and positional properties.
    /// </summary>
    /// <remarks>This method captures both system and custom cursors, ensuring the cursor is visible before
    /// extracting its details. For system cursors, it attempts to load a high-DPI version to improve image quality. The
    /// captured information includes the cursor's size, hotspot, and image layers, which can be used for further
    /// processing or display.</remarks>
    /// <param name="result">When this method returns, contains a CapturedCursor instance populated with details about the current cursor,
    /// including its size, hotspot, and image layers. This parameter is passed uninitialized.</param>
    /// <returns>true if the current cursor information was successfully retrieved and captured; otherwise, false.</returns>
    public static bool TryGetCurrentCursor(out CapturedCursor result)
    {
        result = new CapturedCursor();
        var cursorInfo = CursorInfo.Create();

        if (!NativeCursorMethods.GetCursorInfo(ref cursorInfo)) return false;

        // Skip invisible cursors
        if (!cursorInfo.IsShowing) return false;

        var iconInfo = IconInfoEx.Create();

        if (!NativeIconMethods.GetIconInfoEx(cursorInfo.CursorHandle, ref iconInfo)) return false;

        try
        {
            // A. CALCULATE TARGET SIZE (High-DPI)
            int baseSize = GetCursorBaseSize();
            uint dpi = NativeDpiMethods.GetDpiForSystem();
            int targetWidth = (int)(baseSize * (dpi / 96.0f));
            int targetHeight = targetWidth;
            IntPtr hBestCursor = IntPtr.Zero;
            bool isFreshHandle = true;
            bool isCursorEnlarged = baseSize > 32;

            // Try to reload System Cursors to get High-DPI versions
            if (IsSystemCursor(iconInfo.ModuleName))
            {
                if (!string.IsNullOrEmpty(iconInfo.ModuleName))
                {
                    if (iconInfo.ResourceId != 0)
                    {
                        // Get the module
                        IntPtr hModule = Kernel32Api.GetModuleHandle(iconInfo.ModuleName);
                        // Load from DLL/EXE Resource
                        hBestCursor = NativeCursorMethods.LoadImage(hModule, (IntPtr)iconInfo.ResourceId, ImageType.IMAGE_CURSOR, targetWidth, targetHeight, LoadImageFlags.LR_DEFAULTCOLOR);
                    }
                    else
                    {
                        // Load from File (.cur/.ani)
                        hBestCursor = NativeCursorMethods.LoadImage(IntPtr.Zero, iconInfo.ModuleName, ImageType.IMAGE_CURSOR, targetWidth, targetHeight, LoadImageFlags.LR_LOADFROMFILE);
                    }
                }
            }

            // Fallback to initial handle
            if (hBestCursor == IntPtr.Zero)
            {
                hBestCursor = cursorInfo.CursorHandle;
                isFreshHandle = false;
            }

            // Determine native size (For 1:1 Capture)
            int nativeWidth = targetWidth;
            int nativeHeight = targetHeight;

            if (!isFreshHandle)
            {
                var bmpInfo = new GdiBitmap();
                var hMeasure = iconInfo.ColorBitmapHandle.IsInvalid ? iconInfo.BitmaskBitmapHandle : iconInfo.ColorBitmapHandle;
                if (Gdi32Api.GetObject(hMeasure, Marshal.SizeOf(typeof(GdiBitmap)), ref bmpInfo) > 0)
                {
                    nativeWidth = bmpInfo.Width;
                    nativeHeight = bmpInfo.Height;
                    // If hbmColor is NULL, hbmMask is double-height (AND + XOR) -> Actual cursor height is half.
                    if (iconInfo.ColorBitmapHandle.IsInvalid)
                    {
                        nativeHeight /= 2;
                    }
                }
            }

            bool isCustomCursor = !( nativeWidth == 32 && nativeHeight == 32);

            if (isCursorEnlarged && !isCustomCursor)
            {
                // Determine original hotspot relative to the handle's native size
                // (If nativeW is 32, but target is 48, we scale the hotspot)
                var maskInfo = new GdiBitmap();
                int handleWidth = 32;
                if (Gdi32Api.GetObject(iconInfo.BitmaskBitmapHandle, Marshal.SizeOf(typeof(GdiBitmap)), ref maskInfo) > 0)
                {
                    handleWidth = maskInfo.Width;
                }
                // Scale the target size and hotspot
                float scale = (float)targetWidth / (float)handleWidth;
                result.HotSpot = new NativePoint((int)(iconInfo.Hotspot.X * scale), (int)(iconInfo.Hotspot.Y * scale));
                result.Size = new Size(targetWidth, targetHeight);
            } else {
                // Don't scale, the app specified it's own target size and hotspot or user didn't specify different size
                result.HotSpot = iconInfo.Hotspot;
                result.Size = new Size(nativeWidth, nativeHeight);
            }

            bool isMonochrome = iconInfo.ColorBitmapHandle.IsInvalid;
            
            if (isMonochrome)
            {
                // Classic XOR (Paint.NET)
                result.ColorLayer = BitmapFromHIcon(hBestCursor, targetWidth, targetHeight, DrawIconExFlags.DI_IMAGE, PixelFormat.Format24bppRgb);
                result.MaskLayer = BitmapFromHIcon(hBestCursor, targetWidth, targetHeight, DrawIconExFlags.DI_MASK, PixelFormat.Format24bppRgb);
            }
            else
            {
                // Color Cursors (Modern Alpha, Paint.NET, Green Arrow)
                bool hasAlpha;

                // If the extracted bitmap doesn't match the target size, discard it and fallback
                if (isCustomCursor) {
                    // Directly dump the raw memory to preserve perfectly scaled Premultiplied Alpha pixels
                    result.ColorLayer = ExtractRawColorBitmap(iconInfo.ColorBitmapHandle, nativeWidth, nativeHeight, out hasAlpha);
                    result.HotSpot = iconInfo.Hotspot;
                    result.Size = new Size(nativeWidth, nativeHeight);
                }
                else {
                    result.ColorLayer = BitmapFromHIcon(hBestCursor, targetWidth, targetHeight, DrawIconExFlags.DI_NORMAL);
                    hasAlpha = true;
                }

                if (hasAlpha)
                {
                    // Modern Alpha Cursor (Mask is safely ignored)
                    result.MaskLayer = null;
                }
                else
                {
                    // Legacy Non-Alpha Color Cursor (Needs the mask to cut out the background)
                    result.MaskLayer = BitmapFromHIcon(hBestCursor, targetWidth, targetHeight, DrawIconExFlags.DI_MASK);
                }
            }

            if (isFreshHandle)
            {
                NativeCursorMethods.DestroyCursor(hBestCursor);
            }

            return true;

        }
        finally
        {
            // Always cleanup the GDI objects from GetIconInfoEx
            iconInfo.Dispose();
        }
    }

    /// <summary>
    /// Extracts a 32-bit color bitmap from a native handle and determines whether the bitmap contains an alpha channel.
    /// </summary>
    /// <remarks>The returned bitmap uses premultiplied alpha format to prevent color distortion. If the
    /// source bitmap does not contain an alpha channel, the method forces all pixels to be fully opaque.</remarks>
    /// <param name="hbmColor">A handle to the native color bitmap to extract. Must not be zero.</param>
    /// <param name="width">The width, in pixels, of the bitmap to extract. Must be greater than zero.</param>
    /// <param name="height">The height, in pixels, of the bitmap to extract. Must be greater than zero.</param>
    /// <param name="hasAlpha">When the method returns, contains a value indicating whether the extracted bitmap includes an alpha channel.</param>
    /// <returns>A 32-bit color bitmap representing the extracted image, or null if extraction fails or the parameters are
    /// invalid.</returns>
    private static Bitmap ExtractRawColorBitmap(SafeHBitmapHandle hbmColor, int width, int height, out bool hasAlpha)
    {
        hasAlpha = false;
        if (hbmColor.IsInvalid || width <= 0 || height <= 0) return null;

        // Using PArgb (Premultiplied) prevents .NET from artificially darkening white edges
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

        BitmapInfoHeader bitmapInfoHeader = BitmapInfoHeader.Create(width, -height, 32);
        bitmapInfoHeader.SizeImage = 0;

        IntPtr hdc = User32Api.GetDC(IntPtr.Zero);
        int result = Gdi32Api.GetDIBits(hdc, hbmColor, 0, (uint)height, data.Scan0, ref bitmapInfoHeader, 0);
        User32Api.ReleaseDC(IntPtr.Zero, hdc);

        if (result == 0)
        {
            bmp.UnlockBits(data);
            bmp.Dispose();
            return null;
        }

        unsafe
        {
            byte* ptr = (byte*)data.Scan0;
            int bytes = width * height * 4;

            // Scan to see if an alpha channel actually exists
            for (int i = 3; i < bytes; i += 4)
            {
                if (ptr[i] != 0)
                {
                    hasAlpha = true;
                    break;
                }
            }

            // If it's a legacy cursor with no alpha channel, force it to opaque
            if (!hasAlpha)
            {
                for (int i = 3; i < bytes; i += 4)
                {
                    ptr[i] = 255;
                }
            }
        }

        bmp.UnlockBits(data);
        return bmp;
    }

    /// <summary>
    /// Creates a bitmap image from the specified Windows icon handle at the given size.
    /// </summary>
    /// <remarks>The resulting bitmap is initialized with a transparent background before the icon is drawn.
    /// Ensure that the provided icon handle is valid and that the size parameter is appropriate for the intended
    /// use.</remarks>
    /// <param name="hIcon">A handle to the icon to convert. This parameter must not be zero or invalid.</param>
    /// <param name="width">The width, in pixels, of the resulting bitmap. Must be a positive integer.</param>
    /// <param name="height">The height, in pixels, of the resulting bitmap. Must be a positive integer.</param>
    /// <param name="flags">int with 0x0003 = DI_NORMAL (Draw Image + Draw Mask), 0x0002 = DI_IMAGE (Draw Image Only), 0x0001 = DI_MASK (Draw Mask Only)</param>
    /// <param name="pixelFormat">PixelFormat</param>
    /// <returns>A Bitmap object that represents the icon specified by the hIcon parameter, rendered at the specified size.</returns>
    public static Bitmap BitmapFromHIcon(IntPtr hIcon, int width, int height, DrawIconExFlags flags = DrawIconExFlags.DI_NORMAL, PixelFormat pixelFormat = PixelFormat.Undefined)
    {
        PixelFormat format;
        if (pixelFormat != PixelFormat.Undefined) {
            format = pixelFormat;
        } else {
            format = (flags == DrawIconExFlags.DI_MASK || flags == DrawIconExFlags.DI_IMAGE) ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
        }
        Bitmap bmp = new Bitmap(width, height, format);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            // Check what background color we need based on the type of icon and pixel format:
            if (format == PixelFormat.Format24bppRgb)
            {
                if (flags == DrawIconExFlags.DI_MASK)
                {
                    // For DI_MASK: We want the "Background" to be Transparent (White in GDI mask logic).
                    // So SRCAND leaves the screenshot alone in empty areas.
                    // If we used Black (Transparent), the whole box would become Opaque.
                    g.Clear(Color.White);
                }
                else
                {
                    // For DI_IMAGE: We want the "Background" to be Neutral (Black in XOR logic).
                    // So SRCINVERT doesn't change the screenshot in empty areas.
                    g.Clear(Color.Black);
                }
            }
            else
            {
                // For 32-bit standard transparent is fine.
                g.Clear(Color.Transparent);
            }

            NativeIconMethods.DrawIconEx(g.GetHdc(), 0, 0, hIcon, width, height, 0, IntPtr.Zero, flags);
            g.ReleaseHdc();
        }
        return bmp;
    }

    /// <summary>
    /// Determines whether the specified module name corresponds to a known system cursor provider.
    /// </summary>
    /// <remarks>This method checks for common Windows system cursor sources, such as 'user32.dll', the
    /// Windows cursors directory, and 'main.cpl' (mouse settings).</remarks>
    /// <param name="moduleName">The name of the module to check. This parameter cannot be null or empty.</param>
    /// <returns>true if the module name is associated with a standard Windows system cursor provider; otherwise, false.</returns>
    private static bool IsSystemCursor(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return false;
        }

        string lower = moduleName.ToLowerInvariant();

        // Standard Windows cursors live here
        if (lower.Contains("user32"))
        {
            return true;
        }

        if (lower.Contains(@"\windows\cursors\"))
        {
            return true;
        }

        // Sometimes they come from main.cpl (mouse settings)
        return lower.Contains("main.cpl");
    }

    /// <summary>
    /// Draws the specified cursor image onto the provided graphics context at the given position, applying appropriate
    /// blending techniques based on the cursor type.
    /// </summary>
    /// <remarks>This method supports both modern system cursors and legacy XOR/mask cursors, utilizing
    /// different drawing strategies based on the cursor's properties. It handles transparency and blending
    /// appropriately for each case.</remarks>
    /// <param name="targetGraphics">The graphics context where the cursor will be drawn. This must not be null.</param>
    /// <param name="cursor">The cursor to be drawn, represented as a CapturedCursor containing the color and mask layers. This must not be
    /// null, and the ColorLayer must be available.</param>
    /// <param name="position">The position on the graphics context where the cursor will be drawn, specified as a NativePoint. The cursor will
    /// be offset by its hot spot.</param>
    /// <param name="destinationSize">NativeSize</param>
    public static void DrawCursorOnGraphics(Graphics targetGraphics, CapturedCursor cursor, NativePoint position, NativeSize destinationSize = default)
    {
        if (cursor == null || cursor.ColorLayer == null) return;

        // Calculate target position
        int x = position.X;// - cursor.HotSpot.X;
        int y = position.Y;// - cursor.HotSpot.Y;

        int sourceWidth = cursor.Size.Width;
        int sourceHeight = cursor.Size.Height;
        if (destinationSize.IsEmpty)
        {
            destinationSize = new NativeSize(sourceWidth, sourceHeight);
        }

        // If it's a modern cursor, standard GDI+ drawing is sufficient and supports transparency best.
        if (cursor.MaskLayer == null)
        {
            var state = targetGraphics.Save();
            targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
            targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
            targetGraphics.PixelOffsetMode = PixelOffsetMode.Half;
            using (ImageAttributes wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                Rectangle destRect = new Rectangle(x, y, destinationSize.Width, destinationSize.Height);
                targetGraphics.DrawImage(cursor.ColorLayer,destRect, 0, 0, sourceWidth, sourceWidth, GraphicsUnit.Pixel, wrapMode);
            }
            targetGraphics.Restore(state);
            return;
        }

        Point[] pts = { new Point(position.X, position.Y) };
        targetGraphics.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, pts);
        position = new NativePoint(pts[0].X,  pts[0].Y);

        // We need BitBlt to perform bitwise operations (AND / XOR).

        // Get the handle to the destination device context (The screenshot)
        using var hdcDest = SafeGraphicsDcHandle.FromGraphics(targetGraphics);

        // Create a memory DC to hold our source bitmaps temporarily
        using var hdcSrc = Gdi32Api.CreateCompatibleDC(hdcDest);

        // Convert GDI+ Bitmaps to GDI Handles (HBITMAP)
        // We need raw handles for BitBlt/StretchBlt. 
        // Note: GetHbitmap() creates a copy, so we must delete it after.
        using var hbmMask = new SafeHBitmapHandle(cursor.MaskLayer.GetHbitmap());

        // Apply mask, by selecting the Mask into the source DC
        var hbmOld = Gdi32Api.SelectObject(hdcSrc, hbmMask);
        if (hbmOld.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        // Operation: SRCAND (0x008800C6)
        // Logic: Dest = Dest AND Source
        // Result: 
        // - Where Mask is White (1), Dest stays Dest. (Transparent area)
        // - Where Mask is Black (0), Dest becomes Black. (Cutout for cursor)
        if (!Gdi32Api.StretchBlt(hdcDest, x, y, destinationSize.Width, destinationSize.Height, hdcSrc, 0, 0, sourceWidth, sourceHeight, RasterOperations.SourceAnd))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using var hbmColor = new SafeHBitmapHandle(cursor.ColorLayer.GetHbitmap());
        // Apply image by select the image into the source DC
        Gdi32Api.SelectObject(hdcSrc, hbmColor);

        // Operation: SRCINVERT (0x00660046) -> This is XOR
        // Logic: Dest = Dest XOR Source
        // Result:
        // - In the "Cutout" (Black): 0 XOR Color = Color. (Normal drawing)
        // - In the "Transparent" (Background): Dest XOR White = Inverted Dest. (XOR effect)
        if (!Gdi32Api.StretchBlt(hdcDest, x, y, destinationSize.Width, destinationSize.Height, hdcSrc, 0, 0, sourceWidth, sourceHeight, RasterOperations.SourceInvert))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Restore cleanup
        Gdi32Api.SelectObject(hdcSrc, hbmOld);
    }

    /// <summary>
    /// Draws a captured cursor onto a bitmap at the specified position using pixel-level bitmap operations.
    /// This method is more reliable than DrawCursorOnGraphics when working directly with Bitmap objects.
    /// </summary>
    /// <remarks>
    /// This method uses typed Span-based pixel access (Bgra32/Bgr24) for efficient and readable bitmap manipulation.
    /// It supports both modern alpha-blended cursors and legacy XOR/mask cursors. For legacy cursors, it properly 
    /// applies the AND mask followed by the XOR operation to achieve the correct visual effect.
    /// 
    /// <example>
    /// Basic usage:
    /// <code>
    /// // Capture the current cursor
    /// if (CursorHelper.TryGetCurrentCursor(out var cursor))
    /// {
    ///     // Create or use an existing bitmap
    ///     var bitmap = new Bitmap(800, 600, PixelFormat.Format32bppArgb);
    ///     
    ///     // Draw the cursor at position (100, 100)
    ///     CursorHelper.DrawCursorOnBitmap(bitmap, cursor, new NativePoint(100, 100));
    ///     
    ///     // Optionally scale the cursor
    ///     CursorHelper.DrawCursorOnBitmap(bitmap, cursor, new NativePoint(200, 200), new NativeSize(64, 64));
    ///     
    ///     cursor.Dispose();
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="targetBitmap">The bitmap to draw the cursor onto.</param>
    /// <param name="cursor">The captured cursor data to draw.</param>
    /// <param name="position">The position at which to draw the cursor (typically mouse coordinates).</param>
    /// <param name="destinationSize">Optional size to scale the cursor. If empty, uses the cursor's natural size.</param>
    /// <exception cref="ArgumentNullException">Thrown when targetBitmap or cursor is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the target bitmap's pixel format is not supported.</exception>
    public static void DrawCursorOnBitmap(Bitmap targetBitmap, CapturedCursor cursor, NativePoint position, NativeSize destinationSize = default)
    {
        if (targetBitmap == null)
        {
            throw new ArgumentNullException(nameof(targetBitmap));
        }
        if (cursor == null || cursor.ColorLayer == null)
        {
            return;
        }

        // Calculate target position
        int x = position.X;
        int y = position.Y;

        int sourceWidth = cursor.Size.Width;
        int sourceHeight = cursor.Size.Height;
        if (destinationSize.IsEmpty)
        {
            destinationSize = new NativeSize(sourceWidth, sourceHeight);
        }

        // Determine if we need to scale
        bool needsScaling = destinationSize.Width != sourceWidth || destinationSize.Height != sourceHeight;

        // For modern cursors (no mask), use standard alpha blending
        if (cursor.MaskLayer == null)
        {
            // Scale if needed
            Bitmap cursorToUse = cursor.ColorLayer;
            if (needsScaling)
            {
                cursorToUse = new Bitmap(cursor.ColorLayer, destinationSize.Width, destinationSize.Height);
            }

            try
            {
                DrawAlphaCursorOnBitmap(targetBitmap, cursorToUse, x, y);
            }
            finally
            {
                if (needsScaling && cursorToUse != cursor.ColorLayer)
                {
                    cursorToUse.Dispose();
                }
            }
            return;
        }

        // For legacy cursors with mask, apply AND/XOR operations
        Bitmap scaledColor = cursor.ColorLayer;
        Bitmap scaledMask = cursor.MaskLayer;

        if (needsScaling)
        {
            scaledColor = new Bitmap(cursor.ColorLayer, destinationSize.Width, destinationSize.Height);
            scaledMask = new Bitmap(cursor.MaskLayer, destinationSize.Width, destinationSize.Height);
        }

        try
        {
            DrawMaskedCursorOnBitmap(targetBitmap, scaledColor, scaledMask, x, y);
        }
        finally
        {
            if (needsScaling)
            {
                if (scaledColor != cursor.ColorLayer)
                {
                    scaledColor.Dispose();
                }
                if (scaledMask != cursor.MaskLayer)
                {
                    scaledMask.Dispose();
                }
            }
        }
    }
    private static bool Is32BitFormat(PixelFormat format) =>
        format == PixelFormat.Format32bppArgb ||
        format == PixelFormat.Format32bppRgb ||
        format == PixelFormat.Format32bppPArgb;

    /// <summary>
    /// Draws a modern alpha-blended cursor onto a bitmap.
    /// </summary>
    private static void DrawAlphaCursorOnBitmap(Bitmap targetBitmap, Bitmap cursorBitmap, int x, int y)
    {
        Bitmap convertedBitmap = null;
        try
        {
            // Ensure the cursor bitmap has alpha channel
            if (cursorBitmap.PixelFormat != PixelFormat.Format32bppArgb &&
                cursorBitmap.PixelFormat != PixelFormat.Format32bppPArgb)
            {
                convertedBitmap = new Bitmap(cursorBitmap.Width, cursorBitmap.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(convertedBitmap))
                {
                    g.DrawImage(cursorBitmap, 0, 0, cursorBitmap.Width, cursorBitmap.Height);
                }
                cursorBitmap = convertedBitmap;
            }

            // Dispatch based on target format
            if (Is32BitFormat(targetBitmap.PixelFormat))
            {
                DrawAlphaCursor<Bgra32>(targetBitmap, cursorBitmap, x, y);
            }
            else if (targetBitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                DrawAlphaCursor<Bgr24>(targetBitmap, cursorBitmap, x, y);
            }
            else
            {
                throw new NotSupportedException($"Target bitmap format {targetBitmap.PixelFormat} is not supported.");
            }
        }
        finally
        {
            convertedBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Generic implementation for alpha-blended cursor drawing.
    /// </summary>
    private static void DrawAlphaCursor<TTarget>(Bitmap targetBitmap, Bitmap cursorBitmap, int x, int y)
        where TTarget : struct
    {
        using var targetAccessor = new BitmapAccessor<TTarget>(targetBitmap, readOnly: false);
        using var cursorAccessor = new BitmapAccessor<Bgra32>(cursorBitmap, readOnly: true);

        for (int cy = 0; cy < cursorBitmap.Height; cy++)
        {
            int ty = y + cy;
            if (ty < 0 || ty >= targetAccessor.Height) continue;

            var targetRow = targetAccessor.GetRowSpan(ty);
            var cursorRow = cursorAccessor.GetRowSpan(cy);

            for (int cx = 0; cx < cursorBitmap.Width; cx++)
            {
                int tx = x + cx;
                if (tx < 0 || tx >= targetAccessor.Width) continue;

                ref readonly var cursorPixel = ref cursorRow[cx];
                if (cursorPixel.A == 0) continue;

                if (typeof(TTarget) == typeof(Bgra32))
                {
                    ref var targetPixel = ref Unsafe.As<TTarget, Bgra32>(ref targetRow[tx]);
                    Bgra32.AlphaBlend(ref targetPixel, in cursorPixel);
                }
                else // Bgr24
                {
                    ref var targetPixel = ref Unsafe.As<TTarget, Bgr24>(ref targetRow[tx]);
                    Bgr24.AlphaBlend(ref targetPixel, in cursorPixel);
                }
            }
        }
    }

    /// <summary>
    /// Draws a legacy cursor with mask using AND/XOR operations.
    /// </summary>
    private static void DrawMaskedCursorOnBitmap(Bitmap targetBitmap, Bitmap colorBitmap, Bitmap maskBitmap, int x, int y)
    {
        if (Is32BitFormat(targetBitmap.PixelFormat))
        {
            DrawMaskedCursor<Bgra32>(targetBitmap, colorBitmap, maskBitmap, x, y);
        }
        else if (targetBitmap.PixelFormat == PixelFormat.Format24bppRgb)
        {
            DrawMaskedCursor<Bgr24>(targetBitmap, colorBitmap, maskBitmap, x, y);
        }
        else
        {
            throw new NotSupportedException($"Target bitmap format {targetBitmap.PixelFormat} is not supported.");
        }
    }

    /// <summary>
    /// Generic implementation for masked cursor drawing with AND/XOR operations.
    /// </summary>
    private static void DrawMaskedCursor<TTarget>(Bitmap targetBitmap, Bitmap colorBitmap, Bitmap maskBitmap, int x, int y)
        where TTarget : struct
    {
        using var targetAccessor = new BitmapAccessor<TTarget>(targetBitmap, readOnly: false);

        // Handle both 32-bit and 24-bit color/mask bitmaps
        if (Is32BitFormat(colorBitmap.PixelFormat))
        {
            using var colorAccessor = new BitmapAccessor<Bgra32>(colorBitmap, readOnly: true);
            using var maskAccessor = new BitmapAccessor<Bgra32>(maskBitmap, readOnly: true);
            ApplyMask<TTarget, Bgra32>(targetAccessor, colorAccessor, maskAccessor, x, y);
        }
        else
        {
            using var colorAccessor = new BitmapAccessor<Bgr24>(colorBitmap, readOnly: true);
            using var maskAccessor = new BitmapAccessor<Bgr24>(maskBitmap, readOnly: true);
            ApplyMask<TTarget, Bgr24>(targetAccessor, colorAccessor, maskAccessor, x, y);
        }
    }

    /// <summary>
    /// Applies mask using AND/XOR operations.
    /// </summary>
    private static void ApplyMask<TTarget, TSource>(
        BitmapAccessor<TTarget> targetAccessor,
        BitmapAccessor<TSource> colorAccessor,
        BitmapAccessor<TSource> maskAccessor,
        int x, int y)
        where TTarget : struct
        where TSource : struct
    {
        for (int cy = 0; cy < colorAccessor.Height; cy++)
        {
            int ty = y + cy;
            if (ty < 0 || ty >= targetAccessor.Height) continue;

            var targetRow = targetAccessor.GetRowSpan(ty);
            var colorRow = colorAccessor.GetRowSpan(cy);
            var maskRow = maskAccessor.GetRowSpan(cy);

            for (int cx = 0; cx < colorAccessor.Width; cx++)
            {
                int tx = x + cx;
                if (tx < 0 || tx >= targetAccessor.Width) continue;

                // Extract RGB components from color and mask (use B channel for mask value)
                byte maskValue, colorR, colorG, colorB;
                
                if (typeof(TSource) == typeof(Bgra32))
                {
                    ref readonly var mask = ref Unsafe.As<TSource, Bgra32>(ref maskRow[cx]);
                    ref readonly var color = ref Unsafe.As<TSource, Bgra32>(ref colorRow[cx]);
                    maskValue = mask.B;
                    colorR = color.R;
                    colorG = color.G;
                    colorB = color.B;
                }
                else // Bgr24
                {
                    ref readonly var mask = ref Unsafe.As<TSource, Bgr24>(ref maskRow[cx]);
                    ref readonly var color = ref Unsafe.As<TSource, Bgr24>(ref colorRow[cx]);
                    maskValue = mask.B;
                    colorR = color.R;
                    colorG = color.G;
                    colorB = color.B;
                }

                // Apply AND/XOR operations to target
                if (typeof(TTarget) == typeof(Bgra32))
                {
                    ref var target = ref Unsafe.As<TTarget, Bgra32>(ref targetRow[tx]);
                    target.B = (byte)((target.B & maskValue) ^ colorB);
                    target.G = (byte)((target.G & maskValue) ^ colorG);
                    target.R = (byte)((target.R & maskValue) ^ colorR);
                    target.A = 255;
                }
                else // Bgr24
                {
                    ref var target = ref Unsafe.As<TTarget, Bgr24>(ref targetRow[tx]);
                    target.B = (byte)((target.B & maskValue) ^ colorB);
                    target.G = (byte)((target.G & maskValue) ^ colorG);
                    target.R = (byte)((target.R & maskValue) ^ colorR);
                }
            }
        }
    }

}
#endif
