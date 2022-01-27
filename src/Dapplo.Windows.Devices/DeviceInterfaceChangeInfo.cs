// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;

namespace Dapplo.Windows.Devices;

/// <summary>
/// Information on device changes
/// </summary>
public class DeviceInterfaceChangeInfo
{
    /// <summary>
    /// Type of the event
    /// </summary>
    public DeviceChangeEvent EventType { get; internal set; }

    /// <summary>
    /// The already prepared DevBroadcastDeviceInterface
    /// </summary>
    public DevBroadcastDeviceInterface Device { get; internal set; }
}