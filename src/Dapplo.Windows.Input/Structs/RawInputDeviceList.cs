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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    /// Contains information about a raw input device.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645568.aspx">RAWINPUTDEVICELIST structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
	[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
	[SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputDeviceList
    {
        private readonly IntPtr _hDevice;
        private readonly RawInputDeviceTypes _dwType;

        /// <summary>
        /// A handle to the raw input device.
        /// </summary>
        public IntPtr Handle
        {
            get { return _hDevice; }
        }

        /// <summary>
        /// The type of device
        /// </summary>
        public RawInputDeviceTypes RawInputDeviceType
        {
            get { return _dwType; }
        }
   }
}