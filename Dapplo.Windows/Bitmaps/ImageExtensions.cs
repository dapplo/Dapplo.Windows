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
using System.Drawing.Imaging;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Native;

#endregion

namespace Dapplo.Windows.Bitmaps
{
	/// <summary>
	///     Extensions to modify or create bitmaps
	/// </summary>
	public static class ImageExtensions
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Crops the image to the specified rectangle
		/// </summary>
		/// <param name="image">Image to crop</param>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates, will be "intersected" to the bitmap</param>
		/// <returns>Image</returns>
		public static Image Crop(this Image image, ref Rectangle cropRectangle)
		{
			if (image is Bitmap && (image.Width * image.Height > 0))
			{
				cropRectangle.Intersect(new Rectangle(0, 0, image.Width, image.Height));
				if (cropRectangle.Width != 0 || cropRectangle.Height != 0)
				{
					return CloneArea(image, cropRectangle, PixelFormat.DontCare);
				}
			}
			Log.Warn().WriteLine("Can't crop a null/zero size image!");
			return null;
		}

		/// <summary>
		/// Checks if we support the pixel format
		/// </summary>
		/// <param name="pixelformat">PixelFormat to check</param>
		/// <returns>bool if we support it</returns>
		public static bool SupportsPixelFormat(this PixelFormat pixelformat)
		{
			return pixelformat.Equals(PixelFormat.Format32bppArgb) ||
					pixelformat.Equals(PixelFormat.Format32bppPArgb) ||
					pixelformat.Equals(PixelFormat.Format32bppRgb) ||
					pixelformat.Equals(PixelFormat.Format24bppRgb);
		}

		/// <summary>
		/// Clone an image, taking some rules into account:
		/// 1) When sourceRect is the whole bitmap there is a GDI+ bug in Clone
		///		Clone will than return the same PixelFormat as the source
		///		a quick workaround is using new Bitmap which uses a default of Format32bppArgb
		///	2) When going from a transparent to a non transparent bitmap, we draw the background white!
		/// </summary>
		/// <param name="sourceImage">Source bitmap to clone</param>
		/// <param name="sourceRect">Rectangle to copy from the source, use Rectangle.Empty for all</param>
		/// <param name="targetFormat">Target Format, use PixelFormat.DontCare if you want the original (or a default if the source PixelFormat is not supported)</param>
		/// <returns>Bitmap</returns>
		public static Bitmap CloneArea(this Image sourceImage, Rectangle sourceRect, PixelFormat targetFormat)
		{
			Bitmap newImage;
			Rectangle bitmapRect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);

			// Make sure the source is not Rectangle.Empty
			if (Rectangle.Empty.Equals(sourceRect))
			{
				sourceRect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
			}
			else
			{
				sourceRect.Intersect(bitmapRect);
			}

			// If no pixelformat is supplied 
			if (PixelFormat.DontCare == targetFormat || PixelFormat.Undefined == targetFormat)
			{
				if (SupportsPixelFormat(sourceImage.PixelFormat))
				{
					targetFormat = sourceImage.PixelFormat;
				}
				else if (Image.IsAlphaPixelFormat(sourceImage.PixelFormat))
				{
					targetFormat = PixelFormat.Format32bppArgb;
				}
				else
				{
					targetFormat = PixelFormat.Format24bppRgb;
				}
			}

			// check the target format
			if (!SupportsPixelFormat(targetFormat))
			{
				targetFormat = Image.IsAlphaPixelFormat(targetFormat) ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
			}

			bool destinationIsTransparent = Image.IsAlphaPixelFormat(targetFormat);
			bool sourceIsTransparent = Image.IsAlphaPixelFormat(sourceImage.PixelFormat);
			bool fromTransparentToNon = !destinationIsTransparent && sourceIsTransparent;
			bool isBitmap = sourceImage is Bitmap;
			bool isAreaEqual = sourceRect.Equals(bitmapRect);
			if (isAreaEqual || fromTransparentToNon || !isBitmap)
			{
				// Rule 1: if the areas are equal, always copy ourselves
				newImage = new Bitmap(bitmapRect.Width, bitmapRect.Height, targetFormat);
				// Make sure both images have the same resolution
				newImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);

				using (Graphics graphics = Graphics.FromImage(newImage))
				{
					if (fromTransparentToNon)
					{
						// Rule 2: Make sure the background color is white
						graphics.Clear(Color.White);
					}
					// decide fastest copy method
					if (isAreaEqual)
					{
						graphics.DrawImageUnscaled(sourceImage, 0, 0);
					}
					else
					{
						graphics.DrawImage(sourceImage, 0, 0, sourceRect, GraphicsUnit.Pixel);
					}
				}
			}
			else
			{
				// Let GDI+ decide how to convert, need to test what is quicker...
				newImage = (sourceImage as Bitmap).Clone(sourceRect, targetFormat);
				// Make sure both images have the same resolution
				newImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
			}
			// Clone property items (EXIF information etc)
			foreach (var propertyItem in sourceImage.PropertyItems)
			{
				try
				{
					newImage.SetPropertyItem(propertyItem);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex, "Problem cloning a propertyItem.");
				}
			}
			return newImage;
		}

		/// <summary>
		///     Return an Image representing the Window!
		///     As GDI+ draws it, it will be without Aero borders!
		/// </summary>
		public static Image PrintWindow(this NativeWindow nativeWindow)
		{
			var bounds = nativeWindow.GetBounds();
			// Start the capture
			Exception exceptionOccured = null;
			Image returnImage;
			using (var region = nativeWindow.GetRegion())
			{
				var pixelFormat = PixelFormat.Format24bppRgb;
				// Only use 32 bpp ARGB when the window has a region
				if (region != null)
				{
					pixelFormat = PixelFormat.Format32bppArgb;
				}
				returnImage = new Bitmap(bounds.Width, bounds.Height, pixelFormat);
				using (var graphics = Graphics.FromImage(returnImage))
				{
					using (var safeDeviceContext = graphics.GetSafeDeviceContext())
					{
						var printSucceeded = User32.PrintWindow(nativeWindow.Handle, safeDeviceContext.DangerousGetHandle(), 0x0);
						if (!printSucceeded)
						{
							// something went wrong, most likely a "0x80004005" (Acess Denied) when using UAC
							exceptionOccured = User32.CreateWin32Exception("PrintWindow");
						}
					}

					// Apply the region "transparency"
					if (region != null && !region.IsEmpty(graphics))
					{
						graphics.ExcludeClip(region);
						graphics.Clear(Color.Transparent);
					}

					graphics.Flush();
				}
			}

			// Return null if error
			if (exceptionOccured != null)
			{
				Log.Error().WriteLine("Error calling print window: {0}", exceptionOccured.Message);
				returnImage.Dispose();
				return null;
			}
			if (!nativeWindow.HasParent && nativeWindow.IsMaximized())
			{
				Log.Debug().WriteLine("Correcting for maximalization");
				Size borderSize = nativeWindow.GetBorderSize();
				var borderRectangle = new Rectangle(borderSize.Width, borderSize.Height, bounds.Width - 2 * borderSize.Width, bounds.Height - 2 * borderSize.Height);
				var tmpImage = returnImage;
				returnImage = returnImage.Crop(ref borderRectangle);
				tmpImage.Dispose();
			}
			return returnImage;
		}

		/// <summary>
		/// A generic way to create an empty image
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		/// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
		/// <param name="horizontalResolution"></param>
		/// <param name="verticalResolution"></param>
		/// <returns>Bitmap</returns>
		public static Bitmap CreateEmpty(int width, int height, PixelFormat format, Color backgroundColor, float horizontalResolution, float verticalResolution)
		{
			// Create a new "clean" image
			Bitmap newImage = new Bitmap(width, height, format);
			newImage.SetResolution(horizontalResolution, verticalResolution);
			if (format != PixelFormat.Format8bppIndexed)
			{
				using (Graphics graphics = Graphics.FromImage(newImage))
				{
					// Make sure the background color is what we want (transparent or white, depending on the pixel format)
					if (!Color.Empty.Equals(backgroundColor))
					{
						graphics.Clear(backgroundColor);
					}
					else if (Image.IsAlphaPixelFormat(format))
					{
						graphics.Clear(Color.Transparent);
					}
					else
					{
						graphics.Clear(Color.White);
					}
				}
			}
			return newImage;
		}

	}
}