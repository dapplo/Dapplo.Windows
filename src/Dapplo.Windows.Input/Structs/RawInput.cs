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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains the raw input from a device.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645562.aspx">RAWINPUT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInput
    {
        // 64 bit header size: 24  32 bit the header size: 16
        private RawInputHeader _header;
        // Creating the rest in a struct allows the header size to align correctly for 32/64 bit
        private RawDevice _device;

        /// <summary>
        /// The RawInput header
        /// </summary>
        public RawInputHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// The device.
        /// </summary>
        public RawDevice Device
        {
            get { return _device; }
            set { _device = value; }
        }
    }
}