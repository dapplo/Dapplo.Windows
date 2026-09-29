// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
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
    // The native field is variable length, events are read with FromNative which copies the complete name
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    private string _name;

    /// <summary>
    /// The name of the port, e.g. COM1 or LPT1.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Copy a DEV_BROADCAST_PORT_W from native memory, including the complete (variable length) name.
    /// This must be called while the native memory is valid, e.g. synchronously while processing WM_DEVICECHANGE.
    /// </summary>
    /// <param name="devBroadcastPtr">IntPtr to a DEV_BROADCAST_PORT_W</param>
    /// <returns>DevBroadcastPort</returns>
    internal static DevBroadcastPort FromNative(IntPtr devBroadcastPtr)
    {
        var size = Marshal.ReadInt32(devBroadcastPtr);
        return new DevBroadcastPort
        {
            _size = size,
            _deviceType = DeviceBroadcastDeviceType.Port,
            // dbcp_name follows the 3 DWORDs of the header
            _name = DevBroadcastDeviceInterface.ReadNativeString(devBroadcastPtr, 3 * sizeof(int), size)
        };
    }

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