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
using System.Security.Permissions;
using Dapplo.Log;
using Dapplo.Windows.User32.Enums;
using Microsoft.Win32.SafeHandles;

#endregion

namespace Dapplo.Windows.User32.SafeHandles
{
    /// <summary>
    ///     A SafeHandle class implementation for the current input desktop
    /// </summary>
    public class SafeCurrentInputDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Default constructor, this opens the input destop with GENERIC_ALL
        ///     This is needed to support marshalling!!
        /// </summary>
        public SafeCurrentInputDesktopHandle() : base(true)
        {
            var hDesktop = User32Api.OpenInputDesktop(0, true, DesktopAccessRight.GENERIC_ALL);
            if (hDesktop != IntPtr.Zero)
            {
                // Got desktop, store it as handle for the ReleaseHandle
                SetHandle(hDesktop);
                if (User32Api.SetThreadDesktop(hDesktop))
                {
                    Log.Debug().WriteLine("Switched to desktop {0}", hDesktop);
                }
                else
                {
                    Log.Warn().WriteLine("Couldn't switch to desktop {0}", hDesktop);
                    Log.Error().WriteLine(User32Api.CreateWin32Exception("SetThreadDesktop"));
                }
            }
            else
            {
                Log.Warn().WriteLine("Couldn't get current desktop.");
                Log.Error().WriteLine(User32Api.CreateWin32Exception("OpenInputDesktop"));
            }
        }

        /// <summary>
        ///     Close the desktop
        /// </summary>
        /// <returns>true if this succeeded</returns>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return User32Api.CloseDesktop(handle);
        }
    }
}