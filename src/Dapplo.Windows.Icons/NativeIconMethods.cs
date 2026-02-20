// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Icons.Enums;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Icons.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.SafeHandles;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Win32 native methods for Icon
/// </summary>
public static class NativeIconMethods
{
    /// <summary>
    ///     The following is used for Icon handling, and copies a hicon to a new
    /// </summary>
    /// <param name="hIcon">SafeIconHandle</param>
    /// <returns>SafeIconHandle</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern SafeIconHandle CopyIcon(SafeIconHandle hIcon);

    /// <summary>
    ///     The following is used for Icon handling, and copies a hicon to a new
    /// </summary>
    /// <param name="hIcon">SafeIconHandle</param>
    /// <returns>IntPtr</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern SafeIconHandle CopyIcon(IntPtr hIcon);

    /// <summary>
    /// Destroys an icon and frees any memory the icon occupied.
    /// </summary>
    /// <remarks>It is only necessary to call DestroyIcon for icons and cursors created with the following functions: CreateIconFromResourceEx (if called without the LR_SHARED flag), CreateIconIndirect, and CopyIcon. Do not use this function to destroy a shared icon. A shared icon is valid as long as the module from which it was loaded remains in memory. The following functions obtain a shared icon.</remarks>
    /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
    /// <returns>bool true if the destroy succeeded</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Retrieves information about the specified icon or cursor.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648070(v=vs.85).aspx">GetIconInfo function</a>
    /// This also describes to get more info on standard icons and cursors
    /// </summary>
    /// <param name="iconHandle">A handle to the icon or cursor.</param>
    /// <param name="iconInfo">A pointer to an ICONINFO structure. The function fills in the structure's members.</param>
    /// <returns>bool true if the function succeeds, the return value is in the IconInfo structure.</returns>
    [DllImport(User32Api.User32, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetIconInfo(SafeIconHandle iconHandle, out IconInfo iconInfo);

    /// <summary>
    /// Retrieves information about the specified icon or cursor.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-geticoninfoexw">GetIconInfoEx function</a>
    /// This also describes to get more info on standard icons and cursors
    /// </summary>
    /// <param name="iconOrCursorHandle">A IntPtr handle to the icon or cursor.</param>
    /// <param name="iconInfoEx">A pointer to an ICONINFOEX structure. The function fills in the structure's members.</param>
    /// <returns>bool true if the function succeeds, the return value is in the IconInfo structure.</returns>
    [DllImport(User32Api.User32, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetIconInfoEx(IntPtr iconOrCursorHandle, ref IconInfoEx iconInfoEx);

    /// <summary>
    /// Retrieves information about the specified icon or cursor.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-geticoninfoexw">GetIconInfoEx function</a>
    /// This also describes to get more info on standard icons and cursors
    /// </summary>
    /// <param name="iconOrCursorHandle">A handle to the icon or cursor.</param>
    /// <param name="iconInfoEx">A pointer to an ICONINFOEX structure. The function fills in the structure's members.</param>
    /// <returns>bool true if the function succeeds, the return value is in the IconInfo structure.</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern bool GetIconInfoEx(SafeIconHandle iconOrCursorHandle, ref IconInfoEx iconInfoEx);

    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648062(v=vs.85).aspx">CreateIconIndirect function</a>
    /// </summary>
    /// <param name="icon">IconInfo</param>
    /// <returns>IntPtr with the icon handle</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

    /// <summary>
    /// Loads an icon with the specified dimensions from an icon resource.
    /// The icon resource can be from an application instance or loaded from a file.
    /// This function automatically scales down a larger image to the requested size.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-loadiconmetric">LoadIconMetric function</a>
    /// </summary>
    /// <param name="hInstance">
    /// A handle to the module of either a DLL or executable (.exe) that contains the icon to be loaded.
    /// To load a stock system icon, set this parameter to IntPtr.Zero.
    /// </param>
    /// <param name="pszIconName">
    /// The icon name. To load a stock system icon, use one of the IDI_* constants.
    /// To load from resources, use MAKEINTRESOURCE macro result or the string name.
    /// </param>
    /// <param name="lims">
    /// The size of the icon to load. This can be SmallIcon (SM_CXSMICON) or StandardIcon (SM_CXICON).
    /// </param>
    /// <param name="hIcon">
    /// When this function returns, contains a handle to the loaded icon.
    /// </param>
    /// <returns>
    /// If the function succeeds, it returns S_OK (0). Otherwise, it returns an HRESULT error code.
    /// </returns>
    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern int LoadIconMetric(IntPtr hInstance, IntPtr pszIconName, IconMetricSize lims, out IntPtr hIcon);

    /// <summary>
    /// Loads an icon with the specified dimensions from an icon resource.
    /// The icon resource can be from an application instance or loaded from a file.
    /// This function automatically scales down a larger image to the requested size.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-loadiconmetric">LoadIconMetric function</a>
    /// </summary>
    /// <param name="hInstance">
    /// A handle to the module of either a DLL or executable (.exe) that contains the icon to be loaded.
    /// To load a stock system icon, set this parameter to IntPtr.Zero.
    /// </param>
    /// <param name="pszIconName">
    /// The icon name as a string.
    /// </param>
    /// <param name="lims">
    /// The size of the icon to load. This can be SmallIcon (SM_CXSMICON) or StandardIcon (SM_CXICON).
    /// </param>
    /// <param name="hIcon">
    /// When this function returns, contains a handle to the loaded icon.
    /// </param>
    /// <returns>
    /// If the function succeeds, it returns S_OK (0). Otherwise, it returns an HRESULT error code.
    /// </returns>
    [DllImport("comctl32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int LoadIconMetric(IntPtr hInstance, string pszIconName, IconMetricSize lims, out IntPtr hIcon);

    /// <summary>
    /// Loads an icon. If the icon is larger than the requested size, this function scales down the icon to the requested size.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-loadiconwithscaledown">LoadIconWithScaleDown function</a>
    /// </summary>
    /// <param name="hInstance">
    /// A handle to the module of either a DLL or executable (.exe) that contains the icon to be loaded.
    /// To load a stock system icon, set this parameter to IntPtr.Zero.
    /// </param>
    /// <param name="pszIconName">
    /// The icon name. To load a stock system icon, use one of the IDI_* constants.
    /// To load from resources, use MAKEINTRESOURCE macro result or the string name.
    /// </param>
    /// <param name="cx">The desired width, in pixels, of the icon.</param>
    /// <param name="cy">The desired height, in pixels, of the icon.</param>
    /// <param name="hIcon">
    /// When this function returns, contains a handle to the loaded icon.
    /// </param>
    /// <returns>
    /// If the function succeeds, it returns S_OK (0). Otherwise, it returns an HRESULT error code.
    /// </returns>
    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern int LoadIconWithScaleDown(IntPtr hInstance, IntPtr pszIconName, int cx, int cy, out IntPtr hIcon);

    /// <summary>
    /// Loads an icon. If the icon is larger than the requested size, this function scales down the icon to the requested size.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-loadiconwithscaledown">LoadIconWithScaleDown function</a>
    /// </summary>
    /// <param name="hInstance">
    /// A handle to the module of either a DLL or executable (.exe) that contains the icon to be loaded.
    /// To load a stock system icon, set this parameter to IntPtr.Zero.
    /// </param>
    /// <param name="pszIconName">
    /// The icon name as a string.
    /// </param>
    /// <param name="cx">The desired width, in pixels, of the icon.</param>
    /// <param name="cy">The desired height, in pixels, of the icon.</param>
    /// <param name="hIcon">
    /// When this function returns, contains a handle to the loaded icon.
    /// </param>
    /// <returns>
    /// If the function succeeds, it returns S_OK (0). Otherwise, it returns an HRESULT error code.
    /// </returns>
    [DllImport("comctl32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int LoadIconWithScaleDown(IntPtr hInstance, string pszIconName, int cx, int cy, out IntPtr hIcon);


    [DllImport(User32Api.User32, SetLastError = true)]
    internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, DrawIconExFlags diFlags);
}