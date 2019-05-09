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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;

#endregion

namespace Dapplo.Windows.Gdi32
{
    /// <summary>
    ///     GDIplus Helpers
    /// </summary>
    public static class GdiPlusApi
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");

        // Constant "FieldInfo" for getting the nativeImage from the Bitmap
        private static readonly FieldInfo FieldInfoNativeImage = typeof(Bitmap).GetField("nativeImage", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

        // Constant "FieldInfo" for getting the NativeGraphics from the Graphics
        private static readonly FieldInfo FieldInfoNativeGraphics = typeof(Graphics).GetField("nativeGraphics", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

        // Constant "FieldInfo" for getting the nativeMatrix from the Matrix
        private static readonly FieldInfo FieldInfoNativeMatrix = typeof(Matrix).GetField("nativeMatrix", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

        // Constant "FieldInfo" for getting the nativeImageAttributes from the ImageAttributes
        private static readonly FieldInfo FieldInfoNativeImageattributes = typeof(ImageAttributes).GetField("nativeImageAttributes", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _isBlurEnabled = Environment.OSVersion.Version.Major >= 6;

        /// <summary>
        ///     Use the GDI+ blur effect on the bitmap
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
            var hBlurParams = IntPtr.Zero;
            var hEffect = IntPtr.Zero;

            try
            {
                // Create a BlurParams struct and set the values
                var blurParams = BlurParams.Create(radius, expandEdges);

                // Allocate space in unmanaged memory
                hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
                // Copy the structure to the unmanaged memory
                Marshal.StructureToPtr(blurParams, hBlurParams, false);

                // Create the GDI+ BlurEffect, using the Guid
                var status = GdipCreateEffect(BlurEffectGuid, out hEffect);
                if (status != GdiPlusStatus.Ok)
                {
                    Log.Error().WriteLine("Couldn't create effect {0}: {1}", BlurEffectGuid, status);
                    return false;
                }

                // Set the blurParams to the effect
                status = GdipSetEffectParameters(hEffect, hBlurParams, (uint) Marshal.SizeOf(blurParams));
                if (status != GdiPlusStatus.Ok)
                {
                    Log.Error().WriteLine("Couldn't set effect parameter: {0}", status);
                    return false;
                }

                // Somewhere it said we can use destinationBitmap.GetHbitmap(), this doesn't work!!
                // Get the private nativeImage property from the Bitmap
                var hBitmap = GetNativeImage(destinationBitmap);

                // Create a RECT from the Rectangle
                NativeRect rec = area;
                // Apply the effect to the bitmap in the specified area
                status = GdipBitmapApplyEffect(hBitmap, hEffect, ref rec, false, IntPtr.Zero, 0);
                if (status == GdiPlusStatus.Ok)
                {
                    // Everything worked, return true
                    return true;
                }

                Log.Error().WriteLine("Couldn't apply effect: {0}", status);
                return false;
            }
            catch (Exception ex)
            {
                _isBlurEnabled = false;
                Log.Error().WriteLine(ex, "Problem using GdipBitmapApplyEffect: ");
                return false;
            }
            finally
            {
                try
                {
                    if (hEffect != IntPtr.Zero)
                    {
                        // Delete the effect
                        var status = GdipDeleteEffect(hEffect);
                        if (status != GdiPlusStatus.Ok)
                        {
                            Log.Error().WriteLine("Couldn't delete effect: {0}", status);
                        }
                    }
                    if (hBlurParams != IntPtr.Zero)
                    {
                        // Free the memory
                        Marshal.FreeHGlobal(hBlurParams);
                    }
                }
                catch (Exception ex)
                {
                    _isBlurEnabled = false;
                    Log.Error().WriteLine(ex, "Problem cleaning up ApplyBlur: ");
                }
            }
        }

        /// <summary>
        ///     Draw the image on the graphics with GDI+ blur effect
        /// </summary>
        /// <returns>false if there is no GDI+ available or an exception occured</returns>
        public static bool DrawWithBlur(Graphics graphics, Bitmap image, Rectangle source, Matrix transform, ImageAttributes imageAttributes, int radius, bool expandEdges)
        {
            if (!IsBlurPossible(radius))
            {
                return false;
            }

            var hBlurParams = IntPtr.Zero;
            var hEffect = IntPtr.Zero;

            try
            {
                // Create a BlurParams struct and set the values
                var blurParams = BlurParams.Create(radius, expandEdges);

                // Allocate space in unmanaged memory
                hBlurParams = Marshal.AllocHGlobal(Marshal.SizeOf(blurParams));
                // Copy the structure to the unmanaged memory
                Marshal.StructureToPtr(blurParams, hBlurParams, true);

                // Create the GDI+ BlurEffect, using the Guid
                var status = GdipCreateEffect(BlurEffectGuid, out hEffect);
                if (status != GdiPlusStatus.Ok)
                {
                    Log.Error().WriteLine("Couldn't create effect {0}: {1}", BlurEffectGuid, status);
                    return false;
                }

                // Set the blurParams to the effect
                status = GdipSetEffectParameters(hEffect, hBlurParams, (uint) Marshal.SizeOf(blurParams));
                if (status != GdiPlusStatus.Ok)
                {
                    Log.Error().WriteLine("Couldn't apply parameters: {0}", status);
                    return false;
                }

                // Somewhere it said we can use destinationBitmap.GetHbitmap(), this doesn't work!!
                // Get the private nativeImage property from the Bitmap
                var hBitmap = GetNativeImage(image);
                var hGraphics = GetNativeGraphics(graphics);
                var hMatrix = GetNativeMatrix(transform);
                var hAttributes = GetNativeImageAttributes(imageAttributes);

                // Create a RECT from the Rectangle
                NativeRectFloat sourceRectangleF = source;
                // Apply the effect to the bitmap in the specified area
                status = GdipDrawImageFX(hGraphics, hBitmap, ref sourceRectangleF, hMatrix, hEffect, hAttributes, GpUnit.UnitPixel);
                if (status == GdiPlusStatus.Ok)
                {
                    // Everything worked, return true
                    return true;
                }

                Log.Error().WriteLine("Couldn't draw image: {0}", status);
                return false;
            }
            catch (Exception ex)
            {
                _isBlurEnabled = false;
                Log.Error().WriteLine(ex, "Problem using GdipDrawImageFX: ");
                return false;
            }
            finally
            {
                try
                {
                    if (hEffect != IntPtr.Zero)
                    {
                        // Delete the effect
                        var status = GdipDeleteEffect(hEffect);
                        if (status != GdiPlusStatus.Ok)
                        {
                            Log.Error().WriteLine("Couldn't delete effect: {0}", status);
                        }
                    }
                    if (hBlurParams != IntPtr.Zero)
                    {
                        // Free the memory
                        Marshal.FreeHGlobal(hBlurParams);
                    }
                }
                catch (Exception ex)
                {
                    _isBlurEnabled = false;
                    Log.Error().WriteLine(ex, "Problem cleaning up DrawWithBlur: ");
                }
            }
        }

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
        private static extern GdiPlusStatus GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref NativeRect rectOfInterest, [MarshalAs(UnmanagedType.Bool)] bool useAuxData, IntPtr auxData, int auxDataSize);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
        private static extern GdiPlusStatus GdipCreateEffect(Guid guid, out IntPtr effect);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
        private static extern GdiPlusStatus GdipDeleteEffect(IntPtr effect);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
        private static extern GdiPlusStatus GdipDrawImageFX(IntPtr graphics, IntPtr bitmap, ref NativeRectFloat source, IntPtr matrix, IntPtr effect, IntPtr imageAttributes, GpUnit srcUnit);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true)]
        private static extern GdiPlusStatus GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);

        /// <summary>
        ///     Get the NativeGraphics field from the graphics
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns>IntPtr</returns>
        private static IntPtr GetNativeGraphics(Graphics graphics)
        {
            if (graphics == null)
            {
                return IntPtr.Zero;
            }
            return (IntPtr) FieldInfoNativeGraphics.GetValue(graphics);
        }

        /// <summary>
        ///     Get the nativeImage field from the bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>IntPtr</returns>
        private static IntPtr GetNativeImage(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return IntPtr.Zero;
            }
            return (IntPtr) FieldInfoNativeImage.GetValue(bitmap);
        }

        /// <summary>
        ///     Get the nativeImageAttributes field from the ImageAttributes
        /// </summary>
        /// <param name="imageAttributes">ImageAttributes</param>
        /// <returns>IntPtr</returns>
        private static IntPtr GetNativeImageAttributes(ImageAttributes imageAttributes)
        {
            if (imageAttributes == null)
            {
                return IntPtr.Zero;
            }
            return (IntPtr) FieldInfoNativeImageattributes.GetValue(imageAttributes);
        }

        /// <summary>
        ///     Get the nativeMatrix field from the matrix
        /// </summary>
        /// <param name="matrix">Matrix</param>
        /// <returns>IntPtr</returns>
        private static IntPtr GetNativeMatrix(Matrix matrix)
        {
            if (matrix == null)
            {
                return IntPtr.Zero;
            }
            return (IntPtr) FieldInfoNativeMatrix.GetValue(matrix);
        }

        /// <summary>
        ///     Returns if a GDIPlus blur can be made for the supplied radius.
        ///     This accounts for the "bug" I reported here:
        ///     http://social.technet.microsoft.com/Forums/en/w8itprogeneral/thread/99ddbe9d-556d-475a-8bab-84e25aa13a2c
        /// </summary>
        /// <param name="radius">int</param>
        /// <returns>false if blur is not possible</returns>
        public static bool IsBlurPossible(int radius)
        {
            if (!_isBlurEnabled)
            {
                return false;
            }
            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor < 2)
            {
                return true;
            }
            return Environment.OSVersion.Version.Major > 6 && radius >= 20;
        }
    }
}