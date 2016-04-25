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
using Dapplo.Windows.Structs;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Native
{
	/// <summary>
	/// GDIplus Helpers
	/// </summary>
	public static class Gdiplus
	{
		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
		private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref RECT rectOfInterest, [MarshalAs(UnmanagedType.Bool)] bool useAuxData, IntPtr auxData, int auxDataSize);

		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
		private static extern int GdipDrawImageFX(IntPtr graphics, IntPtr bitmap, ref RECTF source, IntPtr matrix, IntPtr effect, IntPtr imageAttributes, GpUnit srcUnit);

		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
		private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);

		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
		private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);

		[DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
		private static extern int GdipDeleteEffect(IntPtr effect);

		private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");

		// Constant "FieldInfo" for getting the nativeImage from the Bitmap
		private static readonly FieldInfo FIELD_INFO_NATIVE_IMAGE = typeof(Bitmap).GetField("nativeImage", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
		// Constant "FieldInfo" for getting the NativeGraphics from the Graphics
		private static readonly FieldInfo FIELD_INFO_NATIVE_GRAPHICS = typeof(Graphics).GetField("nativeGraphics", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
		// Constant "FieldInfo" for getting the nativeMatrix from the Matrix
		private static readonly FieldInfo FIELD_INFO_NATIVE_MATRIX = typeof(Matrix).GetField("nativeMatrix", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
		// Constant "FieldInfo" for getting the nativeImageAttributes from the ImageAttributes
		private static readonly FieldInfo FIELD_INFO_NATIVE_IMAGEATTRIBUTES = typeof(ImageAttributes).GetField("nativeImageAttributes", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

		private static bool isBlurEnabled = Environment.OSVersion.Version.Major >= 6;

		/// <summary>
		/// Get the nativeImage field from the bitmap
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns>IntPtr</returns>
		private static IntPtr GetNativeImage(Bitmap bitmap)
		{
			if (bitmap == null)
			{
				return IntPtr.Zero;
			}
			return (IntPtr)FIELD_INFO_NATIVE_IMAGE.GetValue(bitmap);
		}

		/// <summary>
		/// Get the NativeGraphics field from the graphics
		/// </summary>
		/// <param name="graphics"></param>
		/// <returns>IntPtr</returns>
		private static IntPtr GetNativeGraphics(Graphics graphics)
		{
			if (graphics == null)
			{
				return IntPtr.Zero;
			}
			return (IntPtr)FIELD_INFO_NATIVE_GRAPHICS.GetValue(graphics);
		}

		/// <summary>
		/// Get the nativeMatrix field from the matrix
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns>IntPtr</returns>
		private static IntPtr GetNativeMatrix(Matrix matrix)
		{
			if (matrix == null)
			{
				return IntPtr.Zero;
			}
			return (IntPtr)FIELD_INFO_NATIVE_MATRIX.GetValue(matrix);
		}

		/// <summary>
		/// Get the nativeImageAttributes field from the ImageAttributes
		/// </summary>
		/// <param name="imageAttributes"></param>
		/// <returns>IntPtr</returns>
		private static IntPtr GetNativeImageAttributes(ImageAttributes imageAttributes)
		{
			if (imageAttributes == null)
			{
				return IntPtr.Zero;
			}
			return (IntPtr)FIELD_INFO_NATIVE_IMAGEATTRIBUTES.GetValue(imageAttributes);
		}

		/// <summary>
		/// Returns if a GDIPlus blur can be made for the supplied radius.
		/// This accounts for the "bug" I reported here: http://social.technet.microsoft.com/Forums/en/w8itprogeneral/thread/99ddbe9d-556d-475a-8bab-84e25aa13a2c
		/// </summary>
		/// <param name="radius"></param>
		/// <returns></returns>
		public static bool IsBlurPossible(int radius)
		{
			if (!isBlurEnabled)
			{
				return false;
			}
			else if (Environment.OSVersion.Version.Minor >= 2 && radius < 20)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Use the GDI+ blur effect on the bitmap
		/// </summary>
		/// <param name="destinationBitmap">Bitmap to apply the effect to</param>
		/// <param name="area">Rectangle to apply the blur effect to</param>
		/// <param name="radius">0-255</param>
		/// <param name="expandEdges">bool true if the edges are expanded with the radius</param>
		/// <returns>false if there is no GDI+ available or an exception occured</returns>
		public static bool ApplyBlur(Bitmap destinationBitmap, Rectangle area, int radius, bool expandEdges)
		{
			if (!IsBlurPossible(radius))
			{
				return false;
			}
			IntPtr hBlurParams = IntPtr.Zero;
			IntPtr hEffect = IntPtr.Zero;

			try
			{
				// Create a BlurParams struct and set the values
				BlurParams blurParams = new BlurParams();
				blurParams.Radius = radius;
				blurParams.ExpandEdges = expandEdges;

				// Allocate space in unmanaged memory
				hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
				// Copy the structure to the unmanaged memory
				Marshal.StructureToPtr(blurParams, hBlurParams, false);

				// Create the GDI+ BlurEffect, using the Guid
				int status = GdipCreateEffect(BlurEffectGuid, out hEffect);

				// Set the blurParams to the effect
				GdipSetEffectParameters(hEffect, hBlurParams, (uint)Marshal.SizeOf(blurParams));

				// Somewhere it said we can use destinationBitmap.GetHbitmap(), this doesn't work!!
				// Get the private nativeImage property from the Bitmap
				IntPtr hBitmap = GetNativeImage(destinationBitmap);

				// Create a RECT from the Rectangle
				RECT rec = new RECT(area);
				// Apply the effect to the bitmap in the specified area
				GdipBitmapApplyEffect(hBitmap, hEffect, ref rec, false, IntPtr.Zero, 0);

				// Everything worked, return true
				return true;
			}
			catch (Exception)
			{
				isBlurEnabled = false;
				// LOG.Error("Problem using GdipBitmapApplyEffect: ", ex);
				return false;
			}
			finally
			{
				try
				{
					if (hEffect != IntPtr.Zero)
					{
						// Delete the effect
						GdipDeleteEffect(hEffect);
					}
					if (hBlurParams != IntPtr.Zero)
					{
						// Free the memory
						Marshal.FreeHGlobal(hBlurParams);
					}
				}
				catch (Exception)
				{
					isBlurEnabled = false;
					// LOG.Error("Problem cleaning up ApplyBlur: ", ex);
				}
			}
		}

		/// <summary>
		/// Draw the image on the graphics with GDI+ blur effect 
		/// </summary>
		/// <returns>false if there is no GDI+ available or an exception occured</returns>
		public static bool DrawWithBlur(Graphics graphics, Bitmap image, Rectangle source, Matrix transform, ImageAttributes imageAttributes, int radius, bool expandEdges)
		{
			if (!IsBlurPossible(radius))
			{
				return false;
			}

			IntPtr hBlurParams = IntPtr.Zero;
			IntPtr hEffect = IntPtr.Zero;

			try
			{
				// Create a BlurParams struct and set the values
				BlurParams blurParams = new BlurParams();
				blurParams.Radius = radius;
				//blurParams.Padding = radius;
				blurParams.ExpandEdges = false;

				// Allocate space in unmanaged memory
				hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
				// Copy the structure to the unmanaged memory
				Marshal.StructureToPtr(blurParams, hBlurParams, true);

				// Create the GDI+ BlurEffect, using the Guid
				int status = GdipCreateEffect(BlurEffectGuid, out hEffect);

				// Set the blurParams to the effect
				GdipSetEffectParameters(hEffect, hBlurParams, (uint)Marshal.SizeOf(blurParams));

				// Somewhere it said we can use destinationBitmap.GetHbitmap(), this doesn't work!!
				// Get the private nativeImage property from the Bitmap
				IntPtr hBitmap = GetNativeImage(image);
				IntPtr hGraphics = GetNativeGraphics(graphics);
				IntPtr hMatrix = GetNativeMatrix(transform);
				IntPtr hAttributes = GetNativeImageAttributes(imageAttributes);

				// Create a RECT from the Rectangle
				RECTF sourceRECF = new RECTF(source);
				// Apply the effect to the bitmap in the specified area
				GdipDrawImageFX(hGraphics, hBitmap, ref sourceRECF, hMatrix, hEffect, hAttributes, GpUnit.UnitPixel);

				// Everything worked, return true
				return true;
			}
			catch (Exception)
			{
				isBlurEnabled = false;
				//LOG.Error("Problem using GdipDrawImageFX: ", ex);
				return false;
			}
			finally
			{
				try
				{
					if (hEffect != IntPtr.Zero)
					{
						// Delete the effect
						GdipDeleteEffect(hEffect);
					}
					if (hBlurParams != IntPtr.Zero)
					{
						// Free the memory
						Marshal.FreeHGlobal(hBlurParams);
					}
				}
				catch (Exception)
				{
					isBlurEnabled = false;
					//LOG.Error("Problem cleaning up DrawWithBlur: ", ex);
				}
			}
		}
	}
}
