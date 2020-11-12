// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Dapplo.Windows.User32;
using Microsoft.Win32.SafeHandles;

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
#if !NET5_0
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
        protected override bool ReleaseHandle()
        {
            return User32Api.ReleaseDC(_hWnd, handle);
        }
    }
}