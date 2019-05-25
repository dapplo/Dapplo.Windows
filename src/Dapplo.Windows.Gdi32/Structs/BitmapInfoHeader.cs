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

using System.Runtime.InteropServices;
using Dapplo.Windows.Gdi32.Enums;

#endregion

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183381(v=vs.85).aspx">BITMAPV5HEADER structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BitmapInfoHeader
    {
        [FieldOffset(0)]
        private uint _biSize;
        [FieldOffset(4)]
        private int _biWidth;
        [FieldOffset(8)]
        private int _biHeight;
        [FieldOffset(12)]
        private ushort _biPlanes;
        [FieldOffset(14)]
        private ushort _biBitCount;
        [FieldOffset(16)]
        private BitmapCompressionMethods _biCompression;
        [FieldOffset(20)]
        private uint _biSizeImage;
        [FieldOffset(24)]
        private int _biXPelsPerMeter;
        [FieldOffset(28)]
        private int _biYPelsPerMeter;
        [FieldOffset(32)]
        private uint _biClrUsed;
        [FieldOffset(36)]
        private uint _biClrImportant;
        [FieldOffset(40)]
        private uint _bV5RedMask;
        [FieldOffset(44)]
        private uint _bV5GreenMask;
        [FieldOffset(48)]
        private uint _bV5BlueMask;
        [FieldOffset(52)]
        private uint _bV5AlphaMask;
        [FieldOffset(56)]
        private ColorSpace _bV5CSType;
        [FieldOffset(60)]
        private CieXyzTripple _bV5Endpoints;
        [FieldOffset(96)]
        private uint _bV5GammaRed;
        [FieldOffset(100)]
        private uint _bV5GammaGreen;
        [FieldOffset(104)]
        private uint _bV5GammaBlue;
        [FieldOffset(108)]
        private ColorSpace _bV5Intent;
        [FieldOffset(112)]
        private uint _bV5ProfileData;
        [FieldOffset(116)]
        private uint _bV5ProfileSize;
        [FieldOffset(120)]
        private uint _bV5Reserved;

        /// <summary>
        ///     The number of bytes required by the structure.
        ///     Applications should use this member to determine which bitmap information header structure is being used.
        /// </summary>
        public uint Size {
            get { return _biSize; }
            set { _biSize = value; }
        }

        /// <summary>
        ///     The width of the bitmap, in pixels.
        ///     If bV5Compression is BI_JPEG or BI_PNG, the bV5Width member specifies the width of the decompressed JPEG or PNG
        ///     image in pixels.
        /// </summary>
        public int Width
        {
            get { return _biWidth; }
            set { _biWidth = value; }
        }


        /// <summary>
        ///     The height of the bitmap, in pixels. If the value of bV5Height is positive, the bitmap is a bottom-up DIB and its
        ///     origin is the lower-left corner. If bV5Height value is negative, the bitmap is a top-down DIB and its origin is the
        ///     upper-left corner.
        ///     If bV5Height is negative, indicating a top-down DIB, bV5Compression must be either BI_RGB or BI_BITFIELDS. Top-down
        ///     DIBs cannot be compressed.
        ///     If bV5Compression is BI_JPEG or BI_PNG, the bV5Height member specifies the height of the decompressed JPEG or PNG
        ///     image in pixels.
        /// </summary>
        public int Height
        {
            get { return _biHeight; }
            set { _biHeight = value; }
        }

        /// <summary>
        ///     The number of planes for the target device. This value must be set to 1.
        /// </summary>
        public ushort Planes
        {
            get { return _biPlanes; }
            set { _biPlanes = value; }
        }

        /// <summary>
        ///     The number of bits that define each pixel and the maximum number of colors in the bitmap.
        ///     This member can be one of the following values:
        ///     0	The number of bits per pixel is specified or is implied by the JPEG or PNG file format.
        ///     1	The bitmap is monochrome, and the bmiColors member of BITMAPINFO contains two entries. Each bit in the bitmap
        ///     array represents a pixel. If the bit is clear, the pixel is displayed with the color of the first entry in the
        ///     bmiColors color table. If the bit is set, the pixel has the color of the second entry in the table.
        ///     4	The bitmap has a maximum of 16 colors, and the bmiColors member of BITMAPINFO contains up to 16 entries. Each
        ///     pixel in the bitmap is represented by a 4-bit index into the color table. For example, if the first byte in the
        ///     bitmap is 0x1F, the byte represents two pixels. The first pixel contains the color in the second table entry, and
        ///     the second pixel contains the color in the sixteenth table entry.
        ///     8	The bitmap has a maximum of 256 colors, and the bmiColors member of BITMAPINFO contains up to 256 entries. In
        ///     this case, each byte in the array represents a single pixel.
        ///     16	The bitmap has a maximum of 2^16 colors. If the bV5Compression member of the BITMAPV5HEADER structure is BI_RGB,
        ///     the bmiColors member of BITMAPINFO is NULL. Each WORD in the bitmap array represents a single pixel. The relative
        ///     intensities of red, green, and blue are represented with five bits for each color component. The value for blue is
        ///     in the least significant five bits, followed by five bits each for green and red. The most significant bit is not
        ///     used. The bmiColors color table is used for optimizing colors used on palette-based devices, and must contain the
        ///     number of entries specified by the bV5ClrUsed member of the BITMAPV5HEADER.
        ///     If the bV5Compression member of the BITMAPV5HEADER is BI_BITFIELDS, the bmiColors member contains three DWORD color
        ///     masks that specify the red, green, and blue components, respectively, of each pixel. Each WORD in the bitmap array
        ///     represents a single pixel.
        ///     When the bV5Compression member is BI_BITFIELDS, bits set in each DWORD mask must be contiguous and should not
        ///     overlap the bits of another mask. All the bits in the pixel do not need to be used.
        ///     24	The bitmap has a maximum of 2^24 colors, and the bmiColors member of BITMAPINFO is NULL. Each 3-byte triplet in
        ///     the bitmap array represents the relative intensities of blue, green, and red, respectively, for a pixel. The
        ///     bmiColors color table is used for optimizing colors used on palette-based devices, and must contain the number of
        ///     entries specified by the bV5ClrUsed member of the BITMAPV5HEADER structure.
        ///     32	The bitmap has a maximum of 2^32 colors. If the bV5Compression member of the BITMAPV5HEADER is BI_RGB, the
        ///     bmiColors member of BITMAPINFO is NULL. Each DWORD in the bitmap array represents the relative intensities of blue,
        ///     green, and red for a pixel. The value for blue is in the least significant 8 bits, followed by 8 bits each for
        ///     green and red. The high byte in each DWORD is not used. The bmiColors color table is used for optimizing colors
        ///     used on palette-based devices, and must contain the number of entries specified by the bV5ClrUsed member of the
        ///     BITMAPV5HEADER.
        ///     If the bV5Compression member of the BITMAPV5HEADER is BI_BITFIELDS, the bmiColors member contains three DWORD color
        ///     masks that specify the red, green, and blue components of each pixel. Each DWORD in the bitmap array represents a
        ///     single pixel.
        /// </summary>
        public ushort BitCount
        {
            get { return _biBitCount; }
            set { _biBitCount = value; }
        }

        /// <summary>
        ///     Specifies that the bitmap is not compressed.
        ///     The bV5RedMask, bV5GreenMask, and bV5BlueMask members specify the red, green, and blue components of each pixel.
        ///     This is valid when used with 16- and 32-bpp bitmaps.
        /// </summary>
        public BitmapCompressionMethods Compression
        {
            get { return _biCompression; }
            set { _biCompression = value; }
        }

        /// <summary>
        ///     The size, in bytes, of the image. This may be set to zero for BI_RGB bitmaps.
        ///     If bV5Compression is BI_JPEG or BI_PNG, bV5SizeImage is the size of the JPEG or PNG image buffer.
        /// </summary>
        public uint SizeImage
        {
            get { return _biSizeImage; }
            set { _biSizeImage = value; }
        }

        /// <summary>
        ///     The horizontal resolution, in pixels-per-meter, of the target device for the bitmap. An application can use this
        ///     value to select a bitmap from a resource group that best matches the characteristics of the current device.
        /// </summary>
        public int XPelsPerMeter
        {
            get { return _biXPelsPerMeter; }
            set { _biXPelsPerMeter = value; }
        }

        /// <summary>
        ///     The vertical resolution, in pixels-per-meter, of the target device for the bitmap.
        /// </summary>
        public int YPelsPerMeter
        {
            get { return _biYPelsPerMeter; }
            set { _biYPelsPerMeter = value; }
        }

        /// <summary>
        ///     The number of color indexes in the color table that are actually used by the bitmap. If this value is zero, the
        ///     bitmap uses the maximum number of colors corresponding to the value of the bV5BitCount member for the compression
        ///     mode specified by bV5Compression.
        ///     If bV5ClrUsed is nonzero and bV5BitCount is less than 16, the bV5ClrUsed member specifies the actual number of
        ///     colors the graphics engine or device driver accesses. If bV5BitCount is 16 or greater, the bV5ClrUsed member
        ///     specifies the size of the color table used to optimize performance of the system color palettes. If bV5BitCount
        ///     equals 16 or 32, the optimal color palette starts immediately following the BITMAPV5HEADER. If bV5ClrUsed is
        ///     nonzero, the color table is used on palettized devices, and bV5ClrUsed specifies the number of entries.
        /// </summary>
        public uint ColorsUsed
        {
            get { return _biClrUsed; }
            set { _biClrUsed = value; }
        }

        /// <summary>
        ///     The number of color indexes that are required for displaying the bitmap. If this value is zero, all colors are
        ///     required.
        /// </summary>
        public uint ColorsImportant
        {
            get { return _biClrImportant; }
            set { _biClrImportant = value; }
        }

        /// <summary>
        ///     Color mask that specifies the red component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint RedMask
        {
            get { return _bV5RedMask; }
            set { _bV5RedMask = value; }
        }


        /// <summary>
        ///     Color mask that specifies the green component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint GreenMask
        {
            get { return _bV5GreenMask; }
            set { _bV5GreenMask = value; }
        }


        /// <summary>
        ///     Color mask that specifies the blue component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint BlueMask
        {
            get { return _bV5BlueMask; }
            set { _bV5BlueMask = value; }
        }

        /// <summary>
        ///     Color mask that specifies the alpha component of each pixel.
        /// </summary>
        public uint AlphaMask
        {
            get { return _bV5AlphaMask; }
            set { _bV5AlphaMask = value; }
        }

        /// <summary>
        ///     The color space of the DIB.
        ///     See also
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd372165(v=vs.85).aspx">LOGCOLORSPACE structure</a>
        /// </summary>
        public ColorSpace ColorSpace
        {
            get { return _bV5CSType; }
            set { _bV5CSType = value; }
        }

        /// <summary>
        ///     A CIEXYZTRIPLE structure that specifies the x, y, and z coordinates of the three colors that correspond to the red,
        ///     green, and blue endpoints for the logical color space associated with the bitmap. This member is ignored unless the
        ///     bV5CSType member specifies LCS_CALIBRATED_RGB.
        /// </summary>
        public CieXyzTripple Endpoints
        {
            get { return _bV5Endpoints; }
            set { _bV5Endpoints = value; }
        }

        /// <summary>
        ///     Toned response curve for red. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaRed
        {
            get { return _bV5GammaRed; }
            set { _bV5GammaRed = value; }
        }


        /// <summary>
        ///     Toned response curve for green. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaGreen
        {
            get { return _bV5GammaGreen; }
            set { _bV5GammaRed = value; }
        }

        /// <summary>
        ///     Toned response curve for blue. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaBlue
        {
            get { return _bV5GammaBlue; }
            set { _bV5GammaBlue = value; }
        }

        /// <summary>
        ///     Rendering intent for bitmap. This can be one of the following values:
        ///     LCS_GM_ABS_COLORIMETRIC, LCS_GM_BUSINESS, LCS_GM_GRAPHICS, LCS_GM_IMAGES
        /// </summary>
        public ColorSpace Intent
        {
            get { return _bV5Intent; }
            set { _bV5Intent = value; }
        }

        /// <summary>
        ///     The offset, in bytes, from the beginning of the BITMAPV5HEADER structure to the start of the profile data. If the
        ///     profile is embedded, profile data is the actual profile, and it is linked. (The profile data is the null-terminated
        ///     file name of the profile.) This cannot be a Unicode string. It must be composed exclusively of characters from the
        ///     Windows character set (code page 1252). These profile members are ignored unless the bV5CSType member specifies
        ///     PROFILE_LINKED or PROFILE_EMBEDDED.
        /// </summary>
        public uint ProfileData
        {
            get { return _bV5ProfileData; }
            set { _bV5ProfileData = value; }
        }

        /// <summary>
        ///     Size, in bytes, of embedded profile data.
        /// </summary>
        public uint ProfileSize
        {
            get { return _bV5ProfileSize; }
            set { _bV5ProfileSize = value; }
        }

        /// <summary>
        ///     This member has been reserved. Its value should be set to zero.
        /// </summary>
        public uint Reserved
        {
            get { return _bV5Reserved; }
            set { _bV5Reserved = value; }
        }

        /// <summary>
        ///     Constructor with values
        /// </summary>
        /// <param name="width">int with the width of the bitmap</param>
        /// <param name="height">int with the height of the bitmap</param>
        /// <param name="bpp">int with the bits per pixel of the bitmap</param>
        public static BitmapInfoHeader Create(int width, int height, ushort bpp)
        {
            return new BitmapInfoHeader
            {
                // BITMAPINFOHEADER < DIBV5 is 40 bytes
                _biSize = (uint) Marshal.SizeOf(typeof(BitmapInfoHeader)),
                // Should allways be 1
                _biPlanes = 1,
                _biCompression = BitmapCompressionMethods.BI_RGB,
                _biWidth = width,
                _biHeight = height,
                _biBitCount = bpp,
                _biSizeImage = (uint) (width * height * (bpp >> 3)),
                _biXPelsPerMeter = 0,
                _biYPelsPerMeter = 0,
                _biClrUsed = 0,
                _biClrImportant = 0,

                // V5
                _bV5RedMask = (uint) 255 << 16,
                _bV5GreenMask = (uint) 255 << 8,
                _bV5BlueMask = 255,
                _bV5AlphaMask = (uint) 255 << 24,
                _bV5CSType = ColorSpace.LCS_sRGB,
                _bV5Endpoints = new CieXyzTripple
                {
                    Blue = CieXyz.Create(0),
                    Green = CieXyz.Create(0),
                    Red = CieXyz.Create(0)
                },
                _bV5GammaRed = 0,
                _bV5GammaGreen = 0,
                _bV5GammaBlue = 0,
                _bV5Intent = ColorSpace.LCS_GM_IMAGES,
                _bV5ProfileData = 0,
                _bV5ProfileSize = 0,
                _bV5Reserved = 0
            };
        }

        /// <summary>
        ///     Check if this is a DIB V5
        /// </summary>
        public bool IsDibV5
        {
            get
            {
                var sizeOfBmi = (uint) Marshal.SizeOf(typeof(BitmapInfoHeader));
                return _biSize >= sizeOfBmi;
            }
        }

        /// <summary>
        ///     Calculate the offset to the pixels
        /// </summary>
        public uint OffsetToPixels
        {
            get
            {
                if (_biCompression == BitmapCompressionMethods.BI_BITFIELDS)
                {
                    // Add 3x4 bytes for the bitfield color mask
                    return _biSize + 3 * 4;
                }
                return _biSize;
            }
        }
    }
}