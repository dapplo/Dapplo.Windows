// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums;

/// <summary>
/// The type of device, using in the RAWINPUTDEVICELIST
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645568.aspx">RAWINPUTDEVICELIST structure</a>
/// </summary>
public enum RawInputDeviceTypes : uint
{
    /// <summary>
    /// RIM_TYPEMOUSE: Specified device is a mouse
    /// </summary>
    Mouse = 0,
    /// <summary>
    /// RIM_TYPEKEYBOARD: Specified device is a keyboard
    /// </summary>
    Keyboard = 1,
    /// <summary>
    /// RIM_TYPEHID: Specified device is not a mouse or a keyboard
    /// </summary>
    // ReSharper disable once InconsistentNaming
    HID = 2
}