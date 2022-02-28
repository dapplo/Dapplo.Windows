// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;

namespace Dapplo.Windows.Shell32
{
    /// <summary>
    /// An API for Shell32 functionality
    /// </summary>
    public static class Shell32Api
    {
        /// <summary>
        /// The DLL Name for the Shell32 library
        /// </summary>
        public const string Shell32 = "shell32";

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
        /// This deletes a file or folder and places it in the recycle-bin
        /// </summary>
        /// <param name="path"></param>
        /// <param name="hWnd">IntPtr with the hWnd</param>
        public static bool DeleteFileOrFolder(string path, IntPtr hWnd = default)
        {
            var fileOp = new ShFileOp
            {
                hWnd = hWnd,
                wFunc = FileFuncFlags.FO_DELETE,
                pFrom = path + '\0' + '\0',
                fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO | FILEOP_FLAGS.FOF_NOCONFIRMATION
            };
            return SHFileOperation(ref fileOp) == 0;
        }


        /// <summary>
        /// Sends an appbar message to the system.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb762108.aspx">SHAppBarMessage function</a>
        /// </summary>
        /// <param name="dwMessage">AppBarMessages - Appbar message value to send.</param>
        /// <param name="pData">A pointer to an AppBarData structure. The content of the structure on entry and on exit depends on the value set in the dwMessage parameter.
        /// See the individual message pages for specifics.</param>
        /// <returns>int 0 is ok, everything else is not</returns>
        [DllImport(Shell32, SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(AppBarMessages dwMessage, [In] ref AppBarData pData);

        /// <summary>
        /// Copies, moves, renames, or deletes a file system object.
        /// </summary>
        /// <param name="fileOp">ShFileOp ref to struct with the information on what needs to be done</param>
        /// <returns></returns>
        [DllImport(Shell32, CharSet = CharSet.Auto)]
        public static extern int SHFileOperation(ref ShFileOp fileOp);
    }
}
