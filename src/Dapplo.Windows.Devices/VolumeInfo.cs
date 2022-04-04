// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;

namespace Dapplo.Windows.Devices;

/// <summary>
/// Information on a volume change
/// </summary>
public class VolumeInfo
{
    /// <summary>
    /// Type of the event
    /// </summary>
    public DeviceChangeEvent EventType { get; internal set; }

    /// <summary>
    /// The volume that was added / removed
    /// </summary>
    public DevBroadcastVolume Volume { get; internal set; }
}