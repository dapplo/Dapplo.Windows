//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     See<a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd183381(v=vs.85).aspx">BITMAPV5HEADER structure</a>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct BitmapInfoHeader
	{
		/// <summary>
		///     The number of bytes required by the structure.
		///     Applications should use this member to determine which bitmap information header structure is being used.
		/// </summary>
		[FieldOffset(0)]
		public uint biSize;

		/// <summary>
		///     The width of the bitmap, in pixels.
		///     If bV5Compression is BI_JPEG or BI_PNG, the bV5Width member specifies the width of the decompressed JPEG or PNG
		///     image in pixels.
		/// </summary>
		[FieldOffset(4)]
		public int biWidth;

		/// <summary>
		///     The height of the bitmap, in pixels. If the value of bV5Height is positive, the bitmap is a bottom-up DIB and its
		///     origin is the lower-left corner. If bV5Height value is negative, the bitmap is a top-down DIB and its origin is the
		///     upper-left corner.
		///     If bV5Height is negative, indicating a top-down DIB, bV5Compression must be either BI_RGB or BI_BITFIELDS. Top-down
		///     DIBs cannot be compressed.
		///     If bV5Compression is BI_JPEG or BI_PNG, the bV5Height member specifies the height of the decompressed JPEG or PNG
		///     image in pixels.
		/// </summary>
		[FieldOffset(8)]
		public int biHeight;

		/// <summary>
		///     The number of planes for the target device. This value must be set to 1.
		/// </summary>
		[FieldOffset(12)]
		public ushort biPlanes;

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
		[FieldOffset(14)]
		public ushort biBitCount;

		/// <summary>
		///     Specifies that the bitmap is not compressed.
		///     The bV5RedMask, bV5GreenMask, and bV5BlueMask members specify the red, green, and blue components of each pixel.
		///     This is valid when used with 16- and 32-bpp bitmaps.
		/// </summary>
		[FieldOffset(16)]
		public BitmapCompressionMethods biCompression;

		/// <summary>
		///     The size, in bytes, of the image. This may be set to zero for BI_RGB bitmaps.
		///     If bV5Compression is BI_JPEG or BI_PNG, bV5SizeImage is the size of the JPEG or PNG image buffer.
		/// </summary>
		[FieldOffset(20)]
		public uint biSizeImage;

		/// <summary>
		///     The horizontal resolution, in pixels-per-meter, of the target device for the bitmap. An application can use this
		///     value to select a bitmap from a resource group that best matches the characteristics of the current device.
		/// </summary>
		[FieldOffset(24)]
		public int biXPelsPerMeter;

		/// <summary>
		///     The vertical resolution, in pixels-per-meter, of the target device for the bitmap.
		/// </summary>
		[FieldOffset(28)]
		public int biYPelsPerMeter;

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
		[FieldOffset(32)]
		public uint biClrUsed;

		/// <summary>
		///     The number of color indexes that are required for displaying the bitmap. If this value is zero, all colors are
		///     required.
		/// </summary>
		[FieldOffset(36)]
		public uint biClrImportant;

		/// <summary>
		///     Color mask that specifies the red component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
		/// </summary>
		[FieldOffset(40)]
		public uint bV5RedMask;

		/// <summary>
		///     Color mask that specifies the green component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
		/// </summary>
		[FieldOffset(44)]
		public uint bV5GreenMask;

		/// <summary>
		///     Color mask that specifies the blue component of each pixel, valid only if bV5Compression is set to BI_BITFIELDS.
		/// </summary>
		[FieldOffset(48)]
		public uint bV5BlueMask;

		/// <summary>
		///     Color mask that specifies the alpha component of each pixel.
		/// </summary>
		[FieldOffset(52)]
		public uint bV5AlphaMask;

		/// <summary>
		///     The color space of the DIB.
		///     See also
		///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd372165(v=vs.85).aspx">LOGCOLORSPACE structure</a>
		/// </summary>
		[FieldOffset(56)]
		public ColorSpaceEnum bV5CSType;

		/// <summary>
		///     A CIEXYZTRIPLE structure that specifies the x, y, and z coordinates of the three colors that correspond to the red,
		///     green, and blue endpoints for the logical color space associated with the bitmap. This member is ignored unless the
		///     bV5CSType member specifies LCS_CALIBRATED_RGB.
		/// </summary>
		[FieldOffset(60)]
		public CieXyzTripple bV5Endpoints;

		/// <summary>
		///     Toned response curve for red. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
		///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
		/// </summary>
		[FieldOffset(96)]
		public uint bV5GammaRed;

		/// <summary>
		///     Toned response curve for green. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
		///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
		/// </summary>
		[FieldOffset(100)]
		public uint bV5GammaGreen;

		/// <summary>
		///     Toned response curve for blue. Used if bV5CSType is set to LCS_CALIBRATED_RGB. Specify in unsigned fixed 16.16
		///     format. The upper 16 bits are the unsigned integer value. The lower 16 bits are the fractional part.
		/// </summary>
		[FieldOffset(104)]
		public uint bV5GammaBlue;

		/// <summary>
		///     Rendering intent for bitmap. This can be one of the following values:
		///     LCS_GM_ABS_COLORIMETRIC, LCS_GM_BUSINESS, LCS_GM_GRAPHICS, LCS_GM_IMAGES
		/// </summary>
		[FieldOffset(108)]
		public ColorSpaceEnum bV5Intent;

		/// <summary>
		///     The offset, in bytes, from the beginning of the BITMAPV5HEADER structure to the start of the profile data. If the
		///     profile is embedded, profile data is the actual profile, and it is linked. (The profile data is the null-terminated
		///     file name of the profile.) This cannot be a Unicode string. It must be composed exclusively of characters from the
		///     Windows character set (code page 1252). These profile members are ignored unless the bV5CSType member specifies
		///     PROFILE_LINKED or PROFILE_EMBEDDED.
		/// </summary>
		[FieldOffset(112)]
		public uint bV5ProfileData;

		/// <summary>
		///     Size, in bytes, of embedded profile data.
		/// </summary>
		[FieldOffset(116)]
		public uint bV5ProfileSize;

		/// <summary>
		///     This member has been reserved. Its value should be set to zero.
		/// </summary>
		[FieldOffset(120)]
		public uint bV5Reserved;

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
				biSize = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader)),
				// Should allways be 1
				biPlanes = 1,
				biCompression = BitmapCompressionMethods.BI_RGB,
				biWidth = width,
				biHeight = height,
				biBitCount = bpp,
				biSizeImage = (uint)(width * height * (bpp >> 3)),
				biXPelsPerMeter = 0,
				biYPelsPerMeter = 0,
				biClrUsed = 0,
				biClrImportant = 0,

				// V5
				bV5RedMask = (uint)255 << 16,
				bV5GreenMask = (uint)255 << 8,
				bV5BlueMask = 255,
				bV5AlphaMask = (uint)255 << 24,
				bV5CSType = ColorSpaceEnum.LCS_sRGB,
				bV5Endpoints = new CieXyzTripple
				{
					CieXyzBlue = CieXyz.Create(0),
					CieXyzGreen = CieXyz.Create(0),
					CieXyzRed = CieXyz.Create(0)
				},
				bV5GammaRed = 0,
				bV5GammaGreen = 0,
				bV5GammaBlue = 0,
				bV5Intent = ColorSpaceEnum.LCS_GM_IMAGES,
				bV5ProfileData = 0,
				bV5ProfileSize = 0,
				bV5Reserved = 0
			};
		}

		/// <summary>
		/// Check if this is a DIB V5
		/// </summary>
		public bool IsDibV5
		{
			get
			{
				var sizeOfBMI = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader));
				return biSize >= sizeOfBMI;
			}
		}

		/// <summary>
		/// Calculate the offset to the pixels
		/// </summary>
		public uint OffsetToPixels
		{
			get
			{
				if (biCompression == BitmapCompressionMethods.BI_BITFIELDS)
				{
					// Add 3x4 bytes for the bitfield color mask
					return biSize + 3 * 4;
				}
				return biSize;
			}
		}
	}
}