/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
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
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Native
{
	/// <summary>
	/// GDI32 Helpers
	/// </summary>
	public static class Gdi32
	{
		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool BitBlt(SafeHandle hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, SafeHandle hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool StretchBlt(SafeHandle hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, SafeHandle hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, CopyPixelOperation dwRop);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern SafeCompatibleDcHandle CreateCompatibleDC(SafeHandle hDC);

		[DllImport("gdi32", SetLastError = true)]
		public static extern IntPtr SelectObject(SafeHandle hDC, SafeHandle hObject);

		[DllImport("gdi32", SetLastError = true)]
		public static extern SafeDibSectionHandle CreateDIBSection(SafeHandle hdc, ref BITMAPINFOHEADER bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

		[DllImport("gdi32", SetLastError = true)]
		public static extern SafeRegionHandle CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("gdi32", SetLastError = true)]
		public static extern uint GetPixel(SafeHandle hdc, int nXPos, int nYPos);

		[DllImport("gdi32", SetLastError = true)]
		public static extern int GetDeviceCaps(SafeHandle hdc, DeviceCaps nIndex);

		/// <summary>
		/// StretchBlt extension for the graphics object
		/// Doesn't work?
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public static void StretchBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, Rectangle destination)
		{
			using (SafeDeviceContextHandle targetDC = target.GetSafeDeviceContext())
			{
				using (SafeCompatibleDcHandle safeCompatibleDCHandle = CreateCompatibleDC(targetDC))
				{
					using (SafeHBitmapHandle hBitmapHandle = new SafeHBitmapHandle(sourceBitmap))
					{
						using (safeCompatibleDCHandle.SelectObject(hBitmapHandle))
						{
							StretchBlt(targetDC, destination.X, destination.Y, destination.Width, destination.Height, safeCompatibleDCHandle, source.Left, source.Top, source.Width, source.Height, CopyPixelOperation.SourceCopy);
						}
					}
				}
			}
		}

		/// <summary>
		/// Bitblt extension for the graphics object
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public static void BitBlt(this Graphics target, Bitmap sourceBitmap, Rectangle source, Point destination, CopyPixelOperation rop)
		{
			using (SafeDeviceContextHandle targetDC = target.GetSafeDeviceContext())
			{
				using (SafeCompatibleDcHandle safeCompatibleDCHandle = CreateCompatibleDC(targetDC))
				{
					using (SafeHBitmapHandle hBitmapHandle = new SafeHBitmapHandle(sourceBitmap.GetHbitmap()))
					{
						using (safeCompatibleDCHandle.SelectObject(hBitmapHandle))
						{
							BitBlt(targetDC, destination.X, destination.Y, source.Width, source.Height, safeCompatibleDCHandle, source.Left, source.Top, rop);
						}
					}
				}
			}
		}
	}
}
