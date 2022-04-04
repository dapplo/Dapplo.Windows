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
    /// Returns an AppBarData struct which describes the Taskbar bounds etc
    /// </summary>
    /// <returns>AppBarData</returns>
    public static AppBarData TaskbarPosition
    {
        get
        {
            var appBarData = AppBarData.Create();
            SHAppBarMessage(AppBarMessages.GetTaskbarPosition, ref appBarData);
            return appBarData;
        }
    }

    /// <summary>
    /// Sends an appbar message to the system.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
    /// </summary>
    /// <param name="dwMessage">AppBarMessages - Appbar message value to send.</param>
    /// <param name="pData">A pointer to an AppBarData structure. The content of the structure on entry and on exit depends on the value set in the dwMessage parameter.
    /// See the individual message pages for specifics.</param>
    /// <returns></returns>
    [DllImport(Shell32Dll, SetLastError = true)]
    private static extern IntPtr SHAppBarMessage(AppBarMessages dwMessage, [In] ref AppBarData pData);

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