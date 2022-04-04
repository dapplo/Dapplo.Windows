// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums;

/// <summary>
/// The different consumer HID usages
/// See <a href="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf">here</a>
/// </summary>
public enum HidUsagesConsumer : ushort
{
    /// <summary>
    /// Unknow usage
    /// </summary>
    Undefined = 0x00,
    /// <summary>
    /// Consumer Control
    /// </summary>
    ConsumerControl = 0x01
}