// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;

namespace Dapplo.Windows.Devices.Structs
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
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
