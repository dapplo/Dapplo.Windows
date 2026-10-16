// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Shell32.Structs;

/// <summary>
///     Receives the information of a stock icon from SHGetStockIconInfo (SHSTOCKICONINFO).
///     See <a href="https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-shstockiconinfo">SHSTOCKICONINFO structure</a>.
///     Always use <see cref="Create"/>: the size of the structure must be set, without it SHGetStockIconInfo fails with E_INVALIDARG.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct StockIconInfo
{
    private uint _cbSize;
    private readonly IntPtr _hIcon;
    private readonly int _iSysImageIndex;
    private readonly int _iIcon;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _szPath;

    /// <summary>
    ///     Create a StockIconInfo with the size of the structure set, as SHGetStockIconInfo requires
    /// </summary>
    /// <returns>StockIconInfo</returns>
    public static StockIconInfo Create()
    {
        return new StockIconInfo
        {
            _cbSize = (uint)Marshal.SizeOf(typeof(StockIconInfo))
        };
    }

    /// <summary>
    ///     The size of the structure in bytes, set by <see cref="Create"/>
    /// </summary>
    public uint Size => _cbSize;

    /// <summary>
    ///     The handle of the icon when StockIconFlags.Icon was requested, else IntPtr.Zero.
    ///     The caller owns it and must destroy it with DestroyIcon.
    /// </summary>
    public IntPtr IconHandle => _hIcon;

    /// <summary>
    ///     The index of the icon in the system image list when StockIconFlags.SystemIconIndex was requested
    /// </summary>
    public int SystemImageIndex => _iSysImageIndex;

    /// <summary>
    ///     The index of the icon in the file <see cref="Path"/>; a negative value is a resource ID, as for ExtractIconEx and SHDefExtractIcon
    /// </summary>
    public int IconIndex => _iIcon;

    /// <summary>
    ///     The path of the file which contains the icon (e.g. imageres.dll)
    /// </summary>
    public string Path => _szPath;
}
