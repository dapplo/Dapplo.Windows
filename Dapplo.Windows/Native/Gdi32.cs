//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using Dapplo.Windows.Enums;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     GDI32 Helpers
	/// </summary>
	public static class Gdi32
	{
		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool BitBlt(SafeHandle hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, SafeHandle hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

		/// <summary>
		///     Bitblt extension for the graphics object
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public static void BitBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, Point destination, CopyPixelOperation rop)
		{
			using (var targetDC = target.GetSafeDeviceContext())
			{
				using (var safeCompatibleDCHandle = CreateCompatibleDC(targetDC))
				{
					using (var hBitmapHandle = new SafeHBitmapHandle(sourceBitmap.GetHbitmap()))
					{
						using (safeCompatibleDCHandle.SelectObject(hBitmapHandle))
						{
							BitBlt(targetDC, destination.X, destination.Y, source.Width, source.Height, safeCompatibleDCHandle, source.Left, source.Top, rop);
						}
					}
				}
			}
		}

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern SafeCompatibleDcHandle CreateCompatibleDC(SafeHandle hDC);

		[DllImport("gdi32", SetLastError = true)]
		public static extern SafeDibSectionHandle CreateDIBSection(SafeHandle hdc, ref BitmapInfoHeader bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

		[DllImport("gdi32", SetLastError = true)]
		public static extern SafeRegionHandle CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("gdi32", SetLastError = true)]
		public static extern int GetDeviceCaps(SafeHandle hdc, DeviceCaps nIndex);

		[DllImport("gdi32", SetLastError = true)]
		public static extern uint GetPixel(SafeHandle hdc, int nXPos, int nYPos);

		[DllImport("gdi32", SetLastError = true)]
		public static extern IntPtr SelectObject(SafeHandle hDC, SafeHandle hObject);

		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool StretchBlt(SafeHandle hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, SafeHandle hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, CopyPixelOperation dwRop);

		/// <summary>
		///     StretchBlt extension for the graphics object
		///     Doesn't work?
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public static void StretchBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, Rectangle destination)
		{
			using (var targetDC = target.GetSafeDeviceContext())
			{
				using (var safeCompatibleDCHandle = CreateCompatibleDC(targetDC))
				{
					using (var hBitmapHandle = new SafeHBitmapHandle(sourceBitmap))
					{
						using (safeCompatibleDCHandle.SelectObject(hBitmapHandle))
						{
							StretchBlt(targetDC, destination.X, destination.Y, destination.Width, destination.Height, safeCompatibleDCHandle, source.Left, source.Top, source.Width, source.Height, CopyPixelOperation.SourceCopy);
						}
					}
				}
			}
		}
	}
}