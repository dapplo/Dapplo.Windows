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
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;

namespace Dapplo.Windows.Messages
{
    /// <summary>
    /// Information on device changes
    /// </summary>
    public class DeviceNotificationEvent
    {
        private DevBroadcastHeader _devBroadcastHeader;
        // IntPtr to the device broadcast structure
        private readonly IntPtr _deviceBroadcastPtr;

        /// <summary>
        /// Creates an DeviceNotificationEvent from a WM_DEVICECHANGE message
        /// </summary>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        public DeviceNotificationEvent(IntPtr wParam, IntPtr lParam)
        {
            EventType = (DeviceChangeEvent) wParam.ToInt32();
            _deviceBroadcastPtr = lParam;
            _devBroadcastHeader = Marshal.PtrToStructure<DevBroadcastHeader>(_deviceBroadcastPtr);
        }

        /// <summary>
        /// Type of the event
        /// </summary>
        public DeviceChangeEvent EventType { get; }

        /// <summary>
        /// Test if the message is a certain DeviceBroadcastDeviceType
        /// </summary>
        /// <param name="deviceBroadcastDeviceType">DeviceBroadcastDeviceType</param>
        /// <returns>bool</returns>
        public bool Is(DeviceBroadcastDeviceType deviceBroadcastDeviceType) => _devBroadcastHeader.DeviceType == deviceBroadcastDeviceType;


        /// <summary>
        /// Get the DevBroadcastVolume
        /// </summary>
        /// <param name="devBroadcastVolume">out DevBroadcastVolume</param>
        /// <returns>bool true the value could be converted</returns>
        public bool TryGetDevBroadcastVolume(out DevBroadcastVolume devBroadcastVolume)
        {
            if (_devBroadcastHeader.DeviceType != DeviceBroadcastDeviceType.Volume)
            {
                devBroadcastVolume = default;
                return false;
            }
            devBroadcastVolume = Marshal.PtrToStructure<DevBroadcastVolume>(_deviceBroadcastPtr);
            return true;
        }

        /// <summary>
        /// Get the DevBroadcastDeviceInterface
        /// </summary>
        /// <param name="devBroadcastDeviceInterface">out DevBroadcastDeviceInterface</param>
        /// <returns>bool true the value could be converted</returns>
        public bool TryGetDevBroadcastDeviceInterface(out DevBroadcastDeviceInterface devBroadcastDeviceInterface)
        {
            if (_devBroadcastHeader.DeviceType != DeviceBroadcastDeviceType.DeviceInterface)
            {
                devBroadcastDeviceInterface = default;
                return false;
            }
            devBroadcastDeviceInterface = Marshal.PtrToStructure<DevBroadcastDeviceInterface>(_deviceBroadcastPtr);
            return true;
        }
    }
}
