// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.Gdi32.Enums;

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    ///     See
    ///     <a href="https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapv4header">BITMAPV4HEADER structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BitmapV4Header
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

        /// <summary>
        ///     The number of bytes required by the structure.
        ///     Applications should use this member to determine which bitmap information header structure is being used.
        /// </summary>
        public uint Size {
            get => _biSize;
            set => _biSize = value;
        }

        /// <summary>
        ///     The width of the bitmap, in pixels.
        ///     If bV5Compression is BI_JPEG or BI_PNG, the bV5Width member specifies the width of the decompressed JPEG or PNG
        ///     image in pixels.
        /// </summary>
        public int Width
        {
            get => _biWidth;
            set => _biWidth = value;
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
            get => _biHeight;
            set => _biHeight = value;
        }

        /// <summary>
        ///     The number of planes for the target device. This value must be set to 1.
        /// </summary>
        public ushort Planes
        {
            get => _biPlanes;
            set => _biPlanes = value;
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
            get => _biBitCount;
            set => _biBitCount = value;
        }

        /// <summary>
        ///     Specifies that the bitmap is not compressed.
        ///     The bV5RedMask, bV5GreenMask, and bV5BlueMask members specify the red, green, and blue components of each pixel.
        ///     This is valid when used with 16- and 32-bpp bitmaps.
        /// </summary>
        public BitmapCompressionMethods Compression
        {
            get => _biCompression;
            set => _biCompression = value;
        }

        /// <summary>
        ///     The size, in bytes, of the image. This may be set to zero for BI_RGB bitmaps.
        ///     If bV5Compression is BI_JPEG or BI_PNG, bV5SizeImage is the size of the JPEG or PNG image buffer.
        /// </summary>
        public uint SizeImage
        {
            get => _biSizeImage;
            set => _biSizeImage = value;
        }

        /// <summary>
        ///     The horizontal resolution, in pixels-per-meter, of the target device for the bitmap. An application can use this
        ///     value to select a bitmap from a resource group that best matches the characteristics of the current device.
        /// </summary>
        public int XPelsPerMeter
        {
            get => _biXPelsPerMeter;
            set => _biXPelsPerMeter = value;
        }

        /// <summary>
        ///     The vertical resolution, in pixels-per-meter, of the target device for the bitmap.
        /// </summary>
        public int YPelsPerMeter
        {
            get => _biYPelsPerMeter;
            set => _biYPelsPerMeter = value;
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
            get => _biClrUsed;
            set => _biClrUsed = value;
        }

        /// <summary>
        ///     The number of color indexes that are required for displaying the bitmap. If this value is zero, all colors are
        ///     required.
        /// </summary>
        public uint ColorsImportant
        {
            get => _biClrImportant;
            set => _biClrImportant = value;
        }

        /// <summary>
        ///     Color mask that specifies the red component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint RedMask
        {
            get => _bV5RedMask;
            set => _bV5RedMask = value;
        }


        /// <summary>
        ///     Color mask that specifies the green component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint GreenMask
        {
            get => _bV5GreenMask;
            set => _bV5GreenMask = value;
        }


        /// <summary>
        ///     Color mask that specifies the blue component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
        /// </summary>
        public uint BlueMask
        {
            get => _bV5BlueMask;
            set => _bV5BlueMask = value;
        }

        /// <summary>
        ///     Color mask that specifies the alpha component of each pixel.
        /// </summary>
        public uint AlphaMask
        {
            get => _bV5AlphaMask;
            set => _bV5AlphaMask = value;
        }

        /// <summary>
        ///     The color space of the DIB.
        ///     See also
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd372165(v=vs.85).aspx">LOGCOLORSPACE structure</a>
        /// </summary>
        public ColorSpace ColorSpace
        {
            get => _bV5CSType;
            set => _bV5CSType = value;
        }

        /// <summary>
        ///     A CIEXYZTRIPLE structure that specifies the x, y, and z coordinates of the three colors that correspond to the red,
        ///     green, and blue endpoints for the logical color space associated with the bitmap. This member is ignored unless the
        ///     bV5CSType member specifies LCS_CALIBRATED_RGB.
        /// </summary>
        public CieXyzTripple Endpoints
        {
            get => _bV5Endpoints;
            set => _bV5Endpoints = value;
        }

        /// <summary>
        ///     Toned response curve for red. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaRed
        {
            get => _bV5GammaRed;
            set => _bV5GammaRed = value;
        }


        /// <summary>
        ///     Toned response curve for green. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaGreen
        {
            get => _bV5GammaGreen;
            set => _bV5GammaRed = value;
        }

        /// <summary>
        ///     Toned response curve for blue. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
        ///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
        /// </summary>
        public uint GammaBlue
        {
            get => _bV5GammaBlue;
            set => _bV5GammaBlue = value;
        }

        /// <summary>
        ///     Constructor with values
        /// </summary>
        /// <param name="width">int with the width of the bitmap</param>
        /// <param name="height">int with the height of the bitmap</param>
        /// <param name="bpp">int with the bits per pixel of the bitmap</param>
        public static BitmapV4Header Create(int width, int height, ushort bpp)
        {
            return new BitmapV4Header
            {
                // BITMAPINFOHEADER < DIBV5 is 40 bytes
                _biSize = (uint) Marshal.SizeOf(typeof(BitmapV4Header)),
                // Should always be 1
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
                _bV5GammaBlue = 0
            };
        }

        /// <summary>
        ///     Check if this is a DIB V5
        /// </summary>
        public bool IsDibV4
        {
            get
            {
                var sizeOfBmi = (uint)Marshal.SizeOf(typeof(BitmapV4Header));
                return _biSize == sizeOfBmi;
            }
        }

        /// <summary>
        ///     Check if this is a DIB V5
        /// </summary>
        public bool IsDibV5
        {
            get
            {
                var sizeOfBmi = (uint) Marshal.SizeOf(typeof(BitmapV5Header));
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