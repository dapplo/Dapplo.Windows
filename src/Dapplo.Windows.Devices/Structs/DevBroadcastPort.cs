// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;

namespace Dapplo.Windows.Devices.Structs;

/// <summary>
/// Contains information about a modem, serial, or parallel port.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-dev_broadcast_port_w">DEV_BROADCAST_PORT_W structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DevBroadcastPort
{
    private int _size;
    // The device type, which determines the event-specific information that follows the first three members. 
    private DeviceBroadcastDeviceType _deviceType;
    private readonly int _reserved;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    private readonly string _name;

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Factory for an empty DevBroadcastPort
    /// </summary>
    /// <returns>DevBroadcastPort</returns>
    public static DevBroadcastPort Create()
    {
        return new DevBroadcastPort
        {
            _deviceType = DeviceBroadcastDeviceType.Port,
            _size = Marshal.SizeOf(typeof(DevBroadcastPort))
        };
    }
}