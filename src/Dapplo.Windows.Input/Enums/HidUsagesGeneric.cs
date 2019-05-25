//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

namespace Dapplo.Windows.Input.Enums
{
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
}
