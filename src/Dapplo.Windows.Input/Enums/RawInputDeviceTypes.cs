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
}
