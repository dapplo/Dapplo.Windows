/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Windows.Enums;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Structs
{
	[StructLayout(LayoutKind.Explicit)]
	public struct BITMAPINFOHEADER
	{
		[FieldOffset(0)]
		public uint biSize;

		[FieldOffset(4)]
		public int biWidth;

		[FieldOffset(8)]
		public int biHeight;

		[FieldOffset(12)]
		public ushort biPlanes;

		[FieldOffset(14)]
		public ushort biBitCount;

		[FieldOffset(16)]
		public BI_COMPRESSION biCompression;

		[FieldOffset(20)]
		public uint biSizeImage;

		[FieldOffset(24)]
		public int biXPelsPerMeter;

		[FieldOffset(28)]
		public int biYPelsPerMeter;

		[FieldOffset(32)]
		public uint biClrUsed;

		[FieldOffset(36)]
		public uint biClrImportant;

		[FieldOffset(40)]
		public uint bV5RedMask;

		[FieldOffset(44)]
		public uint bV5GreenMask;

		[FieldOffset(48)]
		public uint bV5BlueMask;

		[FieldOffset(52)]
		public uint bV5AlphaMask;

		[FieldOffset(56)]
		public uint bV5CSType;

		[FieldOffset(60)]
		public CIEXYZTRIPLE bV5Endpoints;

		[FieldOffset(96)]
		public uint bV5GammaRed;

		[FieldOffset(100)]
		public uint bV5GammaGreen;

		[FieldOffset(104)]
		public uint bV5GammaBlue;

		[FieldOffset(108)]
		public uint bV5Intent; // Rendering intent for bitmap 

		[FieldOffset(112)]
		public uint bV5ProfileData;

		[FieldOffset(116)]
		public uint bV5ProfileSize;

		[FieldOffset(120)]
		public uint bV5Reserved;

		public const int DIB_RGB_COLORS = 0;

		public BITMAPINFOHEADER(int width, int height, ushort bpp)
		{
			biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)); // BITMAPINFOHEADER < DIBV5 is 40 bytes
			biPlanes = 1; // Should allways be 1
			biCompression = BI_COMPRESSION.BI_RGB;
			biWidth = width;
			biHeight = height;
			biBitCount = bpp;
			biSizeImage = (uint)(width * height * (bpp >> 3));
			biXPelsPerMeter = 0;
			biYPelsPerMeter = 0;
			biClrUsed = 0;
			biClrImportant = 0;

			// V5
			bV5RedMask = (uint)255 << 16;
			bV5GreenMask = (uint)255 << 8;
			bV5BlueMask = (uint)255;
			bV5AlphaMask = (uint)255 << 24;
			bV5CSType = 1934772034; // sRGB
			bV5Endpoints = new CIEXYZTRIPLE();
			bV5Endpoints.ciexyzBlue = new CIEXYZ(0);
			bV5Endpoints.ciexyzGreen = new CIEXYZ(0);
			bV5Endpoints.ciexyzRed = new CIEXYZ(0);
			bV5GammaRed = 0;
			bV5GammaGreen = 0;
			bV5GammaBlue = 0;
			bV5Intent = 4;
			bV5ProfileData = 0;
			bV5ProfileSize = 0;
			bV5Reserved = 0;
		}

		public bool IsDibV5
		{
			get
			{
				uint sizeOfBMI = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
				return biSize >= sizeOfBMI;
			}
		}

		public uint OffsetToPixels
		{
			get
			{
				if (biCompression == BI_COMPRESSION.BI_BITFIELDS)
				{
					// Add 3x4 bytes for the bitfield color mask
					return biSize + 3 * 4;
				}
				return biSize;
			}
		}
	}
}
