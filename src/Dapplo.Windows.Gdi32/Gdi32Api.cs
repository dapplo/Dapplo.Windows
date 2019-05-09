//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;

#endregion

namespace Dapplo.Windows.Gdi32
{
    /// <summary>
    ///     Gdi32 Helpers
    /// </summary>
    public static class Gdi32Api
    {
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
        [DllImport("gdi32", SetLastError = true)]
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
        [DllImport("gdi32", SetLastError = true)]
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
        [DllImport("gdi32", SetLastError = true)]
        public static extern SafeDibSectionHandle CreateDIBSection(SafeHandle hdc, ref BitmapInfoHeader bmi, DibColors usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

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
        [DllImport("gdi32", SetLastError = true)]
        public static extern SafeRegionHandle CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144877(v=vs.85).aspx">GetDeviceCaps function</a>
        ///     The GetDeviceCaps function retrieves device-specific information for the specified device.
        /// </summary>
        /// <param name="hdc">A handle to the DC.</param>
        /// <param name="nIndex">The item to be returned</param>
        /// <returns></returns>
        [DllImport("gdi32", SetLastError = true)]
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
        [DllImport("gdi32", SetLastError = true)]
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
        [DllImport("gdi32", SetLastError = true)]
        public static extern IntPtr SelectObject(SafeHandle hDc, SafeHandle hObject);

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
        [DllImport("gdi32", SetLastError = true)]
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
        /// The DeleteObject function deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.
        /// </summary>
        /// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        /// <returns>bool</returns>
        [DllImport("gdi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}