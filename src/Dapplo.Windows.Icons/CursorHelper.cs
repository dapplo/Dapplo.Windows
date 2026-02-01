// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Icons.Structs;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32.Structs;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Helper methods for using cursor information
/// </summary>
public static class CursorHelper
{
    /// <summary>
    /// Gets the base size of the mouse cursor, in pixels, as configured by the user in the system settings.
    /// </summary>
    /// <remarks>This method reads the 'CursorBaseSize' value from the Windows Registry under 'Control
    /// Panel\Cursors'. If the value is not found or an error occurs while accessing the registry, a default size of 32
    /// pixels is returned.</remarks>
    /// <returns>The size of the mouse cursor in pixels. Returns 32 if the value cannot be retrieved from the system settings.</returns>
    public static int GetCursorBaseSize()
    {
        // Reads the "Make mouse pointer bigger" slider value from Registry
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
            {
                if (key?.GetValue("CursorBaseSize") is int size)
                {
                    return size;
                }
            }
        }
        catch
        {
            // Empty by design
        }
        return 32; // Default
    }

    /// <summary>
    /// This method will capture the current Cursor by using User32 Code and will try to get the actual size of the cursor as set in Accessibility settings.
    /// </summary>
    /// <param name="returnBitmap">The cursor image represented by a Bitmap or BitmapSource</param>
    /// <param name="hotSpot">NativePoint with the hotspot information, this is the offset where the click on the screen happens</param>
    /// <returns>bool true if it worked</returns>
    public static bool TryGetCurrentCursor<TBitmapType>(out TBitmapType returnBitmap, out NativePoint hotSpot) where TBitmapType : class
    {
        var cursorInfo = CursorInfo.Create();
        returnBitmap = null;
        hotSpot = NativePoint.Empty;
        if (!NativeCursorMethods.GetCursorInfo(ref cursorInfo))
        {
            return false;
        }

        if (!cursorInfo.IsShowing)
        {
            return false;
        }

        var iconInfoEx = IconInfoEx.Create();

        if (NativeIconMethods.GetIconInfoEx(cursorInfo.CursorHandle, ref iconInfoEx))
        {
            try
            {
                int targetWidth = 0;
                int targetHeight = 0;
                bool forceSystemScaling = false;

                var moduleName = iconInfoEx.ModuleName;
                // Check if this is a System Cursor (Windows standard)
                if (IsSystemCursor(moduleName))
                {
                    // It is a standard cursor (Arrow, Hand, etc).
                    // We MUST apply the registry sizing, because Windows might be "faking" the handle size.
                    // "CursorBaseSize" is the raw size (32, 48, 64) set in Accessibility settings.
                    int baseSize = GetCursorBaseSize();
                    uint dpi = NativeDpiMethods.GetDpiForSystem();
                    // Ignore what the handle says. Calculate what it SHOULD be.
                    targetWidth = targetHeight = (int)(baseSize * (dpi / 96.0f));
                    forceSystemScaling = true;
                }
                else
                {
                    // It is an App-Specific cursor (Photoshop Brush, Game Crosshair). TRUST THE APP. Do not inflate it.
                    // We need to read the actual size of the bitmap handle we received.
                    var bmpInfo = new Gdi32.Structs.Bitmap();
                    IntPtr hBitmapToMeasure = iconInfoEx.ColorBitmapHandle.IsInvalid ? iconInfoEx.BitmaskBitmapHandle.DangerousGetHandle(): iconInfoEx.ColorBitmapHandle.DangerousGetHandle();
                    Gdi32Api.GetObject(hBitmapToMeasure, Marshal.SizeOf(typeof(Gdi32.Structs.Bitmap)), ref bmpInfo);

                    targetWidth = bmpInfo.Width;
                    targetHeight = bmpInfo.Height;


                    // If monochrome, height is doubled in the mask
                    if (iconInfoEx.ColorBitmapHandle.IsInvalid) targetHeight /= 2;
                }

                IntPtr hRealCursor = IntPtr.Zero;
                bool isSharedHandle = false;
                if (forceSystemScaling)
                {
                    if (iconInfoEx.ResourceId != 0 && !string.IsNullOrEmpty(moduleName))
                    {
                        // It's a System Resource (like the standard Arrow)
                        IntPtr hModule = Kernel32Api.GetModuleHandle(moduleName);
                        hRealCursor = NativeCursorMethods.LoadImage(hModule, (IntPtr)iconInfoEx.ResourceId, 2, targetWidth, targetHeight, 0);
                    }
                    else if (!string.IsNullOrEmpty(moduleName))
                    {
                        // It's a Custom File (like a downloaded theme)
                        hRealCursor = NativeCursorMethods.LoadImage(IntPtr.Zero, moduleName, 2, targetWidth, targetHeight, 0x0010); // LR_LOADFROMFILE
                    }
                }

                // If reload failed (dynamic cursor), fallback to the original 32px handle
                if (hRealCursor == IntPtr.Zero)
                {
                    hRealCursor = cursorInfo.CursorHandle;
                    isSharedHandle = true;
                }

                // This renders the (potentially huge) cursor into a transparent bitmap
                if (targetWidth > 0 && targetHeight > 0)
                {
                    if (typeof(TBitmapType) == typeof(BitmapSource) || typeof(TBitmapType) == typeof(ImageSource))
                    {
                        using (iconInfoEx.BitmaskBitmapHandle)
                        using (iconInfoEx.ColorBitmapHandle)
                        {
                            // Pass the BitmapSource to the caller
                            returnBitmap = Imaging.CreateBitmapSourceFromHIcon(hRealCursor, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()) as TBitmapType;
                        }
                    }
                    else if (typeof(TBitmapType) == typeof(System.Drawing.Bitmap) || typeof(TBitmapType) == typeof(Image))
                    {
                        var bmp = new System.Drawing.Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            NativeIconMethods.DrawIconEx(g.GetHdc(), 0, 0, hRealCursor, targetWidth, targetHeight, 0, IntPtr.Zero, 0x0003); // DI_NORMAL
                            g.ReleaseHdc();
                        }
                        // Pass the bitmap to the called
                        returnBitmap = bmp as TBitmapType;
                    } else
                    {
                        throw new NotSupportedException(typeof(TBitmapType).Name);
                    }
                }

                // The original hotspot is for the 32px version. Scale it up.
                if (forceSystemScaling && targetWidth > 0)
                {
                    float scaleFactor = targetWidth / 32.0f; // Assuming 32 is standard base
                    hotSpot = new NativePoint((int)(iconInfoEx.Hotspot.X * scaleFactor), (int)(iconInfoEx.Hotspot.Y * scaleFactor));
                }
                else
                {
                    hotSpot = iconInfoEx.Hotspot;
                }

                // Cleanup cursor, but only if it's not a shared handle (in the other case. we clean up elsewhere)
                if (!isSharedHandle && hRealCursor != IntPtr.Zero) NativeCursorMethods.DestroyCursor(hRealCursor);
            }
            finally
            {
                // Cleanup icon
                iconInfoEx.BitmaskBitmapHandle.Dispose();
                iconInfoEx.ColorBitmapHandle.Dispose();
            }
        } else {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified module name corresponds to a known system cursor provider.
    /// </summary>
    /// <remarks>This method checks for common Windows system cursor sources, such as 'user32.dll', the
    /// Windows cursors directory, and 'main.cpl' (mouse settings).</remarks>
    /// <param name="moduleName">The name of the module to check. This parameter cannot be null or empty.</param>
    /// <returns>true if the module name is associated with a standard Windows system cursor provider; otherwise, false.</returns>
    private static bool IsSystemCursor(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return false;
        }

        string lower = moduleName.ToLowerInvariant();

        // Standard Windows cursors live here
        if (lower.Contains("user32.dll"))
        {
            return true;
        }

        if (lower.Contains(@"\windows\cursors\"))
        {
            return true;
        }

        // Sometimes they come from main.cpl (mouse settings)
        return lower.Contains("main.cpl");
    }
}
#endif