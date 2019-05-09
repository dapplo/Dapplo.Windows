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
        /// Creates a DC as SafeWindowDcHandle for the whole of the specified hWnd
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>SafeWindowDcHandle</returns>
        public static SafeWindowDcHandle FromWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }
            var hDcDesktop = User32Api.GetWindowDC(hWnd);
            return new SafeWindowDcHandle(hWnd, hDcDesktop);
        }

        /// <summary>
        /// Creates a DC as SafeWindowDcHandle for the client area of the specified hWnd
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>SafeWindowDcHandle</returns>
        public static SafeWindowDcHandle FromWindowClientArea(IntPtr hWnd)
        {
            var hDcDesktop = User32Api.GetWindowDC(hWnd);
            return new SafeWindowDcHandle(hWnd, hDcDesktop);
        }

        /// <summary>
        /// Creates a SafeWindowDcHandle for the Desktop
        /// </summary>
        /// <returns>SafeWindowDcHandle</returns>
        public static SafeWindowDcHandle FromDesktop()
        {
            var hWndDesktop = User32Api.GetDesktopWindow();
            return FromWindow(hWndDesktop);
        }

        /// <summary>
        ///     ReleaseDC for the original Window
        /// </summary>
        /// <returns>true if this worked</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return User32Api.ReleaseDC(_hWnd, handle);
        }
    }
}