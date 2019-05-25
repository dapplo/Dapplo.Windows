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

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645597.aspx">GetRawInputDeviceInfo function</a>
    /// </summary>
    public enum RawInputDeviceInfoCommands : uint
    {
        /// <summary>
        /// pData points to a string that contains the device name.
        /// For this uiCommand only, the value in pcbSize is the character count (not the byte count)
        /// </summary>
        DeviceName = 0x20000007,
        /// <summary>
        /// pData points to an RID_DEVICE_INFO structure.
        /// </summary>
        DeviceInfo = 0x2000000b,
        /// <summary>
        /// pData points to the previously parsed data.
        /// </summary>
        PreparsedData = 0x20000005
    }
}
