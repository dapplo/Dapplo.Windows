// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace Dapplo.Windows.Gdi32;

/// <summary>
///     Gdi32 Helpers
/// </summary>
public static class Gdi32Api
{
    private const string GDI32Dll = "gdi32.dll";

    /// <summary>
    ///     The BitBlt function performs a bit-block transfer of the color data corresponding to a rectangle of pixels from the
    ///     specified source device context into a destination device context.
    /// </summary>
    /// <param name="hdcDest">A handle to the destination device context.</param>
    /// <param name="nXDest">The x-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
    /// <param name="nYDest">The y-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
    /// <param name="nWidth">The width, in logical units, of the source and destination rectangles.</param>
    /// <param name="nHeight">The height, in logical units, of the source and the destination rectangles.</param>
    /// <param name="hdcSrc">A handle to the source device context.</param>
    /// <param name="nXSrc">The x-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
    /// <param name="nYSrc">The y-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
    /// <param name="rasterOperation">
    ///     A raster-operation code. These codes define how the color data for the source rectangle
    ///     is to be combined with the color data for the destination rectangle to achieve the final color.
    /// </param>
    /// <returns></returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BitBlt(SafeHandle hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, SafeHandle hdcSrc, int nXSrc, int nYSrc, RasterOperations rasterOperation);

    /// <summary>
    ///     Bitblt extension for the graphics object
    /// </summary>
    /// <param name="target">Graphics</param>
    /// <param name="sourceBitmap">Bitmap</param>
    /// <param name="source">Rectangle</param>
    /// <param name="destination">Point</param>
    /// <param name="rasterOperations">RasterOperations</param>
    public static void BitBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, NativePoint destination, RasterOperations rasterOperations)
    {
        using (var targetDc = target.GetSafeDeviceContext())
        using (var safeCompatibleDcHandle = CreateCompatibleDC(targetDc))
        using (var hBitmapHandle = new SafeHBitmapHandle(sourceBitmap.GetHbitmap()))
        using (safeCompatibleDcHandle.SelectObject(hBitmapHandle))
        {
            BitBlt(targetDc, destination.X, destination.Y, source.Width, source.Height, safeCompatibleDcHandle, source.Left, source.Top, rasterOperations);
        }
    }

    /// <summary>
    ///     The CreateCompatibleDC function creates a memory device context (DC) compatible with the specified device.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183489(v=vs.85).aspx">
    ///         CreateCompatibleDC
    ///         function
    ///     </a>
    /// </summary>
    /// <param name="hDc">
    ///     A handle to an existing DC. If this handle is NULL, the function creates a memory DC compatible with
    ///     the application's current screen.
    /// </param>
    /// <returns>
    ///     If the function succeeds, the return value is the handle to a memory DC.
    ///     If the function fails, the return value is NULL.
    /// </returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern SafeCompatibleDcHandle CreateCompatibleDC(SafeHandle hDc);

    /// <summary>
    ///     The CreateDIBSection function creates a DIB that applications can write to directly.
    ///     The function gives you a pointer to the location of the bitmap bit values.
    ///     You can supply a handle to a file-mapping object that the function will use to create the bitmap, or you can let
    ///     the system allocate the memory for the bitmap.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183494(v=vs.85).aspx">CreateDIBSection function</a>
    /// </summary>
    /// <param name="hdc">
    ///     A handle to a device context. If the value of iUsage is DIB_PAL_COLORS, the function uses this device
    ///     context's logical palette to initialize the DIB colors.
    /// </param>
    /// <param name="bmi">
    ///     A pointer to a BITMAPINFO structure that specifies various attributes of the DIB, including the
    ///     bitmap dimensions and colors.
    /// </param>
    /// <param name="usage">
    ///     The type of data contained in the bmiColors array member of the BITMAPINFO structure pointed to by pbmi (either
    ///     logical palette indexes or literal RGB values).
    ///     The following values are defined.
    ///     DIB_PAL_COLORS The bmiColors member is an array of 16-bit indexes into the logical palette of the device context
    ///     specified by hdc.
    ///     DIB_RGB_COLORS The BITMAPINFO structure contains an array of literal RGB values.
    /// </param>
    /// <param name="bits">A pointer to a variable that receives a pointer to the location of the DIB bit values.</param>
    /// <param name="hSection">
    ///     A handle to a file-mapping object that the function will use to create the DIB. This parameter can be NULL.
    ///     If hSection is not NULL, it must be a handle to a file-mapping object created by calling the CreateFileMapping
    ///     function with the PAGE_READWRITE or PAGE_WRITECOPY flag. Read-only DIB sections are not supported. Handles created
    ///     by other means will cause CreateDIBSection to fail.
    ///     If hSection is not NULL, the CreateDIBSection function locates the bitmap bit values at offset dwOffset in the
    ///     file-mapping object referred to by hSection. An application can later retrieve the hSection handle by calling the
    ///     GetObject function with the HBITMAP returned by CreateDIBSection.
    ///     If hSection is NULL, the system allocates memory for the DIB. In this case, the CreateDIBSection function ignores
    ///     the dwOffset parameter. An application cannot later obtain a handle to this memory. The dshSection member of the
    ///     DIBSECTION structure filled in by calling the GetObject function will be NULL.
    /// </param>
    /// <param name="dwOffset">
    ///     The offset from the beginning of the file-mapping object referenced by hSection where storage
    ///     for the bitmap bit values is to begin. This value is ignored if hSection is NULL. The bitmap bit values are aligned
    ///     on doubleword boundaries, so dwOffset must be a multiple of the size of a DWORD.
    /// </param>
    /// <returns>
    ///     If the function succeeds, the return value is a handle to the newly created DIB, and *ppvBits points to the bitmap
    ///     bit values.
    ///     If the function fails, the return value is NULL, and *ppvBits is NULL.
    /// </returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern SafeDibSectionHandle CreateDIBSection(SafeHandle hdc, ref BitmapV5Header bmi, DibColors usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

    /// <summary>
    ///     The CreateRectRgn function creates a rectangular region.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183514(v=vs.85).aspx">CreateRectRgn function</a>
    /// </summary>
    /// <param name="nLeftRect">Specifies the x-coordinate of the upper-left corner of the region in logical units.</param>
    /// <param name="nTopRect">Specifies the y-coordinate of the upper-left corner of the region in logical units.</param>
    /// <param name="nRightRect">Specifies the x-coordinate of the lower-right corner of the region in logical units.</param>
    /// <param name="nBottomRect">Specifies the y-coordinate of the lower-right corner of the region in logical units.</param>
    /// <returns>
    ///     If the function succeeds, the return value is the handle to the region.
    ///     If the function fails, the return value is NULL.
    /// </returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern SafeRegionHandle CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144877(v=vs.85).aspx">GetDeviceCaps function</a>
    ///     The GetDeviceCaps function retrieves device-specific information for the specified device.
    /// </summary>
    /// <param name="hdc">A handle to the DC.</param>
    /// <param name="nIndex">The item to be returned</param>
    /// <returns></returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern int GetDeviceCaps(SafeHandle hdc, DeviceCaps nIndex);

    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144909(v=vs.85).aspx">GetPixel function</a>
    ///     The GetPixel function retrieves the red, green, blue (RGB) color value of the pixel at the specified coordinates.
    /// </summary>
    /// <param name="hdc">A handle to the device context.</param>
    /// <param name="nXPos">The x-coordinate, in logical units, of the pixel to be examined.</param>
    /// <param name="nYPos">The y-coordinate, in logical units, of the pixel to be examined.</param>
    /// <returns>
    ///     The return value is the COLORREF value that specifies the RGB of the pixel. If the pixel is outside of the
    ///     current clipping region, the return value is CLR_INVALID (0xFFFFFFFF defined in Wingdi.h).
    /// </returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern uint GetPixel(SafeHandle hdc, int nXPos, int nYPos);

    /// <summary>
    ///     The SelectObject function selects an object into the specified device context (DC). The new object replaces the
    ///     previous object of the same type.
    /// </summary>
    /// <param name="hDc">A handle to the DC.</param>
    /// <param name="hObject">
    ///     A handle to the object to be selected. The specified object must have been created by using one of the following
    ///     functions.
    ///     Object	Functions
    ///     Bitmap  CreateBitmap, CreateBitmapIndirect, CreateCompatibleBitmap, CreateDIBitmap, CreateDIBSection
    ///     (Bitmaps can only be selected into memory DC's. A single bitmap cannot be selected into more than one DC at the
    ///     same time.)
    ///     Brush   CreateBrushIndirect, CreateDIBPatternBrush, CreateDIBPatternBrushPt, CreateHatchBrush, CreatePatternBrush,
    ///     CreateSolidBrush
    ///     Font    CreateFont, CreateFontIndirect
    ///     Pen     CreatePen, CreatePenIndirect
    ///     Region  CombineRgn, CreateEllipticRgn, CreateEllipticRgnIndirect, CreatePolygonRgn, CreateRectRgn,
    ///     CreateRectRgnIndirect
    /// </param>
    /// <returns>
    ///     If the selected object is not a region and the function succeeds, the return value is a handle to the object being
    ///     replaced.
    ///     If the selected object is a region and the function succeeds, the return value is one of the following values.
    ///     SIMPLEREGION	Region consists of a single rectangle.
    ///     COMPLEXREGION	Region consists of more than one rectangle.
    ///     NULLREGION	Region is empty.
    /// </returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern SafeNonDisposableObjectHandle SelectObject(SafeDcHandle hDc, SafeObjectHandle hObject);

    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145120(v=vs.85).aspx">StretchBlt function</a>
    /// </summary>
    /// <param name="hdcDest">A handle to the destination device context.</param>
    /// <param name="nXOriginDest">The x-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
    /// <param name="nYOriginDest">The y-coordinate, in logical units, of the upper-left corner of the destination rectangle.</param>
    /// <param name="nWidthDest">The width, in logical units, of the destination rectangle.</param>
    /// <param name="nHeightDest">The height, in logical units, of the destination rectangle.</param>
    /// <param name="hdcSrc">A handle to the source device context.</param>
    /// <param name="nXOriginSrc">The x-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
    /// <param name="nYOriginSrc">The y-coordinate, in logical units, of the upper-left corner of the source rectangle.</param>
    /// <param name="nWidthSrc">The width, in logical units, of the source rectangle.</param>
    /// <param name="nHeightSrc">The height, in logical units, of the source rectangle.</param>
    /// <param name="rasterOperation">
    ///     he raster operation to be performed. Raster operation codes define how the system combines colors in output
    ///     operations that involve a brush, a source bitmap, and a destination bitmap.
    ///     See BitBlt for a list of common raster operation codes (ROPs). Note that the CAPTUREBLT ROP generally cannot be
    ///     used for printing device contexts.
    /// </param>
    /// <returns>bool true if success</returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StretchBlt(SafeHandle hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, SafeHandle hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, RasterOperations rasterOperation);

    /// <summary>
    ///     StretchBlt extension for the graphics object
    ///     Doesn't work?
    /// </summary>
    /// <param name="target">Graphics</param>
    /// <param name="sourceBitmap">Bitmap</param>
    /// <param name="source">Rectangle</param>
    /// <param name="destination">Rectangle</param>
    /// <param name="rasterOperation">RasterOperations</param>
    public static void StretchBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, Rectangle destination, RasterOperations rasterOperation)
    {
        using (var targetDc = target.GetSafeDeviceContext())
        using (var safeCompatibleDcHandle = CreateCompatibleDC(targetDc))
        using (var hBitmapHandle = new SafeHBitmapHandle(sourceBitmap))
        using (safeCompatibleDcHandle.SelectObject(hBitmapHandle))
        {
            StretchBlt(targetDc, destination.X, destination.Y, destination.Width, destination.Height, safeCompatibleDcHandle, source.Left, source.Top, source.Width, source.Height, rasterOperation);
        }
    }

    /// <summary>
    /// Retrieves information about a specified graphics object, such as a bitmap, and copies it into a provided buffer.
    /// </summary>
    /// <remarks>This method can be used to obtain details about various types of graphics objects, such as
    /// bitmaps. Ensure that the buffer size specified in cbBuffer is sufficient for the object type. If the function
    /// fails, call GetLastError to obtain extended error information.</remarks>
    /// <param name="hgdiobj">A handle to the graphics object for which information is to be retrieved. This must be a valid handle to an
    /// object created by a GDI function.</param>
    /// <param name="cbBuffer">The size, in bytes, of the buffer that receives the information. The buffer must be large enough to hold the
    /// data for the object type being queried.</param>
    /// <param name="lpvObject">A reference to a Bitmap structure that receives the information about the graphics object. The structure is
    /// populated with data if the call succeeds.</param>
    /// <returns>The number of bytes copied to the buffer if successful; otherwise, zero if the function fails.</returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern int GetObject(SafeHBitmapHandle hgdiobj, int cbBuffer, ref Structs.GdiBitmap lpvObject);

    /// <summary>
    /// The DeleteObject function deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.
    /// </summary>
    /// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
    /// <returns>bool</returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// Creates a logical brush with a solid color for use in GDI drawing operations.
    /// </summary>
    /// <param name="crColor">The color of the brush, specified as a COLORREF value. The low-order byte contains the red component, the next
    /// byte contains the green component, and the third byte contains the blue component.</param>
    /// <returns>A handle to the created logical brush, or IntPtr.Zero if the function fails.</returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern IntPtr CreateSolidBrush(uint crColor);

    /// <summary>
    /// Retrieves the bits of a device-independent bitmap (DIB) and copies them into a buffer, using the specified device context and bitmap handle.
    /// </summary>
    /// <remarks>This method is a P/Invoke wrapper for the native GDI GetDIBits function. The caller is
    /// responsible for ensuring that the buffer pointed to by lpvBits is large enough to hold the requested bitmap
    /// data. If lpvBits is null, the function fills the bmi structure with information about the bitmap without copying
    /// any bits.</remarks>
    /// <param name="hdc">A handle to the device context used for the operation. This must be compatible with the bitmap specified by the
    /// hbm parameter.</param>
    /// <param name="hbm">A handle to the bitmap whose bits are to be retrieved.</param>
    /// <param name="start">The starting scan line index, zero-based, from which to begin retrieving bitmap data.</param>
    /// <param name="cLines">The number of scan lines to retrieve from the bitmap, beginning at the start parameter.</param>
    /// <param name="lpvBits">A pointer to the buffer that receives the bitmap bits. If this parameter is null, the function fills the bmi
    /// structure with information about the bitmap.</param>
    /// <param name="bmi">A reference to a BitmapInfoHeader structure that specifies the desired format for the DIB and receives
    /// information about the bitmap.</param>
    /// <param name="usage">Specifies whether the bmi colors are provided as a color table or as direct RGB values. Typically set to
    /// DIB_RGB_COLORS or DIB_PAL_COLORS.</param>
    /// <returns>The number of scan lines copied into the buffer, or zero if the operation fails.</returns>
    [DllImport(GDI32Dll, SetLastError = true)]
    public static extern int GetDIBits(IntPtr hdc, SafeHBitmapHandle hbm, uint start, uint cLines, IntPtr lpvBits, ref BitmapInfoHeader bmi, DibColors usage);
}