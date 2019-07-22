//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019 Dapplo
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

using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Messages.Structs
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-_dev_broadcast_hdr">DEV_BROADCAST_HDR structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DevBroadcastHeader
    {
        private readonly int _size;
        // The device type, which determines the event-specific information that follows the first three members. 
        private readonly DeviceBroadcastDeviceType _deviceType;
        private readonly int _reserved;

        /// <summary>
        /// The device type, which determines the event-specific information that follows the first three members. 
        /// </summary>
        public DeviceBroadcastDeviceType DeviceType => _deviceType;
    }
}
