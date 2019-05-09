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
using System.Drawing;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

#endregion

namespace Dapplo.Windows.Icons.SafeHandles
{
    /// <summary>
    ///     A SafeHandle class implementation for the hIcon
    /// </summary>
    public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeIconHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeIconHandle from a bitmap by calling GetHicon
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        public SafeIconHandle(Bitmap bitmap) : base(true)
        {
            SetHandle(bitmap.GetHicon());
        }

        /// <summary>
        ///     Create a SafeIconHandle from a hIcon
        /// </summary>
        /// <param name="hIcon">IntPtr to an icon</param>
        public SafeIconHandle(IntPtr hIcon) : base(true)
        {
            SetHandle(hIcon);
        }

        /// <summary>
        ///     Call destroy icon
        /// </summary>
        /// <returns>true if this worked</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return NativeIconMethods.DestroyIcon(handle);
        }
    }
}