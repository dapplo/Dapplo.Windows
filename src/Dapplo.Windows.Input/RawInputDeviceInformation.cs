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

using System;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input
{
    /// <summary>
    /// Desribes a RawInput device
    /// </summary>
    public class RawInputDeviceInformation
    {
        /// <summary>
        /// The handle to the raw input device
        /// </summary>
        public IntPtr Handle { get; internal set; }

        /// <summary>
        /// The cryptic device name
        /// </summary>
        public string DeviceName { get; internal set; }

        /// <summary>
        /// A name which can be used to display to a user
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// The actual device information
        /// </summary>
        public RawInputDeviceInfo DeviceInfo { get; internal set; }
    }
}
