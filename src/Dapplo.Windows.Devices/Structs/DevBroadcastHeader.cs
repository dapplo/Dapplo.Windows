// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;

namespace Dapplo.Windows.Devices.Structs;

/// <summary>
/// Serves as a standard header for information related to a device event reported through the WM_DEVICECHANGE message.
/// 
/// The members of the DEV_BROADCAST_HDR structure are contained in each device management structure. To determine which structure you have received through WM_DEVICECHANGE, treat the structure as a DEV_BROADCAST_HDR structure and check its dbch_devicetype member.
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