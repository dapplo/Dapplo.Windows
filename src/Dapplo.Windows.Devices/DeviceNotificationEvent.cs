// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;

namespace Dapplo.Windows.Devices;

/// <summary>
/// Information on device changes
/// </summary>
public class DeviceNotificationEvent
{
    private DevBroadcastHeader _devBroadcastHeader;
    // IntPtr to the device broadcast structure
    private readonly IntPtr _deviceBroadcastPtr;

    /// <summary>
    /// Creates an DeviceNotificationEvent from a WM_DEVICECHANGE message
    /// </summary>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    public DeviceNotificationEvent(IntPtr wParam, IntPtr lParam)
    {
        EventType = (DeviceChangeEvent) wParam.ToInt32();
        _deviceBroadcastPtr = lParam;
        _devBroadcastHeader = Marshal.PtrToStructure<DevBroadcastHeader>(_deviceBroadcastPtr);
    }

    /// <summary>
    /// Type of the event
    /// </summary>
    public DeviceChangeEvent EventType { get; }

    /// <summary>
    /// Test if the message is a certain DeviceBroadcastDeviceType
    /// </summary>
    /// <param name="deviceBroadcastDeviceType">DeviceBroadcastDeviceType</param>
    /// <returns>bool</returns>
    public bool Is(DeviceBroadcastDeviceType deviceBroadcastDeviceType) => _devBroadcastHeader.DeviceType == deviceBroadcastDeviceType;


    /// <summary>
    /// Get the DevBroadcastVolume
    /// </summary>
    /// <param name="devBroadcastVolume">out DevBroadcastVolume</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastVolume(out DevBroadcastVolume devBroadcastVolume)
    {
        if (_devBroadcastHeader.DeviceType != DeviceBroadcastDeviceType.Volume)
        {
            devBroadcastVolume = default;
            return false;
        }
        devBroadcastVolume = Marshal.PtrToStructure<DevBroadcastVolume>(_deviceBroadcastPtr);
        return true;
    }

    /// <summary>
    /// Get the DevBroadcastDeviceInterface
    /// </summary>
    /// <param name="devBroadcastDeviceInterface">out DevBroadcastDeviceInterface</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastDeviceInterface(out DevBroadcastDeviceInterface devBroadcastDeviceInterface)
    {
        if (_devBroadcastHeader.DeviceType != DeviceBroadcastDeviceType.DeviceInterface)
        {
            devBroadcastDeviceInterface = default;
            return false;
        }
        devBroadcastDeviceInterface = Marshal.PtrToStructure<DevBroadcastDeviceInterface>(_deviceBroadcastPtr);
        return true;
    }
}