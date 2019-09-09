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

using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;

namespace Dapplo.Windows.Devices
{
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
}
