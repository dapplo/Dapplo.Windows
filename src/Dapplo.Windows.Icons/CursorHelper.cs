// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
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
                // Ignore what the handle says. Calculate what it SHOULD be.
                // "CursorBaseSize" is the raw size (32, 48, 64) set in Accessibility settings.
                int baseSize = GetCursorBaseSize();
                uint dpi = NativeDpiMethods.GetDpiForSystem();
                int targetSize = (int)(baseSize * (dpi / 96.0f));

                // We ask Windows: "Load this specific resource again, but make it exactly [targetSize] pixels."
                IntPtr hRealCursor = IntPtr.Zero;
                bool isSharedHandle = false;

                var moduleName = iconInfoEx.ModuleName;
                if (iconInfoEx.ResourceId != 0 && !string.IsNullOrEmpty(moduleName))
                {
                    // It's a System Resource (like the standard Arrow)
                    IntPtr hModule = Kernel32Api.GetModuleHandle(moduleName);
                    hRealCursor = NativeCursorMethods.LoadImage(hModule, (IntPtr)iconInfoEx.ResourceId, 2, targetSize, targetSize, 0);
                }
                else if (!string.IsNullOrEmpty(moduleName))
                {
                    // It's a Custom File (like a downloaded theme)
                    hRealCursor = NativeCursorMethods.LoadImage(IntPtr.Zero, moduleName, 2, targetSize, targetSize, 0x0010); // LR_LOADFROMFILE
                }

                // If reload failed (dynamic cursor), fallback to the original 32px handle
                if (hRealCursor == IntPtr.Zero)
                {
                    hRealCursor = cursorInfo.CursorHandle;
                    isSharedHandle = true;
                }

                // This renders the (potentially huge) cursor into a transparent bitmap
                if (targetSize > 0)
                {
                    if (typeof(TBitmapType) == typeof(BitmapSource) || typeof(TBitmapType) == typeof(ImageSource))
                    {
                        using (iconInfoEx.BitmaskBitmapHandle)
                        using (iconInfoEx.ColorBitmapHandle)
                        {
                            // Pass the BitmapSource to the called
                            returnBitmap = Imaging.CreateBitmapSourceFromHIcon(hRealCursor, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()) as TBitmapType;
                        }
                    }
                    else if (typeof(TBitmapType) == typeof(Bitmap) || typeof(TBitmapType) == typeof(Image))
                    {
                        var bmp = new Bitmap(targetSize, targetSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            NativeIconMethods.DrawIconEx(g.GetHdc(), 0, 0, hRealCursor, targetSize, targetSize, 0, IntPtr.Zero, 0x0003); // DI_NORMAL
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
                if (targetSize > 0 && baseSize > 0) // Avoid divide by zero
                {
                    float scaleFactor = (float)targetSize / 32.0f; // Assuming 32 is standard base

                    // Refine: If we know the original was actually, say, 48, we should use that. 
                    // But 32 is the standard logical unit for Windows cursors.
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
}
#endif