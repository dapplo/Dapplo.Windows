// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;

namespace Dapplo.Windows.Devices;

/// <summary>
/// Information on device changes, a copy of the information of a WM_DEVICECHANGE message.
/// </summary>
/// <remarks>
/// The DEV_BROADCAST_* structure which lParam points to is only valid while the message is processed,
/// therefore the constructor copies everything which is needed. The instance can be used on any thread and at any time later.
/// </remarks>
public class DeviceNotificationEvent
{
    private readonly bool _hasDevBroadcast;
    private readonly DeviceBroadcastDeviceType _deviceType;
    private readonly DevBroadcastVolume _devBroadcastVolume;
    private readonly DevBroadcastDeviceInterface _devBroadcastDeviceInterface;
    private readonly DevBroadcastPort _devBroadcastPort;
    private readonly DevBroadcastHandle _devBroadcastHandle;

    /// <summary>
    /// Creates an DeviceNotificationEvent from a WM_DEVICECHANGE message.
    /// This must be called synchronously while the message is processed (inside OnNext on the window thread), as lParam is only valid at that time.
    /// </summary>
    /// <param name="wParam">IntPtr with the wParam of the WM_DEVICECHANGE, the DeviceChangeEvent</param>
    /// <param name="lParam">IntPtr with the lParam of the WM_DEVICECHANGE, a pointer to a DEV_BROADCAST_* structure or 0</param>
    public DeviceNotificationEvent(IntPtr wParam, IntPtr lParam)
    {
        EventType = unchecked((DeviceChangeEvent)(int)wParam.ToInt64());
        if (lParam == IntPtr.Zero)
        {
            return;
        }

        var devBroadcastHeader = Marshal.PtrToStructure<DevBroadcastHeader>(lParam);
        _hasDevBroadcast = true;
        _deviceType = devBroadcastHeader.DeviceType;
        switch (_deviceType)
        {
            case DeviceBroadcastDeviceType.Volume:
                _devBroadcastVolume = Marshal.PtrToStructure<DevBroadcastVolume>(lParam);
                break;
            case DeviceBroadcastDeviceType.DeviceInterface:
                _devBroadcastDeviceInterface = DevBroadcastDeviceInterface.FromNative(lParam);
                break;
            case DeviceBroadcastDeviceType.Port:
                _devBroadcastPort = DevBroadcastPort.FromNative(lParam);
                break;
            case DeviceBroadcastDeviceType.Handle:
                _devBroadcastHandle = Marshal.PtrToStructure<DevBroadcastHandle>(lParam);
                break;
        }
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
    public bool Is(DeviceBroadcastDeviceType deviceBroadcastDeviceType) => _hasDevBroadcast && _deviceType == deviceBroadcastDeviceType;

    /// <summary>
    /// Get the DevBroadcastVolume
    /// </summary>
    /// <param name="devBroadcastVolume">out DevBroadcastVolume</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastVolume(out DevBroadcastVolume devBroadcastVolume)
    {
        devBroadcastVolume = _devBroadcastVolume;
        return Is(DeviceBroadcastDeviceType.Volume);
    }

    /// <summary>
    /// Get the DevBroadcastDeviceInterface
    /// </summary>
    /// <param name="devBroadcastDeviceInterface">out DevBroadcastDeviceInterface</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastDeviceInterface(out DevBroadcastDeviceInterface devBroadcastDeviceInterface)
    {
        devBroadcastDeviceInterface = _devBroadcastDeviceInterface;
        return Is(DeviceBroadcastDeviceType.DeviceInterface);
    }

    /// <summary>
    /// Get the DevBroadcastPort
    /// </summary>
    /// <param name="devBroadcastPort">out DevBroadcastPort</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastPort(out DevBroadcastPort devBroadcastPort)
    {
        devBroadcastPort = _devBroadcastPort;
        return Is(DeviceBroadcastDeviceType.Port);
    }

    /// <summary>
    /// Get the DevBroadcastHandle
    /// </summary>
    /// <param name="devBroadcastHandle">out DevBroadcastHandle</param>
    /// <returns>bool true the value could be converted</returns>
    public bool TryGetDevBroadcastHandle(out DevBroadcastHandle devBroadcastHandle)
    {
        devBroadcastHandle = _devBroadcastHandle;
        return Is(DeviceBroadcastDeviceType.Handle);
    }
}
