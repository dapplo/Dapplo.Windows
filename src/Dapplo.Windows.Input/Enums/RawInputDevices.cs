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
    /// This is used for mapping a device to Usage Page and Usage, to register for RawInput
    /// </summary>
    public enum RawInputDevices
    {
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage:  0x01
        /// Hardware ID: HID_DEVICE_SYSTEM_MOUSE
        /// </summary>
        Pointer,
        /// <summary>
        /// Mouse
        /// UsagePage: 0x01
        /// Usage: 0x02
        /// Hardware ID: HID_DEVICE_SYSTEM_MOUSE
        /// </summary>
        Mouse,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage: 0x04
        /// Hardware ID: HID_DEVICE_SYSTEM_GAME
        /// </summary>
        Joystick,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage: 0x05
        /// Hardware ID: HID_DEVICE_SYSTEM_GAME
        /// </summary>
        GamePad,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage:  0x06
        /// Hardware ID: HID_DEVICE_SYSTEM_KEYBOARD
        /// </summary>
        Keyboard,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage: 0x07
        /// Hardware ID: HID_DEVICE_SYSTEM_KEYBOARD
        /// </summary>
        Keypad,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x01
        /// Usage: 0x80
        /// Hardware ID: HID_DEVICE_SYSTEM_CONTROL
        /// </summary>
        SystemControl,
        /// <summary>
        /// Pointer
        /// UsagePage: 0x0C
        /// Usage: 0x01
        /// Hardware ID: HID_DEVICE_SYSTEM_CONSUMER
        /// </summary>
        ConsumerAudioControl
    }
}
