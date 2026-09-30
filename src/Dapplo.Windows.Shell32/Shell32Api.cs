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