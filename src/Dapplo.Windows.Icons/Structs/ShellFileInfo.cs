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
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Icons.Structs
{ 
    /// <summary>
    /// A structure which describes shell32 info on a file
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ShellFileInfo
    {
        private readonly IntPtr _hIcon;
        private readonly int _iIcon;
        private readonly uint _dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        private readonly string _szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        private readonly string _szTypeName;

        /// <summary>
        /// A handle to the icon that represents the file.
        /// You are responsible for destroying this handle with DestroyIcon when you no longer need it.
        /// </summary>
        public IntPtr IconHandle => _hIcon;

        /// <summary>
        /// The index of the icon image within the system image list.
        /// </summary>
        public int IconIndex => _iIcon;

        /// <summary>
        /// An array of values that indicates the attributes of the file object.
        /// For information about these values, see the IShellFolder::GetAttributesOf method.
        /// </summary>
        public uint Attributes => _dwAttributes;

        /// <summary>
        /// A string that contains the name of the file as it appears in the Windows Shell, or the path and file name of the file that contains the icon representing the file.
        /// </summary>
        public string DisplayName => _szDisplayName;

        /// <summary>
        /// A string that describes the type of file
        /// </summary>
        public string TypeName => _szTypeName;
    }
}