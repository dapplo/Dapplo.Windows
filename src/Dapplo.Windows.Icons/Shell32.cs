//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

#region using

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Icons.Enums;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Icons.Structs;

#endregion

namespace Dapplo.Windows.Icons
{
    /// <summary>
    ///     Description of Shell32.
    /// </summary>
    public static class Shell32
    {
        /// <summary>
        ///     Returns an icon representation of an image contained in the specified file.
        ///     This function is identical to System.Drawing.Icon.ExtractAssociatedIcon, xcept this version works.
        ///     See: http://stackoverflow.com/questions/1842226/how-to-get-the-associated-icon-from-a-network-share-file
        /// </summary>
        /// <param name="filePath">The path to the file that contains an image.</param>
        /// <param name="iconIndex">Index of the icon</param>
        /// <returns>The System.Drawing.Icon representation of the image contained in the specified file.</returns>
        public static TIcon ExtractAssociatedIcon<TIcon>(string filePath, int iconIndex = 0) where TIcon : class
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (!Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
            {
                filePath = Path.GetFullPath(filePath);
                uri = new Uri(filePath);
            }
            if (!uri.IsFile)
            {
                return null;
            }
            if (!File.Exists(filePath))
            {
                return null;
            }
            var iconPath = new StringBuilder(1024);
            iconPath.Append(filePath);

            using (var handle = ExtractAssociatedIcon(new HandleRef(null, IntPtr.Zero), iconPath, ref iconIndex))
            {
                return IconHelper.IconHandleTo<TIcon>(handle.DangerousGetHandle());
            }
        }

        /// <summary>
        ///     Returns an icon for a given file extension - indicated by the name parameter.
        ///     See: http://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="size">Large or small</param>
        /// <param name="linkOverlay">Whether to include the link icon</param>
        /// <returns>System.Drawing.Icon</returns>
        public static TIcon GetFileExtensionIcon<TIcon>(string filename, IconSize size, bool linkOverlay) where TIcon : class
        {
            var shfi = new ShellFileInfo();
            // UseFileAttributes makes it simulate, just gets the icon for the extension
            var flags = ShellGetFileInfoFlags.Icon | ShellGetFileInfoFlags.UseFileAttributes;

            if (linkOverlay)
            {
                flags |= ShellGetFileInfoFlags.LinkOverlay;
            }

            // Check the size specified for return.
            if (IconSize.Small == size)
            {
                flags |= ShellGetFileInfoFlags.SmallIcon;
            }
            else
            {
                flags |= ShellGetFileInfoFlags.LargeIcon;
            }

            SHGetFileInfo(Path.GetFileName(filename), FILE_ATTRIBUTE_NORMAL, ref shfi, (uint) Marshal.SizeOf(shfi), flags);

            // TODO: Fix bad practise for cleanup, and use generics to allow the user to specify if it's an icon/bitmap/-source
            // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
            try
            {
                return IconHelper.IconHandleTo<TIcon>(shfi.IconHandle);
            }
            finally
            {
                if (shfi.IconHandle != IntPtr.Zero)
                {
                    // Cleanup
                    NativeIconMethods.DestroyIcon(shfi.IconHandle);
                }
            }
        }

        /// <summary>
        ///     Used to access system folder icons.
        /// </summary>
        /// <param name="size">Specify large or small icons.</param>
        /// <param name="folderType">Specify open or closed FolderType.</param>
        /// <returns>System.Drawing.Icon</returns>
        public static TIcon GetFolderIcon<TIcon>(IconSize size, FolderType folderType) where TIcon : class
        {
            // Need to add size check, although errors generated at present!
            var flags = ShellGetFileInfoFlags.Icon | ShellGetFileInfoFlags.UseFileAttributes;

            if (FolderType.Open == folderType)
            {
                flags |= ShellGetFileInfoFlags.OpenIcon;
            }

            if (IconSize.Small == size)
            {
                flags |= ShellGetFileInfoFlags.SmallIcon;
            }
            else
            {
                flags |= ShellGetFileInfoFlags.LargeIcon;
            }

            // Get the folder icon
            var shellFileInfo = new ShellFileInfo();
            SHGetFileInfo(null, FILE_ATTRIBUTE_DIRECTORY, ref shellFileInfo, (uint) Marshal.SizeOf(shellFileInfo), flags);

            // Now clone the icon, so that it can be successfully stored in an ImageList
            try
            {
                return IconHelper.IconHandleTo<TIcon>(shellFileInfo.IconHandle);
            }
            finally
            {
                if (shellFileInfo.IconHandle != IntPtr.Zero)
                {
                    // Cleanup
                    NativeIconMethods.DestroyIcon(shellFileInfo.IconHandle);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pszPath">string</param>
        /// <param name="dwFileAttributes">uint</param>
        /// <param name="psfi">ref to ShellFileInfo</param>
        /// <param name="cbFileInfo">uint</param>
        /// <param name="uFlags">ShellGetFileInfoFlags</param>
        /// <returns>IntPtr</returns>
        [DllImport("shell32", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref ShellFileInfo psfi, uint cbFileInfo, ShellGetFileInfoFlags uFlags);

        /// <summary>
        /// Retrieves a handle to an indexed icon found in a file or an icon found in an associated executable file.
        /// </summary>
        /// <param name="hInst">A handle to the instance of the application calling the function.</param>
        /// <param name="iconPath">StringBuilder</param>
        /// <param name="iconIndex">The full path and file name of the file that contains the icon. The function extracts the icon handle from that file, or from an executable file associated with that file. If the icon handle is obtained from an executable file, the function stores the full path and file name of that executable in the string pointed to by lpIconPath.</param>
        /// <returns>SafeIconHandle</returns>
        [DllImport("shell32", CharSet = CharSet.Unicode)]
        private static extern SafeIconHandle ExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int iconIndex);

        #region Constants

        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        #endregion
    }
}