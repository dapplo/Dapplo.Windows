// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;

namespace Dapplo.Windows.Shell32;

/// <summary>
/// An API for Shell32 functionality
/// </summary>
public static class Shell32Api
{
    private const string Shell32Dll = "shell32.dll";

    /// <summary>
    /// Retrieves the bounds and edge of the Windows taskbar (ABM_GETTASKBARPOS).
    /// </summary>
    /// <param name="appBarData">AppBarData which describes the taskbar bounds and edge, default when the call failed</param>
    /// <returns>true if the taskbar position could be retrieved</returns>
    public static bool TryGetTaskbarPosition(out AppBarData appBarData)
    {
        appBarData = AppBarData.Create();
        if (SHAppBarMessage(AppBarMessages.GetTaskbarPosition, ref appBarData) != IntPtr.Zero)
        {
            return true;
        }
        appBarData = default;
        return false;
    }

    /// <summary>
    /// Retrieves the autohide and always-on-top states of the Windows taskbar (ABM_GETSTATE).
    /// The state is the return value of SHAppBarMessage, it is not returned in the AppBarData.
    /// </summary>
    /// <returns>AppBarStates</returns>
    public static AppBarStates GetTaskbarState()
    {
        var appBarData = AppBarData.Create();
        return (AppBarStates)SHAppBarMessage(AppBarMessages.GetState, ref appBarData).ToInt64();
    }

    /// <summary>
    ///     Extracts icons from an executable, DLL or icon file.
    ///     See <a href="https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-extracticonexw">ExtractIconExW function</a>
    ///     The caller owns the returned icon handles and must destroy them with DestroyIcon.
    /// </summary>
    /// <param name="lpszFile">Path of the executable, DLL or icon file</param>
    /// <param name="nIconIndex">Zero-based index of the first icon to extract, a negative value is a resource ID</param>
    /// <param name="phiconLarge">Array which receives the large icon handles, must have at least <paramref name="nIcons"/> elements, or null</param>
    /// <param name="phiconSmall">Array which receives the small icon handles, must have at least <paramref name="nIcons"/> elements, or null</param>
    /// <param name="nIcons">Number of icons to extract</param>
    /// <returns>The number of icons successfully extracted</returns>
    [DllImport(Shell32Dll, CharSet = CharSet.Unicode, EntryPoint = "ExtractIconExW", ExactSpelling = true)]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, uint nIcons);

    /// <summary>
    ///     Returns the number of icons in an executable, DLL or icon file,
    ///     this uses ExtractIconEx with nIconIndex -1 and both icon arrays NULL, as documented.
    /// </summary>
    /// <param name="lpszFile">Path of the executable, DLL or icon file</param>
    /// <returns>The number of icons in the file</returns>
    public static uint CountIcons(string lpszFile) => ExtractIconExCount(lpszFile, -1, IntPtr.Zero, IntPtr.Zero, 0);

    [DllImport(Shell32Dll, CharSet = CharSet.Unicode, EntryPoint = "ExtractIconExW", ExactSpelling = true)]
    private static extern uint ExtractIconExCount(string lpszFile, int nIconIndex, IntPtr phiconLarge, IntPtr phiconSmall, uint nIcons);


    /// <summary>
    ///     Retrieves information about a system-defined Shell icon, a stock icon (Windows Vista and later).
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shgetstockiconinfo">SHGetStockIconInfo function</a>.
    ///     Create the <paramref name="info"/> with <see cref="StockIconInfo.Create"/>, the size of the structure must be set.
    ///     With <see cref="StockIconFlags.Icon"/> the caller owns <see cref="StockIconInfo.IconHandle"/> and must destroy it with DestroyIcon.
    ///     For an icon in any size use the path and index (<see cref="StockIconFlags.IconLocation"/>) with <see cref="SHDefExtractIcon"/>,
    ///     or StockIcons.GetIcon in Dapplo.Windows.Icons, which does that.
    /// </summary>
    /// <param name="siid">StockIconId of the icon</param>
    /// <param name="uFlags">StockIconFlags, what to return</param>
    /// <param name="info">ref StockIconInfo, receives the information</param>
    /// <returns>HRESULT, 0 (S_OK) on success, e.g. E_INVALIDARG (0x80070057) for an unknown id or a structure without its size</returns>
    [DllImport(Shell32Dll, ExactSpelling = true)]
    public static extern int SHGetStockIconInfo(StockIconId siid, StockIconFlags uFlags, ref StockIconInfo info);

    /// <summary>
    ///     Extracts an icon from a file in the requested size, e.g. the file and index of a stock icon.
    ///     See <a href="https://learn.microsoft.com/windows/win32/api/shlobj_core/nf-shlobj_core-shdefextracticonw">SHDefExtractIconW function</a>.
    ///     The caller owns the returned icon handles and must destroy both with DestroyIcon.
    /// </summary>
    /// <remarks>
    ///     <paramref name="nIconSize"/> holds two sizes in pixels: the large size in the low word and the small size in the high word,
    ///     large | (small &lt;&lt; 16). E.g. 96 px large and 16 px small is 96 | (16 &lt;&lt; 16) = 0x00100060. Passing only the large size (96)
    ///     asks for a small icon of 0 px. Use <see cref="MakeIconSize"/> to build the value.
    /// </remarks>
    /// <param name="pszIconFile">Path of the file which contains the icon, e.g. <see cref="StockIconInfo.Path"/></param>
    /// <param name="iIndex">Index of the icon in the file, a negative value is a resource ID (e.g. <see cref="StockIconInfo.IconIndex"/>)</param>
    /// <param name="uFlags">GIL_* flags, normally 0; GIL_SIMULATEDOC (1) draws the icon as a document</param>
    /// <param name="phiconLarge">Receives the handle of the large icon</param>
    /// <param name="phiconSmall">Receives the handle of the small icon</param>
    /// <param name="nIconSize">The sizes of the large (low word) and small (high word) icon in pixels, see the remarks and <see cref="MakeIconSize"/></param>
    /// <returns>HRESULT, 0 (S_OK) on success, 1 (S_FALSE) when the file has no icon at the index, E_FAIL when the file isn't found</returns>
    [DllImport(Shell32Dll, CharSet = CharSet.Unicode, EntryPoint = "SHDefExtractIconW", ExactSpelling = true)]
    public static extern int SHDefExtractIcon(string pszIconFile, int iIndex, uint uFlags, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);

    /// <summary>
    ///     The nIconSize value of <see cref="SHDefExtractIcon"/>: the large size in the low word, the small size in the high word,
    ///     largeSize | (smallSize &lt;&lt; 16)
    /// </summary>
    /// <param name="largeSize">int with the size of the large icon in pixels, 1 to 65535</param>
    /// <param name="smallSize">int with the size of the small icon in pixels, 1 to 65535</param>
    /// <returns>uint for nIconSize</returns>
    /// <exception cref="ArgumentOutOfRangeException">a size is not between 1 and 65535</exception>
    public static uint MakeIconSize(int largeSize, int smallSize)
    {
        if (largeSize < 1 || largeSize > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(largeSize), largeSize, "The size must be between 1 and 65535 pixels.");
        }
        if (smallSize < 1 || smallSize > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(smallSize), smallSize, "The size must be between 1 and 65535 pixels.");
        }
        return (uint)largeSize | ((uint)smallSize << 16);
    }

    /// <summary>
    /// Sends an appbar message to the system.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
    /// </summary>
    /// <param name="dwMessage">AppBarMessages - Appbar message value to send.</param>
    /// <param name="pData">A pointer to an AppBarData structure. The content of the structure on entry and on exit depends on the value set in the dwMessage parameter.
    /// See the individual message pages for specifics.</param>
    /// <returns>A message-dependent value, see the individual message pages. For ABM_GETSTATE this is the AppBarStates, for most other messages 0 means failure.</returns>
    [DllImport(Shell32Dll)]
    public static extern IntPtr SHAppBarMessage(AppBarMessages dwMessage, ref AppBarData pData);

    /// <summary>
    /// Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfoa">SHGetFileInfo</a>
    /// </summary>
    /// <param name="pszPath">string
    /// A pointer to a null-terminated string of maximum length MAX_PATH that contains the path and file name. Both absolute and relative paths are valid.
    /// If the uFlags parameter includes the SHGFI_PIDL flag, this parameter must be the address of an ITEMIDLIST (PIDL) structure that contains the list of item identifiers that uniquely identifies the file within the Shell's namespace. The PIDL must be a fully qualified PIDL. Relative PIDLs are not allowed.
    /// If the uFlags parameter includes the SHGFI_USEFILEATTRIBUTES flag, this parameter does not have to be a valid file name. The function will proceed as if the file exists with the specified name and with the file attributes passed in the dwFileAttributes parameter. This allows you to obtain information about a file type by passing just the extension for pszPath and passing FILE_ATTRIBUTE_NORMAL in dwFileAttributes.
    /// This string can use either short (the 8.3 form) or long file names.
    /// </param>
    /// <param name="dwFileAttributes">uint
    /// A combination of one or more file attribute flags (FILE_ATTRIBUTE_ values as defined in Winnt.h).
    /// If uFlags does not include the SHGFI_USEFILEATTRIBUTES flag, this parameter is ignored.
    /// </param>
    /// <param name="psfi">ref to ShellFileInfo</param>
    /// <param name="cbFileInfo">uint</param>
    /// <param name="uFlags">ShellGetFileInfoFlags</param>
    /// <returns>IntPtr</returns>
    [DllImport("shell32", CharSet = CharSet.Unicode)]
    public static extern IntPtr SHGetFileInfo(string pszPath, ShellFileAttributeFlags dwFileAttributes, ref ShellFileInfo psfi, uint cbFileInfo, ShellGetFileInfoFlags uFlags);
}