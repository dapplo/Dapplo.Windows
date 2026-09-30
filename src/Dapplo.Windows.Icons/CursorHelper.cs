// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        // GetIconInfoEx created two bitmaps which we own, take the ownership exactly once so each is deleted exactly once
        iconInfo.TakeBitmaps(out var originalBitmaskBitmap, out var originalColorBitmap);
        var bitmaskBitmap = originalBitmaskBitmap;
        var colorBitmap = originalColorBitmap;
        IntPtr hFreshCursor = IntPtr.Zero;
        SafeHBitmapHandle freshBitmaskBitmap = null;
        SafeHBitmapHandle freshColorBitmap = null;
        var capturedCursor = new CapturedCursor();
        bool success = false;
        try
        {
            // The size of the bitmaps of the cursor handle which is currently used
            var nativeSize = GetCursorBitmapSize(bitmaskBitmap, colorBitmap);
            if (nativeSize.IsEmpty)
            {
                return false;
            }
            var hotSpot = iconInfo.Hotspot;
            var hCursor = cursorInfo.CursorHandle;

            // A. CALCULATE TARGET SIZE (High-DPI, "Make mouse pointer bigger")
            int baseSize = GetCursorBaseSize();
            int targetSize = (int)Math.Round(baseSize * (NativeDpiMethods.GetDpiForSystem() / 96.0));

            // B. Try to reload System Cursors at the target size, to get the High-DPI versions.
            // All values (size, hotspot, bitmaps) are taken from the handle which is actually rendered.
            if (targetSize > 0 && nativeSize.Width != targetSize && IsSystemCursor(iconInfo.ModuleName))
            {
                hFreshCursor = LoadCursor(iconInfo, targetSize);
                if (hFreshCursor != IntPtr.Zero)
                {
                    var freshIconInfo = IconInfoEx.Create();
                    if (NativeIconMethods.GetIconInfoEx(hFreshCursor, ref freshIconInfo))
                    {
                        freshIconInfo.TakeBitmaps(out freshBitmaskBitmap, out freshColorBitmap);
                        var freshSize = GetCursorBitmapSize(freshBitmaskBitmap, freshColorBitmap);
                        if (!freshSize.IsEmpty)
                        {
                            hCursor = hFreshCursor;
                            hotSpot = freshIconInfo.Hotspot;
                            nativeSize = freshSize;
                            bitmaskBitmap = freshBitmaskBitmap;
                            colorBitmap = freshColorBitmap;
                        }
                    }
                }
            }

            // C. The size the cursor is rendered with, this is the native size of the handle,
            // except for a not reloaded default sized (32x32) cursor while the user enlarged the mouse pointer.
            var renderSize = nativeSize;
            if (hCursor == cursorInfo.CursorHandle && baseSize > 32 && nativeSize.Width == 32 && nativeSize.Height == 32 && targetSize > 32)
            {
                renderSize = new NativeSize(targetSize, targetSize);
            }
            capturedCursor.Size = renderSize;
            capturedCursor.HotSpot = ScaleHotSpot(hotSpot, nativeSize, renderSize);

            // D. Modern cursors with an alpha channel: take the pixels directly, these are straight (not premultiplied) alpha
            if (!colorBitmap.IsInvalid)
            {
                var rawColorBitmap = ExtractRawColorBitmap(colorBitmap, nativeSize.Width, nativeSize.Height, out var hasAlpha);
                if (rawColorBitmap != null && hasAlpha)
                {
                    capturedCursor.MaskLayer = null;
                    if (renderSize == nativeSize)
                    {
                        capturedCursor.ColorLayer = rawColorBitmap;
                    }
                    else
                    {
                        // Don't scale the pixels up, that is blurry: let DrawIconEx render the cursor at the render size,
                        // for a shared (system) cursor Windows then uses the best matching image of the cursor resource.
                        rawColorBitmap.Dispose();
                        capturedCursor.ColorLayer = BitmapFromHIcon(hCursor, renderSize.Width, renderSize.Height, DrawIconExFlags.DI_NORMAL, PixelFormat.Format32bppArgb);
                    }
                    success = true;
                    result = capturedCursor;
                    return true;
                }
                rawColorBitmap?.Dispose();
            }

            // E. Monochrome cursors and color cursors without alpha channel need the AND mask and XOR image,
            // these are rendered with DrawIconEx, which also takes care of the scaling.
            capturedCursor.ColorLayer = BitmapFromHIcon(hCursor, renderSize.Width, renderSize.Height, DrawIconExFlags.DI_IMAGE, PixelFormat.Format24bppRgb);
            capturedCursor.MaskLayer = BitmapFromHIcon(hCursor, renderSize.Width, renderSize.Height, DrawIconExFlags.DI_MASK, PixelFormat.Format24bppRgb);
            success = true;
            result = capturedCursor;
            return true;
        }
        finally
        {
            if (!success)
            {
                capturedCursor.Dispose();
            }
            if (hFreshCursor != IntPtr.Zero)
            {
                NativeCursorMethods.DestroyCursor(hFreshCursor);
            }
            // Always cleanup the GDI objects from GetIconInfoEx
            freshColorBitmap?.Dispose();
            freshBitmaskBitmap?.Dispose();
            originalColorBitmap.Dispose();
            originalBitmaskBitmap.Dispose();
        }
    }

    /// <summary>
    /// Load the cursor from the module or file specified in the IconInfoEx, at the specified size
    /// </summary>
    /// <param name="iconInfo">IconInfoEx</param>
    /// <param name="size">int with the size in pixels</param>
    /// <returns>IntPtr with the cursor handle, which must be destroyed, or IntPtr.Zero</returns>
    private static IntPtr LoadCursor(IconInfoEx iconInfo, int size)
    {
        if (string.IsNullOrEmpty(iconInfo.ModuleName))
        {
            return IntPtr.Zero;
        }
        if (iconInfo.ResourceId != 0)
        {
            // Load from DLL/EXE Resource
            IntPtr hModule = Kernel32Api.GetModuleHandle(iconInfo.ModuleName);
            if (hModule == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            return NativeCursorMethods.LoadImage(hModule, (IntPtr)iconInfo.ResourceId, ImageType.IMAGE_CURSOR, size, size, LoadImageFlags.LR_DEFAULTCOLOR);
        }
        // Load from File (.cur/.ani)
        return NativeCursorMethods.LoadImage(IntPtr.Zero, iconInfo.ModuleName, ImageType.IMAGE_CURSOR, size, size, LoadImageFlags.LR_LOADFROMFILE);
    }

    /// <summary>
    /// Get the size of the cursor from its bitmaps, for a monochrome cursor (no color bitmap) the mask contains the AND and XOR mask so the height is halved.
    /// </summary>
    /// <param name="bitmaskBitmap">SafeHBitmapHandle for the mask</param>
    /// <param name="colorBitmap">SafeHBitmapHandle for the color bitmap, can be invalid</param>
    /// <returns>NativeSize, empty if the size can't be determined</returns>
    private static NativeSize GetCursorBitmapSize(SafeHBitmapHandle bitmaskBitmap, SafeHBitmapHandle colorBitmap)
    {
        bool isMonochrome = colorBitmap == null || colorBitmap.IsInvalid;
        var hMeasure = isMonochrome ? bitmaskBitmap : colorBitmap;
        if (hMeasure == null || hMeasure.IsInvalid)
        {
            return NativeSize.Empty;
        }
        var bitmapInfo = new GdiBitmap();
        if (Gdi32Api.GetObject(hMeasure, Marshal.SizeOf(typeof(GdiBitmap)), ref bitmapInfo) <= 0)
        {
            return NativeSize.Empty;
        }
        var height = Math.Abs(bitmapInfo.Height);
        return new NativeSize(bitmapInfo.Width, isMonochrome ? height / 2 : height);
    }

    /// <summary>
    /// Scale the hotspot from the native size of the cursor to the size it is rendered with
    /// </summary>
    /// <param name="hotSpot">NativePoint</param>
    /// <param name="nativeSize">NativeSize</param>
    /// <param name="renderSize">NativeSize</param>
    /// <returns>NativePoint</returns>
    private static NativePoint ScaleHotSpot(NativePoint hotSpot, NativeSize nativeSize, NativeSize renderSize)
    {
        if (nativeSize == renderSize || nativeSize.IsEmpty)
        {
            return hotSpot;
        }
        return new NativePoint(hotSpot.X * renderSize.Width / nativeSize.Width, hotSpot.Y * renderSize.Height / nativeSize.Height);
    }

    /// <summary>
    /// Create a scaled copy of the bitmap
    /// </summary>
    /// <param name="source">Bitmap</param>
    /// <param name="size">NativeSize for the result</param>
    /// <param name="pixelFormat">PixelFormat for the result</param>
    /// <param name="interpolationMode">InterpolationMode, use NearestNeighbor for AND/XOR masks</param>
    /// <returns>Bitmap</returns>
    private static Bitmap ScaleBitmap(Bitmap source, NativeSize size, PixelFormat pixelFormat, InterpolationMode interpolationMode)
    {
        var result = new Bitmap(size.Width, size.Height, pixelFormat);
        try
        {
            using var graphics = Graphics.FromImage(result);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = interpolationMode;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            using var imageAttributes = new ImageAttributes();
            imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(source, new Rectangle(0, 0, size.Width, size.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Extracts a 32-bit color bitmap from a native handle and determines whether the bitmap contains an alpha channel.
    /// </summary>
    /// <remarks>The color bitmap of an icon or cursor contains straight (not premultiplied) alpha, so the returned bitmap is Format32bppArgb.
    /// If the source bitmap does not contain an alpha channel, the method forces all pixels to be fully opaque.</remarks>
    /// <param name="hbmColor">A handle to the native color bitmap to extract. Must not be zero.</param>
    /// <param name="width">The width, in pixels, of the bitmap to extract. Must be greater than zero.</param>
    /// <param name="height">The height, in pixels, of the bitmap to extract. Must be greater than zero.</param>
    /// <param name="hasAlpha">When the method returns, contains a value indicating whether the extracted bitmap includes an alpha channel.</param>
    /// <returns>A 32-bit color bitmap representing the extracted image, or null if extraction fails or the parameters are
    /// invalid.</returns>
    private static Bitmap ExtractRawColorBitmap(SafeHBitmapHandle hbmColor, int width, int height, out bool hasAlpha)
    {
        hasAlpha = false;
        if (hbmColor == null || hbmColor.IsInvalid || width <= 0 || height <= 0) return null;

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        bool success = false;
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            // Top-down 32bpp DIB, the rows are width * 4 bytes which is exactly the stride of the locked 32bpp bitmap
            BitmapInfoHeader bitmapInfoHeader = BitmapInfoHeader.Create(width, -height, 32);
            bitmapInfoHeader.SizeImage = 0;

            IntPtr hdc = User32Api.GetDC(IntPtr.Zero);
            int result;
            try
            {
                result = Gdi32Api.GetDIBits(hdc, hbmColor, 0, (uint)height, data.Scan0, ref bitmapInfoHeader, 0);
            }
            finally
            {
                User32Api.ReleaseDC(IntPtr.Zero, hdc);
            }

            if (result == 0)
            {
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
            success = true;
        }
        finally
        {
            bmp.UnlockBits(data);
            if (!success)
            {
                bmp.Dispose();
            }
        }
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
    /// <remarks>This method supports both modern alpha cursors and legacy XOR/mask cursors, utilizing
    /// different drawing strategies based on the cursor's properties. Modern cursors are drawn with GDI+ and respect the full transformation of the Graphics,
    /// legacy cursors are drawn with StretchBlt (AND/XOR) at the position and size transformed to device coordinates, rotations and shears are not supported for these.</remarks>
    /// <param name="targetGraphics">The graphics context where the cursor will be drawn. This must not be null.</param>
    /// <param name="cursor">The cursor to be drawn, represented as a CapturedCursor containing the color and mask layers.</param>
    /// <param name="position">The position (in world coordinates of the graphics) of the top-left corner of the cursor image. This is NOT offset by the hot spot,
    /// to draw the cursor at the mouse location pass the mouse location minus <see cref="CapturedCursor.HotSpot"/> (scaled with destinationSize when scaling).</param>
    /// <param name="destinationSize">NativeSize, when empty the cursor.Size is used</param>
    public static void DrawCursorOnGraphics(Graphics targetGraphics, CapturedCursor cursor, NativePoint position, NativeSize destinationSize = default)
    {
        if (targetGraphics == null)
        {
            throw new ArgumentNullException(nameof(targetGraphics));
        }
        if (cursor == null || cursor.ColorLayer == null) return;

        int sourceWidth = cursor.ColorLayer.Width;
        int sourceHeight = cursor.ColorLayer.Height;
        if (destinationSize.IsEmpty)
        {
            destinationSize = new NativeSize(sourceWidth, sourceHeight);
        }

        // If it's a modern cursor, standard GDI+ drawing is sufficient and supports transparency best.
        if (cursor.MaskLayer == null)
        {
            var state = targetGraphics.Save();
            try
            {
                targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
                targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
                targetGraphics.CompositingMode = CompositingMode.SourceOver;
                targetGraphics.PixelOffsetMode = PixelOffsetMode.Half;
                using ImageAttributes wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                var destRect = new Rectangle(position.X, position.Y, destinationSize.Width, destinationSize.Height);
                targetGraphics.DrawImage(cursor.ColorLayer, destRect, 0, 0, sourceWidth, sourceHeight, GraphicsUnit.Pixel, wrapMode);
            }
            finally
            {
                targetGraphics.Restore(state);
            }
            return;
        }

        // We need StretchBlt to perform bitwise operations (AND / XOR), this works on the HDC which uses device coordinates.
        // Transform the destination rectangle, a mirroring results in a negative width or height which StretchBlt supports.
        Point[] pts = { new Point(position.X, position.Y), new Point(position.X + destinationSize.Width, position.Y + destinationSize.Height) };
        targetGraphics.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, pts);
        int x = pts[0].X;
        int y = pts[0].Y;
        int width = pts[1].X - pts[0].X;
        int height = pts[1].Y - pts[0].Y;
        if (width == 0 || height == 0)
        {
            return;
        }

        // Convert GDI+ Bitmaps to GDI Handles (HBITMAP), GetHbitmap() creates a copy which is deleted by the SafeHBitmapHandle.
        using var hbmMask = new SafeHBitmapHandle(cursor.MaskLayer.GetHbitmap());
        using var hbmColor = new SafeHBitmapHandle(cursor.ColorLayer.GetHbitmap());

        // Get the handle to the destination device context (The screenshot)
        using var hdcDest = SafeGraphicsDcHandle.FromGraphics(targetGraphics);

        // Create a memory DC to hold our source bitmaps temporarily
        using var hdcSrc = Gdi32Api.CreateCompatibleDC(hdcDest);

        // Apply mask, by selecting the Mask into the source DC, the previous bitmap is selected back when the SafeSelectObjectHandle is disposed.
        using (var selectedMask = hdcSrc.SelectObject(hbmMask))
        {
            if (selectedMask.IsInvalid)
            {
                throw new InvalidOperationException("Couldn't select the cursor mask into the device context.");
            }
            // Operation: SRCAND (0x008800C6)
            // Logic: Dest = Dest AND Source
            // Result:
            // - Where Mask is White (1), Dest stays Dest. (Transparent area)
            // - Where Mask is Black (0), Dest becomes Black. (Cutout for cursor)
            if (!Gdi32Api.StretchBlt(hdcDest, x, y, width, height, hdcSrc, 0, 0, sourceWidth, sourceHeight, RasterOperations.SourceAnd))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        // Apply image by selecting the image into the source DC
        using (var selectedColor = hdcSrc.SelectObject(hbmColor))
        {
            if (selectedColor.IsInvalid)
            {
                throw new InvalidOperationException("Couldn't select the cursor image into the device context.");
            }
            // Operation: SRCINVERT (0x00660046) -> This is XOR
            // Logic: Dest = Dest XOR Source
            // Result:
            // - In the "Cutout" (Black): 0 XOR Color = Color. (Normal drawing)
            // - In the "Transparent" (Background): Dest XOR White = Inverted Dest. (XOR effect)
            if (!Gdi32Api.StretchBlt(hdcDest, x, y, width, height, hdcSrc, 0, 0, sourceWidth, sourceHeight, RasterOperations.SourceInvert))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    /// <summary>
    /// Draws a captured cursor onto a bitmap at the specified position using pixel-level bitmap operations.
    /// This method is more reliable than DrawCursorOnGraphics when working directly with Bitmap objects.
    /// </summary>
    /// <remarks>
    /// This method uses typed Span-based pixel access (Bgra32/Bgr24) for efficient and readable bitmap manipulation.
    /// It supports both modern alpha-blended cursors and legacy XOR/mask cursors. For legacy cursors, it properly 
    /// applies the AND mask followed by the XOR operation to achieve the correct visual effect.
    /// Alpha cursors are blended with the Porter-Duff "over" operator, taking the (premultiplied) alpha of the target into account.
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
    ///     // Draw the cursor with its hot spot at the mouse position (100, 100)
    ///     CursorHelper.DrawCursorOnBitmap(bitmap, cursor, new NativePoint(100 - cursor.HotSpot.X, 100 - cursor.HotSpot.Y));
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
    /// <param name="position">The position of the top-left corner of the cursor image, this is NOT offset by the hot spot.</param>
    /// <param name="destinationSize">Optional size to scale the cursor. If empty, uses the cursor's natural size.</param>
    /// <exception cref="ArgumentNullException">Thrown when targetBitmap is null.</exception>
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

        int sourceWidth = cursor.ColorLayer.Width;
        int sourceHeight = cursor.ColorLayer.Height;
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
                cursorToUse = ScaleBitmap(cursor.ColorLayer, destinationSize, PixelFormat.Format32bppArgb, InterpolationMode.HighQualityBicubic);
            }

            try
            {
                DrawAlphaCursorOnBitmap(targetBitmap, cursorToUse, x, y);
            }
            finally
            {
                if (cursorToUse != cursor.ColorLayer)
                {
                    cursorToUse.Dispose();
                }
            }
            return;
        }

        // For legacy cursors with mask, apply AND/XOR operations, these are bit masks so they are scaled without interpolation
        Bitmap scaledColor = cursor.ColorLayer;
        Bitmap scaledMask = cursor.MaskLayer;

        try
        {
            if (needsScaling)
            {
                scaledColor = ScaleBitmap(cursor.ColorLayer, destinationSize, PixelFormat.Format24bppRgb, InterpolationMode.NearestNeighbor);
                scaledMask = ScaleBitmap(cursor.MaskLayer, destinationSize, PixelFormat.Format24bppRgb, InterpolationMode.NearestNeighbor);
            }
            DrawMaskedCursorOnBitmap(targetBitmap, scaledColor, scaledMask, x, y);
        }
        finally
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

    private static bool Is32BitFormat(PixelFormat format) =>
        format == PixelFormat.Format32bppArgb ||
        format == PixelFormat.Format32bppRgb ||
        format == PixelFormat.Format32bppPArgb;

    /// <summary>
    /// How the alpha channel of the target is interpreted
    /// </summary>
    private enum TargetAlphaMode
    {
        /// <summary>Format32bppArgb, straight alpha</summary>
        Straight,
        /// <summary>Format32bppPArgb, premultiplied alpha</summary>
        Premultiplied,
        /// <summary>Format32bppRgb and Format24bppRgb, the target is opaque</summary>
        Opaque
    }

    /// <summary>
    /// Draws a modern alpha-blended cursor onto a bitmap.
    /// </summary>
    private static void DrawAlphaCursorOnBitmap(Bitmap targetBitmap, Bitmap cursorBitmap, int x, int y)
    {
        Bitmap convertedBitmap = null;
        try
        {
            // The blending needs straight alpha, GDI+ converts other formats (also premultiplied) when cloning
            if (cursorBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                convertedBitmap = cursorBitmap.Clone(new Rectangle(0, 0, cursorBitmap.Width, cursorBitmap.Height), PixelFormat.Format32bppArgb);
                cursorBitmap = convertedBitmap;
            }

            // Dispatch based on target format
            switch (targetBitmap.PixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                    DrawAlphaCursor<Bgra32>(targetBitmap, cursorBitmap, x, y, TargetAlphaMode.Straight);
                    break;
                case PixelFormat.Format32bppPArgb:
                    DrawAlphaCursor<Bgra32>(targetBitmap, cursorBitmap, x, y, TargetAlphaMode.Premultiplied);
                    break;
                case PixelFormat.Format32bppRgb:
                    DrawAlphaCursor<Bgra32>(targetBitmap, cursorBitmap, x, y, TargetAlphaMode.Opaque);
                    break;
                case PixelFormat.Format24bppRgb:
                    DrawAlphaCursor<Bgr24>(targetBitmap, cursorBitmap, x, y, TargetAlphaMode.Opaque);
                    break;
                default:
                    throw new NotSupportedException($"Target bitmap format {targetBitmap.PixelFormat} is not supported.");
            }
        }
        finally
        {
            convertedBitmap?.Dispose();
        }
    }

    /// <summary>
    /// Generic implementation for alpha-blended cursor drawing, the cursor bitmap must be Format32bppArgb (straight alpha).
    /// </summary>
    private static void DrawAlphaCursor<TTarget>(Bitmap targetBitmap, Bitmap cursorBitmap, int x, int y, TargetAlphaMode targetAlphaMode)
        where TTarget : struct
    {
        using var targetAccessor = new BitmapAccessor<TTarget>(targetBitmap, readOnly: false);
        using var cursorAccessor = new BitmapAccessor<Bgra32>(cursorBitmap, readOnly: true);

        for (int cy = 0; cy < cursorAccessor.Height; cy++)
        {
            int ty = y + cy;
            if (ty < 0 || ty >= targetAccessor.Height) continue;

            var targetRow = targetAccessor.GetRowSpan(ty);
            var cursorRow = cursorAccessor.GetRowSpan(cy);

            for (int cx = 0; cx < cursorAccessor.Width; cx++)
            {
                int tx = x + cx;
                if (tx < 0 || tx >= targetAccessor.Width) continue;

                ref readonly var cursorPixel = ref cursorRow[cx];
                if (cursorPixel.A == 0) continue;

                if (typeof(TTarget) == typeof(Bgra32))
                {
                    ref var targetPixel = ref Unsafe.As<TTarget, Bgra32>(ref targetRow[tx]);
                    switch (targetAlphaMode)
                    {
                        case TargetAlphaMode.Premultiplied:
                            Bgra32.AlphaBlendPremultiplied(ref targetPixel, cursorPixel.ToPremultiplied());
                            break;
                        case TargetAlphaMode.Opaque:
                            // The alpha of a Format32bppRgb is undefined, treat it as opaque
                            targetPixel.A = 255;
                            Bgra32.AlphaBlend(ref targetPixel, in cursorPixel);
                            break;
                        default:
                            Bgra32.AlphaBlend(ref targetPixel, in cursorPixel);
                            break;
                    }
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
        // The color and mask are read with the same pixel type, convert them if the formats don't match
        Bitmap convertedColor = null;
        Bitmap convertedMask = null;
        try
        {
            bool use32Bit = Is32BitFormat(colorBitmap.PixelFormat) && Is32BitFormat(maskBitmap.PixelFormat);
            if (!use32Bit)
            {
                if (colorBitmap.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    convertedColor = colorBitmap.Clone(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height), PixelFormat.Format24bppRgb);
                    colorBitmap = convertedColor;
                }
                if (maskBitmap.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    convertedMask = maskBitmap.Clone(new Rectangle(0, 0, maskBitmap.Width, maskBitmap.Height), PixelFormat.Format24bppRgb);
                    maskBitmap = convertedMask;
                }
            }

            using var targetAccessor = new BitmapAccessor<TTarget>(targetBitmap, readOnly: false);
            if (use32Bit)
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
        finally
        {
            convertedColor?.Dispose();
            convertedMask?.Dispose();
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
        int height = Math.Min(colorAccessor.Height, maskAccessor.Height);
        int width = Math.Min(colorAccessor.Width, maskAccessor.Width);
        for (int cy = 0; cy < height; cy++)
        {
            int ty = y + cy;
            if (ty < 0 || ty >= targetAccessor.Height) continue;

            var targetRow = targetAccessor.GetRowSpan(ty);
            var colorRow = colorAccessor.GetRowSpan(cy);
            var maskRow = maskAccessor.GetRowSpan(cy);

            for (int cx = 0; cx < width; cx++)
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
