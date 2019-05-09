#region Copyright (C) 2016-2019 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2019 Dapplo
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
#endregion

#region using

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common;
#if !NETSTANDARD2_0
using System.Windows.Media;
#endif
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Dapplo.Windows.DesktopWindowsManager.Structs;
using Microsoft.Win32;

#endregion

namespace Dapplo.Windows.DesktopWindowsManager
{
    /// <summary>
    ///     Dwm Utils class
    /// </summary>
    public static class Dwm
    {
        private const uint DwmEcDisableComposition = 0;
        private const uint DwmEcEnableComposition = 1;

        // Key to ColorizationColor for DWM
        private const string ColorizationColorKey = @"SOFTWARE\Microsoft\Windows\DWM";
        private const string DwmApiDll = "dwmapi.dll";

#if !NETSTANDARD2_0
        /// <summary>
        ///     Return the AERO Color
        /// </summary>
        public static Color ColorizationColor
        {
            get
            {
                var color = ColorizationSystemDrawingColor;
                return Color.FromArgb(color.A, color.R, color.G, color.B);
            }
        }
#endif

        /// <summary>
        ///     Return the Aero Color
        /// </summary>
        public static System.Drawing.Color ColorizationSystemDrawingColor
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(ColorizationColorKey, false))
                {
                    var dwordValue = key?.GetValue("ColorizationColor");
                    if (dwordValue != null)
                    {
                        return System.Drawing.Color.FromArgb((int) dwordValue);
                    }
                }
                return System.Drawing.Color.White;
            }
        }

        /// <summary>
        ///     Helper method for an easy DWM check
        /// </summary>
        /// <returns>bool true if DWM is available AND active</returns>
        public static bool IsDwmEnabled
        {
            get
            {
                // According to: http://technet.microsoft.com/en-us/subscriptions/aa969538%28v=vs.85%29.aspx
                // And: http://msdn.microsoft.com/en-us/library/windows/desktop/aa969510%28v=vs.85%29.aspx
                // DMW is always enabled on Windows 8! So return true and save a check! ;-)
                if (WindowsVersion.IsWindows8X)
                {
                    return true;
                }
                if (WindowsVersion.IsWindowsBeforeVista)
                {
                    return false;
                }

                DwmIsCompositionEnabled(out var dwmEnabled);
                return dwmEnabled;
            }
        }

        /// <summary>
        ///     Disable composition
        /// </summary>
        public static bool DisableComposition() => DwmEnableComposition(DwmEcDisableComposition).Succeeded();

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969508(v=vs.85).aspx">
        ///         DwmEnableBlurBehindWindow
        ///         function
        ///     </a>
        ///     Enables the blur effect on a specified window.
        /// </summary>
        /// <param name="hwnd">The handle to the window on which the blur behind data is applied.</param>
        /// <param name="blurBehind">DwmBlurBehind</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmEnableBlurBehindWindow(IntPtr hwnd, ref DwmBlurBehind blurBehind);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969510(v=vs.85).aspx">
        ///         DwmEnableComposition
        ///         function
        ///     </a>
        ///     As of Windows 8, calling this function with DWM_EC_DISABLECOMPOSITION has no effect. However, the function will
        ///     still return a success code.
        /// </summary>
        /// <param name="uCompositionAction">
        ///     DWM_EC_ENABLECOMPOSITION to enable DWM composition; DWM_EC_DISABLECOMPOSITION to
        ///     disable composition.
        /// </param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmEnableComposition(uint uCompositionAction);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969515(v=vs.85).aspx">
        ///         DwmGetWindowAttribute
        ///         function
        ///     </a>
        ///     Retrieves the current value of a specified attribute applied to a window.
        ///     TODO: Currently only DWMWA_EXTENDED_FRAME_BOUNDS is supported, due to the type of lpRect
        /// </summary>
        /// <param name="hwnd">The handle to the window from which the attribute data is retrieved.</param>
        /// <param name="dwAttribute">The attribute to retrieve, specified as a DwmWindowAttributes value.</param>
        /// <param name="lpRect">
        ///     A pointer to a value that, when this function returns successfully, receives the current value of
        ///     the attribute. The type of the retrieved value depends on the value of the dwAttribute parameter.
        /// </param>
        /// <param name="size"></param>
        /// <returns>
        ///     The size of the DWMWINDOWATTRIBUTE value being retrieved. The size is dependent on the type of the pvAttribute
        ///     parameter.
        /// </returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttributes dwAttribute, out NativeRect lpRect, int size);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969518(v=vs.85).aspx">
        ///         DwmIsCompositionEnabled
        ///         function
        ///     </a>
        ///     Obtains a value that indicates whether Desktop Window Manager (DWM) composition is enabled.
        ///     Applications on machines running Windows 7 or earlier can listen for composition state changes by handling the
        ///     WM_DWMCOMPOSITIONCHANGED notification.
        ///     Note: As of Windows 8, DWM composition is always enabled.
        ///     If an app declares Windows 8 compatibility in their manifest, this function will receive a value of TRUE through
        ///     pfEnabled.
        ///     If no such manifest entry is found, Windows 8 compatibility is not assumed and this function receives a value of
        ///     FALSE through pfEnabled.
        ///     This is done so that older programs that interpret a value of TRUE to imply that high contrast mode is off can
        ///     continue to make the correct decisions about rendering their images.
        ///     (Note that this is a bad practice—you should use the SystemParametersInfo function with the SPI_GETHIGHCONTRAST
        ///     flag to determine the state of high contrast mode.)
        /// </summary>
        /// <param name="pfEnabled">out bool to get the current state</param>
        /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        private static extern HResult DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled);

        /// <summary>
        ///     Activate Aero Peek
        /// </summary>
        /// <param name="active">uint</param>
        /// <param name="handle"></param>
        /// <param name="onTopHandle">IntPtr</param>
        /// <param name="unknown">uint</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, EntryPoint = "#113", SetLastError = true)]
        internal static extern HResult DwmpActivateLivePreview(uint active, IntPtr handle, IntPtr onTopHandle, uint unknown);

        /// <summary>
        ///     Activate Windows + Tab effect
        /// </summary>
        /// <returns>bool if it worked</returns>
        [DllImport(DwmApiDll, EntryPoint = "#105", SetLastError = true)]
        public static extern bool DwmpStartOrStopFlip3D();

        /// <summary>
        /// Retrieves the source size of the Desktop Window Manager (DWM) thumbnail.
        /// </summary>
        /// <param name="hThumbnail">A handle to the thumbnail to retrieve the source window size from.</param>
        /// <param name="size">a NativeSize structure that, when this function returns successfully, receives the size of the source thumbnail.</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmQueryThumbnailSourceSize(IntPtr hThumbnail, out NativeSize size);

        /// <summary>
        /// Creates a Desktop Window Manager (DWM) thumbnail relationship between the destination and source windows.
        /// </summary>
        /// <param name="hwndDestination">The handle to the window that will use the DWM thumbnail. Setting the destination window handle to anything other than a top-level window type will result in a return value of E_INVALIDARG.</param>
        /// <param name="hwndSource">The handle to the window to use as the thumbnail source. Setting the source window handle to anything other than a top-level window type will result in a return value of E_INVALIDARG.</param>
        /// <param name="phThumbnailId">A pointer to a handle that, when this function returns successfully, represents the registration of the DWM thumbnail.</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmRegisterThumbnail(IntPtr hwndDestination, IntPtr hwndSource, out IntPtr phThumbnailId);

        /// <summary>
        ///     Sets a static, iconic bitmap to display a live preview (also known as a Peek preview) of a window or tab. The
        ///     taskbar can use this bitmap to show a full-sized preview of a window or tab.
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd389410(v=vs.85).aspx">
        ///         DwmSetIconicLivePreviewBitmap
        ///         function
        ///     </a>
        /// </summary>
        /// <param name="hwnd">IntPtr for the window Handle</param>
        /// <param name="hbitmap">IntPtr for the bitmap</param>
        /// <param name="ptClient">
        ///     The offset of a tab window's client region (the content area inside the client window frame)
        ///     from the host window's frame. This offset enables the tab window's contents to be drawn correctly in a live preview
        ///     when it is drawn without its frame.
        /// </param>
        /// <param name="setIconicLivePreviewFlags">The display options for the live preview.</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        internal static extern HResult DwmSetIconicLivePreviewBitmap(IntPtr hwnd, IntPtr hbitmap, ref NativePoint ptClient, DwmSetIconicLivePreviewFlags setIconicLivePreviewFlags);

        /// <summary>
        ///     Sets the value of non-client rendering attributes for a window.
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969524(v=vs.85).aspx">
        ///         DwmSetWindowAttribute
        ///         function
        ///     </a>
        /// </summary>
        /// <param name="hwnd">IntPtr with the handle to the window that will receive the attributes.</param>
        /// <param name="dwAttributeToSet">
        ///     A single DWMWINDOWATTRIBUTE flag to apply to the window. This parameter specifies the
        ///     attribute and the pvAttribute parameter points to the value of that attribute.
        /// </param>
        /// <param name="pvAttributeValue">
        ///     A pointer to the value of the attribute specified in the dwAttribute parameter.
        ///     Different DWMWINDOWATTRIBUTE flags require different value types.
        /// </param>
        /// <param name="cbAttribute">The size, in bytes, of the value type pointed to by the pvAttribute parameter.</param>
        /// <returns></returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttributes dwAttributeToSet, IntPtr pvAttributeValue, uint cbAttribute);

        /// <summary>
        /// Removes a Desktop Window Manager (DWM) thumbnail relationship created by the DwmRegisterThumbnail function.
        /// </summary>
        /// <param name="hThumbnailId">The handle to the thumbnail relationship to be removed. Null or non-existent handles will result in a return value of E_INVALIDARG.</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmUnregisterThumbnail(IntPtr hThumbnailId);

        /// <summary>
        /// Updates the properties for a Desktop Window Manager (DWM) thumbnail.
        /// </summary>
        /// <param name="hThumbnailId">IntPtr The handle to the DWM thumbnail to be updated. Null or invalid thumbnails, as well as thumbnails owned by other processes will result in a return value of E_INVALIDARG.</param>
        /// <param name="props">A pointer to a DwmThumbnailProperties structure that contains the new thumbnail properties.</param>
        /// <returns>HResult</returns>
        [DllImport(DwmApiDll, SetLastError = true)]
        public static extern HResult DwmUpdateThumbnailProperties(IntPtr hThumbnailId, ref DwmThumbnailProperties props);

        /// <summary>
        ///     Enable DWM composition
        /// </summary>
        public static bool EnableComposition() => DwmEnableComposition(DwmEcEnableComposition).Succeeded();

        /// <summary>
        ///     Helper method to get the window size for DWM Windows
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="rectangle">out RECT</param>
        /// <returns>bool true if it worked</returns>
        public static bool GetExtendedFrameBounds(IntPtr hWnd, out NativeRect rectangle)
        {
            var result = DwmGetWindowAttribute(hWnd, DwmWindowAttributes.ExtendedFrameBounds, out rectangle, NativeRect.SizeOf);
            if (result.Succeeded())
            {
                return true;
            }
            rectangle = NativeRect.Empty;
            return false;
        }

        /// <summary>
        /// Retrieves the shared surface of the specified hWnd, maybe https://github.com/notr1ch/DWMCapture can help on the usage.
        /// http://undoc.airesoft.co.uk/user32.dll/DwmGetDxSharedSurface.php?
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="adapterLuid"></param>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="pD3DFormat"></param>
        /// <param name="pSharedHandle"></param>
        /// <param name="unknown"></param>
        /// <returns></returns>
        [PreserveSig]
        [DllImport(DwmApiDll, EntryPoint = "#100", SetLastError = true)]
        public static extern int GetSharedSurface(IntPtr hWnd, long adapterLuid, uint one, uint two, [In] [Out] ref uint pD3DFormat, [Out] out IntPtr pSharedHandle, ulong unknown);

        /// <summary>
        /// maybe https://github.com/notr1ch/DWMCapture can help on the usage.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="three"></param>
        /// <param name="hMonitor"></param>
        /// <param name="unknown"></param>
        /// <returns></returns>
        [PreserveSig]
        [DllImport(DwmApiDll, EntryPoint = "#101", SetLastError = true)]
        public static extern int UpdateWindowShared(IntPtr hWnd, int one, int two, int three, IntPtr hMonitor, IntPtr unknown);
    }
}