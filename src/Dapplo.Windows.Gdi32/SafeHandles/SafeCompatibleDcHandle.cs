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

#endregion

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A CompatibleDC SafeHandle implementation
    /// </summary>
    public class SafeCompatibleDcHandle : SafeDcHandle
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeCompatibleDcHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create SafeCompatibleDcHandle from existing handle
        /// </summary>
        /// <param name="preexistingHandle">IntPtr with existing handle</param>
        [SecurityCritical]
        public SafeCompatibleDcHandle(IntPtr preexistingHandle) : base(true)
        {
            SetHandle(preexistingHandle);
        }

        [DllImport("gdi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hDc);

        /// <summary>
        ///     Call DeleteDC, this disposes the unmanaged resources
        /// </summary>
        /// <returns>bool true if the DC was deleted</returns>
        protected override bool ReleaseHandle()
        {
            return DeleteDC(handle);
        }

        /// <summary>
        ///     Select an object onto the DC
        /// </summary>
        /// <param name="objectSafeHandle">SafeHandle for object</param>
        /// <returns>SafeSelectObjectHandle</returns>
        public SafeSelectObjectHandle SelectObject(SafeHandle objectSafeHandle)
        {
            return new SafeSelectObjectHandle(this, objectSafeHandle);
        }
    }
}