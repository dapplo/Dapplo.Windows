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
    /// The different known HID usage pages
    /// </summary>
    public enum HidUsagePages : ushort
    {
        /// <summary>
        /// Unknow usage page
        /// </summary>
        Undefined = 0x00,
        /// <summary>
        /// Generic desktop controls
        /// </summary>
        Generic = 0x01,
        /// <summary>
        /// Simulation controls
        /// </summary>
        Simulation = 0x02,
        /// <summary>
        /// Virtual reality controls
        /// </summary>
        // ReSharper disable once InconsistentNaming
        VR = 0x03,
        /// <summary>
        /// Sports controls
        /// </summary>
        Sport = 0x04,
        /// <summary>
        /// Games controls
        /// </summary>
        Game = 0x05,
        /// <summary>
        /// Keyboard controls
        /// </summary>
        Keyboard = 0x07,
        /// <summary>
        /// LED controls.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        LED = 0x08,
        /// <summary>
        /// Button
        /// </summary>
        Button = 0x09,
        /// <summary>
        /// Ordinal
        /// </summary>
        Ordinal = 0x0A,
        /// <summary>
        /// Telephony
        /// </summary>
        Telephony = 0x0B,
        /// <summary>
        /// Consumer
        /// </summary>
        Consumer = 0x0C,
        /// <summary>
        /// Digitizer
        /// </summary>
        Digitizer = 0x0D,
        /// <summary>
        /// Physical interface device
        /// </summary>
        // ReSharper disable once InconsistentNaming
        PID = 0x0F,
        /// <summary>
        /// Unicode
        /// </summary>
        Unicode = 0x10,
        /// <summary>
        /// Alphanumeric display
        /// </summary>
        AlphaNumeric = 0x14,
        /// <summary>
        /// Medical instruments
        /// </summary>
        Medical = 0x40,
        /// <summary>
        /// Monitor page 0
        /// </summary>
        MonitorPage0 = 0x80,
        /// <summary>
        /// Monitor page 1
        /// </summary>
        MonitorPage1 = 0x81,
        /// <summary>
        /// Monitor page 2
        /// </summary>
        MonitorPage2 = 0x82,
        /// <summary>
        /// Monitor page 3
        /// </summary>
        MonitorPage3 = 0x83,
        /// <summary>
        /// Power page 0
        /// </summary>
        PowerPage0 = 0x84,
        /// <summary>
        /// Power page 1
        /// </summary>
        PowerPage1 = 0x85,
        /// <summary>
        /// Power page 2
        /// </summary>
        PowerPage2 = 0x86,
        /// <summary>
        /// Power page 3
        /// </summary>
        PowerPage3 = 0x87,
        /// <summary>
        /// Bar code scanner
        /// </summary>
        BarCode = 0x8C,
        /// <summary>
        /// Scale page
        /// </summary>
        Scale = 0x8D,
        /// <summary>
        /// Magnetic strip reading devices
        /// </summary>
        // ReSharper disable once InconsistentNaming
        MSR = 0x8E,
        /// <summary>
        /// Camera
        /// </summary>
        Camera = 0x90,
        /// <summary>
        /// Arcade
        /// </summary>
        Arcade = 0x91
    }
}
