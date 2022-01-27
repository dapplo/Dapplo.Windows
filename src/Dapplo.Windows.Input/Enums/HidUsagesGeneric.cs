// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums;

/// <summary>
/// The different known generic HID usages
/// See <a href="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf">here</a>
/// </summary>
public enum HidUsagesGeneric : ushort
{
    /// <summary>
    /// Unknow usage
    /// </summary>
    Undefined = 0x00,
    /// <summary>
    /// Pointer
    /// </summary>
    Pointer = 0x01,
    /// <summary>
    /// Mouse
    /// </summary>
    Mouse = 0x02,
    /// <summary>
    /// Joystick
    /// </summary>
    Joystick = 0x04,
    /// <summary>
    /// Game Pad
    /// </summary>
    Gamepad = 0x05,
    /// <summary>
    /// Keyboard
    /// </summary>
    Keyboard = 0x06,
    /// <summary>
    /// Keypad
    /// </summary>
    Keypad = 0x07,
    /// <summary>
    /// Multi-Axis
    /// </summary>
    MultiAxis = 0x08,
    /// <summary>
    /// Tablet PC
    /// </summary>
    Tablet = 0x09,
    /// <summary>
    /// Consumer
    /// </summary>
    Consumer = 0x0C,
    /// <summary>
    /// X
    /// </summary>
    X = 0x30,
    /// <summary>
    /// Y
    /// </summary>
    Y = 0x31,
    /// <summary>
    /// Z
    /// </summary>
    Z = 0x32,
    /// <summary>
    /// Rx
    /// </summary>
    Rx = 0x33,
    /// <summary>
    /// Ry
    /// </summary>
    Ry = 0x34,
    /// <summary>
    /// Rz
    /// </summary>
    Rz = 0x35,
    /// <summary>
    /// Slider
    /// </summary>
    Slider = 0x36,
    /// <summary>
    /// Dial
    /// </summary>
    Dial = 0x37,
    /// <summary>
    /// Wheel
    /// </summary>
    Wheel = 0x38,
    /// <summary>
    /// Hat switch
    /// </summary>
    HatSwitch = 0x39,
    /// <summary>
    /// Counted buffer
    /// </summary>
    CountedBuffer = 0x3a,
    /// <summary>
    /// Byte count
    /// </summary>
    ByteCount = 0x3b,
    /// <summary>
    /// Motion Wakeup
    /// </summary>
    MotionWakeup = 0x3c,
    /// <summary>
    /// Start
    /// </summary>
    Start = 0x3d,
    /// <summary>
    /// Select
    /// </summary>
    Select = 0x3E,
    /// <summary>
    /// Muilt-axis Controller
    /// </summary>
    SystemControl = 0x80
}