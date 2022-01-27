// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Devices.Enums;

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