// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums;

/// <summary>
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645597.aspx">GetRawInputDeviceInfo function</a>
/// </summary>
public enum RawInputDeviceInfoCommands : uint
{
    /// <summary>
    /// pData points to a string that contains the device name.
    /// For this uiCommand only, the value in pcbSize is the character count (not the byte count)
    /// </summary>
    DeviceName = 0x20000007,
    /// <summary>
    /// pData points to an RID_DEVICE_INFO structure.
    /// </summary>
    DeviceInfo = 0x2000000b,
    /// <summary>
    /// pData points to the previously parsed data.
    /// </summary>
    PreparsedData = 0x20000005
}