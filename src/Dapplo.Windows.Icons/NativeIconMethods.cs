﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Icons.Structs;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Win32 native methods for Icon
/// </summary>
public static class NativeIconMethods
{
    /// <summary>
    ///     Get the Icon from a file
    /// </summary>
    /// <param name="sFile">string</param>
    /// <param name="iIndex">int</param>
    /// <param name="piLargeVersion">IntPtr</param>
    /// <param name="piSmallVersion">IntPtr</param>
    /// <param name="amountIcons">int</param>
    /// <returns></returns>
    [DllImport("shell32", CharSet = CharSet.Unicode)]
    public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

    /// <summary>
    ///     The following is used for Icon handling, and copies a hicon to a new
    /// </summary>
    /// <param name="hIcon">IntPtr</param>
    /// <returns>SafeIconHandle</returns>
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
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern bool GetIconInfo(SafeIconHandle iconHandle, out IconInfo iconInfo);

    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648062(v=vs.85).aspx">CreateIconIndirect function</a>
    /// </summary>
    /// <param name="icon">IconInfo</param>
    /// <returns>IntPtr with the icon handle</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    public static extern IntPtr CreateIconIndirect(ref IconInfo icon);
}