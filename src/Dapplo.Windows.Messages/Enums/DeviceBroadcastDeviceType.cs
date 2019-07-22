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

namespace Dapplo.Windows.Messages.Enums
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-_dev_broadcast_hdr">DEV_BROADCAST_HDR structure</a>
    /// </summary>
    public enum DeviceBroadcastDeviceType : uint
    {
        /// <summary>
        /// DBT_DEVTYP_OEM: OEM- or IHV-defined device type. This structure is a DEV_BROADCAST_OEM structure.
        /// </summary>
        Oem = 0,
        /// <summary>
        /// DBT_DEVTYP_VOLUME: Logical volume. This structure is a DEV_BROADCAST_VOLUME structure.
        /// </summary>
        Volume = 2,
        /// <summary>
        /// DBT_DEVTYP_PORT: Port device (serial or parallel). This structure is a DEV_BROADCAST_PORT structure.
        /// </summary>
        Port = 3,
        /// <summary>
        /// DBT_DEVTYP_DEVICEINTERFACE: Class of devices. This structure is a DEV_BROADCAST_DEVICEINTERFACE structure.
        /// </summary>
        DeviceInterface = 5,
        /// <summary>
        /// DBT_DEVTYP_HANDLE: File system handle. This structure is a DEV_BROADCAST_HANDLE structure.
        /// </summary>
        Handle = 6

    }
}
