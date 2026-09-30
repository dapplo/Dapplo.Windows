// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;

namespace Dapplo.Windows.Gdi32;

/// <summary>
///     GDI+ Helpers
/// </summary>
public static class GdiPlusApi
{
    private const string GDIPLUSDLL = "gdiplus.dll";

    private static readonly LogSource Log = new LogSource();
    private static readonly Guid BlurEffectGuid = new("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");

    // System.Drawing has no public API to get the native GDI+ handles, the names of the internal members differ per framework:
    // .NET Framework: fields nativeImage (in Image), nativeGraphics, nativeMatrix and nativeImageAttributes, all IntPtr
    // System.Drawing.Common 9+ (dotnet/winforms): the internal IPointer<T> interface for Image and Graphics,
    // the field _nativeImage, the properties NativeGraphics and NativeMatrix and the field _nativeImageAttributes, these are pointers.
    private static readonly Func<object, IntPtr> NativeImageAccessor = CreateNativeHandleAccessor(typeof(Bitmap), "nativeImage", "_nativeImage", "NativeImage");
    private static readonly Func<object, IntPtr> NativeGraphicsAccessor = CreateNativeHandleAccessor(typeof(Graphics), "nativeGraphics", "NativeGraphics", "_nativeGraphics");
    private static readonly Func<object, IntPtr> NativeMatrixAccessor = CreateNativeHandleAccessor(typeof(Matrix), "nativeMatrix", "NativeMatrix", "_nativeMatrix");
    private static readonly Func<object, IntPtr> NativeImageAttributesAccessor = CreateNativeHandleAccessor(typeof(ImageAttributes), "nativeImageAttributes", "_nativeImageAttributes", "NativeImageAttributes");

    private static bool _isBlurEnabled = WindowsVersion.IsWindowsVistaOrLater && AreNativeHandlesAvailable();

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

    [DllImport(GDIPLUSDLL, SetLastError = true, ExactSpelling = true)]
    private static extern GdiPlusStatus GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref NativeRect rectOfInterest, [MarshalAs(UnmanagedType.Bool)] bool useAuxData, IntPtr auxData, int auxDataSize);

    [DllImport(GDIPLUSDLL, SetLastError = true, ExactSpelling = true)]
    private static extern GdiPlusStatus GdipCreateEffect(Guid guid, out IntPtr effect);

    [DllImport(GDIPLUSDLL, SetLastError = true, ExactSpelling = true)]
    private static extern GdiPlusStatus GdipDeleteEffect(IntPtr effect);

    [DllImport(GDIPLUSDLL, SetLastError = true, ExactSpelling = true)]
    private static extern GdiPlusStatus GdipDrawImageFX(IntPtr graphics, IntPtr bitmap, ref NativeRectFloat source, IntPtr matrix, IntPtr effect, IntPtr imageAttributes, GpUnit srcUnit);

    [DllImport(GDIPLUSDLL, SetLastError = true, ExactSpelling = true)]
    private static extern GdiPlusStatus GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);

    /// <summary>
    ///     Checks if the native GDI+ handles of the System.Drawing objects, which are needed for the blur, can be retrieved in the current framework.
    /// </summary>
    /// <returns>true if the native handles for Bitmap, Graphics, Matrix and ImageAttributes are available</returns>
    public static bool AreNativeHandlesAvailable()
    {
        return NativeImageAccessor != null && NativeGraphicsAccessor != null && NativeMatrixAccessor != null && NativeImageAttributesAccessor != null;
    }

    /// <summary>
    ///     Create an accessor for the native GDI+ handle of the specified System.Drawing type.
    ///     First the internal IPointer&lt;T&gt; interface (System.Drawing.Common 9+) is tried, than fields and properties with the specified names in the type hierarchy.
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="memberNames">string array with the possible names of the member which holds the handle</param>
    /// <returns>Func which returns the handle, or null if not found</returns>
    private static Func<object, IntPtr> CreateNativeHandleAccessor(Type type, params string[] memberNames)
    {
        try
        {
            // System.Drawing.Common 9+ implements the internal Windows.Win32.Foundation.IPointer<T> interface, explicitly
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (!interfaceType.IsGenericType || interfaceType.Name != "IPointer`1")
                {
                    continue;
                }
                var pointerProperty = interfaceType.GetProperty("Pointer");
                if (pointerProperty == null)
                {
                    continue;
                }
                return instance => ToIntPtr(pointerProperty.GetValue(instance));
            }

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
            foreach (var memberName in memberNames)
            {
                for (var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
                {
                    var field = currentType.GetField(memberName, bindingFlags);
                    if (field != null && IsHandleType(field.FieldType))
                    {
                        return instance => ToIntPtr(field.GetValue(instance));
                    }
                    var property = currentType.GetProperty(memberName, bindingFlags);
                    if (property != null && property.GetIndexParameters().Length == 0 && property.GetGetMethod(true) != null && IsHandleType(property.PropertyType))
                    {
                        return instance => ToIntPtr(property.GetValue(instance));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn().WriteLine(ex, "Couldn't find the native GDI+ handle of {0}", type.FullName);
            return null;
        }
        Log.Warn().WriteLine("Couldn't find the native GDI+ handle of {0}, the GDI+ blur effect is not available.", type.FullName);
        return null;
    }

    /// <summary>
    ///     Check if the type can hold a native handle
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns>bool</returns>
    private static bool IsHandleType(Type type) => type == typeof(IntPtr) || type == typeof(UIntPtr) || type.IsPointer;

    /// <summary>
    ///     Convert the value of a handle field or property, which is an IntPtr or a boxed pointer, to an IntPtr
    /// </summary>
    /// <param name="value">object</param>
    /// <returns>IntPtr</returns>
    private static unsafe IntPtr ToIntPtr(object value)
    {
        return value switch
        {
            null => IntPtr.Zero,
            IntPtr intPtr => intPtr,
            UIntPtr uIntPtr => new IntPtr(uIntPtr.ToPointer()),
            Pointer pointer => new IntPtr(Pointer.Unbox(pointer)),
            _ => throw new NotSupportedException($"Can't convert {value.GetType()} to an IntPtr")
        };
    }

    /// <summary>
    ///     Get the native GDI+ handle of the object
    /// </summary>
    /// <param name="accessor">Func which retrieves the handle</param>
    /// <param name="instance">object or null</param>
    /// <returns>IntPtr</returns>
    private static IntPtr GetNativeHandle(Func<object, IntPtr> accessor, object instance)
    {
        if (instance == null)
        {
            return IntPtr.Zero;
        }
        if (accessor == null)
        {
            throw new NotSupportedException($"The native GDI+ handle of {instance.GetType().FullName} is not available");
        }
        return accessor(instance);
    }

    /// <summary>
    ///     Get the native GpGraphics handle from the graphics
    /// </summary>
    /// <param name="graphics"></param>
    /// <returns>IntPtr</returns>
    private static IntPtr GetNativeGraphics(Graphics graphics) => GetNativeHandle(NativeGraphicsAccessor, graphics);

    /// <summary>
    ///     Get the native GpImage handle from the bitmap
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>IntPtr</returns>
    private static IntPtr GetNativeImage(Bitmap bitmap) => GetNativeHandle(NativeImageAccessor, bitmap);

    /// <summary>
    ///     Get the native GpImageAttributes handle from the ImageAttributes
    /// </summary>
    /// <param name="imageAttributes">ImageAttributes</param>
    /// <returns>IntPtr</returns>
    private static IntPtr GetNativeImageAttributes(ImageAttributes imageAttributes) => GetNativeHandle(NativeImageAttributesAccessor, imageAttributes);

    /// <summary>
    ///     Get the native GpMatrix handle from the matrix
    /// </summary>
    /// <param name="matrix">Matrix</param>
    /// <returns>IntPtr</returns>
    private static IntPtr GetNativeMatrix(Matrix matrix) => GetNativeHandle(NativeMatrixAccessor, matrix);

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
        // Windows Vista and 7 can blur with every radius, from Windows 8 on only a radius of 20 or more works.
        // WindowsVersion is used as Environment.OSVersion reports 6.2 on Windows 8.1 and later for a not manifested .NET Framework application.
        if (!WindowsVersion.IsWindows8OrLater)
        {
            return true;
        }
        return radius >= 20;
    }
}