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

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Messages.Structs
{
    /// <summary>
    /// Contains information about a file system handle.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-_dev_broadcast_handle">DEV_BROADCAST_HANDLE structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DevBroadcastHandle
    {
        private int _size;
        // The device type, which determines the event-specific information that follows the first three members. 
        private DeviceBroadcastDeviceType _deviceType;
        private readonly int _reserved;
        private readonly IntPtr _handle;
        // A handle to the device notification. This handle is returned by RegisterDeviceNotification.
        private readonly IntPtr _hdevnotify;
        // The GUID for the custom event. For more information, see Device Events. Valid only for DBT_CUSTOMEVENT.
        private readonly Guid _eventguid;
        private readonly ulong _nameoffset;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
        private readonly byte[] dbch_data;

        /// <summary>
        /// Factory for an empty DevBroadcastHandle
        /// </summary>
        /// <returns>DevBroadcastHandle</returns>
        public static DevBroadcastHandle Create()
        {
            return new DevBroadcastHandle
            {
                _deviceType = DeviceBroadcastDeviceType.Handle,
                _size = Marshal.SizeOf(typeof(DevBroadcastHandle))
            };
        }
    }
}
