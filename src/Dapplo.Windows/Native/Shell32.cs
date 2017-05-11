//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Dapplo.Windows.Native
{
    /// <summary>
    ///     Description of Shell32.
    /// </summary>
    public static class Shell32
    {
        /// <summary>
        ///     Options to specify whether folders should be in the open or closed state.
        /// </summary>
        public enum FolderType
        {
            /// <summary>
            ///     Specify open folder.
            /// </summary>
            Open = 0,

            /// <summary>
            ///     Specify closed folder.
            /// </summary>
            Closed = 1
        }

        /// <summary>
        ///     Options to specify the size of icons to return.
        /// </summary>
        public enum IconSize
        {
            /// <summary>
            ///     Specify large icon - 32 pixels by 32 pixels.
            /// </summary>
            Large = 0,

            /// <summary>
            ///     Specify small icon - 16 pixels by 16 pixels.
            /// </summary>
            Small = 1
        }

        [DllImport("shell32", CharSet = CharSet.Unicode)]
        internal static extern IntPtr ExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index);

        /// <summary>
        ///     Returns an icon representation of an image contained in the specified file.
        ///     This function is identical to System.Drawing.Icon.ExtractAssociatedIcon, xcept this version works.
        ///     See: http://stackoverflow.com/questions/1842226/how-to-get-the-associated-icon-from-a-network-share-file
        /// </summary>
        /// <param name="filePath">The path to the file that contains an image.</param>
        /// <returns>The System.Drawing.Icon representation of the image contained in the specified file.</returns>
        public static Icon ExtractAssociatedIcon(string filePath)
        {
            var index = 0;

            Uri uri;
            if (filePath == null)
            {
                throw new ArgumentException(string.Format("'{0}' is not valid for '{1}'", "null", "filePath"), "filePath");
            }
            try
            {
                uri = new Uri(filePath);
            }
            catch (UriFormatException)
            {
                filePath = Path.GetFullPath(filePath);
                uri = new Uri(filePath);
            }

            if (uri.IsFile)
            {
                if (File.Exists(filePath))
                {
                    var iconPath = new StringBuilder(1024);
                    iconPath.Append(filePath);

                    var handle = ExtractAssociatedIcon(new HandleRef(null, IntPtr.Zero), iconPath, ref index);
                    if (handle != IntPtr.Zero)
                    {
                        return Icon.FromHandle(handle);
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     Get the Icon from a file
        /// </summary>
        /// <param name="sFile"></param>
        /// <param name="iIndex"></param>
        /// <param name="piLargeVersion"></param>
        /// <param name="piSmallVersion"></param>
        /// <param name="amountIcons"></param>
        /// <returns></returns>
        [DllImport("shell32", CharSet = CharSet.Unicode)]
        public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        /// <summary>
        ///     Returns an icon for a given file extension - indicated by the name parameter.
        ///     See: http://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="size">Large or small</param>
        /// <param name="linkOverlay">Whether to include the link icon</param>
        /// <returns>System.Drawing.Icon</returns>
        public static Icon GetFileIcon(string filename, IconSize size, bool linkOverlay)
        {
            var shfi = new SHFILEINFO();
            // SHGFI_USEFILEATTRIBUTES makes it simulate, just gets the icon for the extension
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (linkOverlay)
            {
                flags += SHGFI_LINKOVERLAY;
            }

            // Check the size specified for return.
            if (IconSize.Small == size)
            {
                flags += SHGFI_SMALLICON;
            }
            else
            {
                flags += SHGFI_LARGEICON;
            }

            SHGetFileInfo(Path.GetFileName(filename), FILE_ATTRIBUTE_NORMAL, ref shfi, (uint) Marshal.SizeOf(shfi), flags);

            // Only return an icon if we really got one
            if (shfi.hIcon != IntPtr.Zero)
            {
                // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
                var icon = (Icon) Icon.FromHandle(shfi.hIcon).Clone();
                // Cleanup
                User32.User32Api.DestroyIcon(shfi.hIcon);
                return icon;
            }
            return null;
        }

        /// <summary>
        ///     Used to access system folder icons.
        /// </summary>
        /// <param name="size">Specify large or small icons.</param>
        /// <param name="folderType">Specify open or closed FolderType.</param>
        /// <returns>System.Drawing.Icon</returns>
        public static Icon GetFolderIcon(IconSize size, FolderType folderType)
        {
            // Need to add size check, although errors generated at present!
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (FolderType.Open == folderType)
            {
                flags += SHGFI_OPENICON;
            }

            if (IconSize.Small == size)
            {
                flags += SHGFI_SMALLICON;
            }
            else
            {
                flags += SHGFI_LARGEICON;
            }

            // Get the folder icon
            var shfi = new SHFILEINFO();
            SHGetFileInfo(null, FILE_ATTRIBUTE_DIRECTORY, ref shfi, (uint) Marshal.SizeOf(shfi), flags);

            //Icon.FromHandle(shfi.hIcon);	// Load the icon from an HICON handle
            // Now clone the icon, so that it can be successfully stored in an ImageList
            var icon = (Icon) Icon.FromHandle(shfi.hIcon).Clone();

            // Cleanup
            User32.User32Api.DestroyIcon(shfi.hIcon);
            return icon;
        }

        [DllImport("shell32", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        #region Structs

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public readonly IntPtr hIcon;
            public readonly int iIcon;
            public readonly uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public readonly string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public readonly string szTypeName;
        }

        #endregion

        #region Constants

        // Browsing for directory.
        private const uint BIF_RETURNONLYFSDIRS = 0x0001;

        private const uint BIF_DONTGOBELOWDOMAIN = 0x0002;
        private const uint BIF_STATUSTEXT = 0x0004;
        private const uint BIF_RETURNFSANCESTORS = 0x0008;
        private const uint BIF_EDITBOX = 0x0010;
        private const uint BIF_VALIDATE = 0x0020;
        private const uint BIF_NEWDIALOGSTYLE = 0x0040;
        private const uint BIF_USENEWUI = BIF_NEWDIALOGSTYLE | BIF_EDITBOX;
        private const uint BIF_BROWSEINCLUDEURLS = 0x0080;
        private const uint BIF_BROWSEFORCOMPUTER = 0x1000;
        private const uint BIF_BROWSEFORPRINTER = 0x2000;
        private const uint BIF_BROWSEINCLUDEFILES = 0x4000;
        private const uint BIF_SHAREABLE = 0x8000;

        private const uint SHGFI_ICON = 0x000000100; // get icon
        private const uint SHGFI_DISPLAYNAME = 0x000000200; // get display name
        private const uint SHGFI_TYPENAME = 0x000000400; // get type name
        private const uint SHGFI_ATTRIBUTES = 0x000000800; // get attributes
        private const uint SHGFI_ICONLOCATION = 0x000001000; // get icon location
        private const uint SHGFI_EXETYPE = 0x000002000; // return exe type
        private const uint SHGFI_SYSICONINDEX = 0x000004000; // get system icon index
        private const uint SHGFI_LINKOVERLAY = 0x000008000; // put a link overlay on icon
        private const uint SHGFI_SELECTED = 0x000010000; // show icon in selected state
        private const uint SHGFI_ATTR_SPECIFIED = 0x000020000; // get only specified attributes
        private const uint SHGFI_LARGEICON = 0x000000000; // get large icon
        private const uint SHGFI_SMALLICON = 0x000000001; // get small icon
        private const uint SHGFI_OPENICON = 0x000000002; // get open icon
        private const uint SHGFI_SHELLICONSIZE = 0x000000004; // get shell size icon
        private const uint SHGFI_PIDL = 0x000000008; // pszPath is a pidl
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // use passed dwFileAttribute
        private const uint SHGFI_ADDOVERLAYS = 0x000000020; // apply the appropriate overlays
        private const uint SHGFI_OVERLAYINDEX = 0x000000040; // Get the index of the overlay

        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        #endregion
    }
}