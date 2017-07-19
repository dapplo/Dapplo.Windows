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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Dapplo.Windows.User32;
using Microsoft.Win32.SafeHandles;

#endregion

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A WindowDC SafeHandle implementation
    /// </summary>
    public class SafeWindowDcHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly IntPtr _hWnd;

        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeWindowDcHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeWindowDcHandle for an existing handöe
        /// </summary>
        /// <param name="hWnd">IntPtr for the window</param>
        /// <param name="existingDcHandle">IntPtr to the DC</param>
        [SecurityCritical]
        public SafeWindowDcHandle(IntPtr hWnd, IntPtr existingDcHandle) : base(true)
        {
            _hWnd = hWnd;
            SetHandle(existingDcHandle);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static SafeWindowDcHandle FromDesktop()
        {
            var hWndDesktop = User32Api.GetDesktopWindow();
            var hDcDesktop = GetWindowDC(hWndDesktop);
            return new SafeWindowDcHandle(hWndDesktop, hDcDesktop);
        }

        [DllImport(User32Api.User32, SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport(User32Api.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        /// <summary>
        ///     ReleaseDC for the original Window
        /// </summary>
        /// <returns>true if this worked</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return ReleaseDC(_hWnd, handle);
        }
    }
}