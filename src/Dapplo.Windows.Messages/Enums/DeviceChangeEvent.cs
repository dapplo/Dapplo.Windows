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

namespace Dapplo.Windows.Messages.Enums
{
    /// <summary>
    /// These are the possible device change values, as described in  the <a href="https://docs.microsoft.com/en-us/windows/win32/devio/device-management-events">Device Management Events</a>
    /// </summary>
    public enum DeviceChangeEvent : uint
    {
        /// <summary>
        /// The system broadcasts the DBT_DEVNODES_CHANGED device event when a device has been added to or removed from the system.
        /// Applications that maintain lists of devices in the system should refresh their lists.
        /// </summary>
        DevNodesChanged = 0x0007,

        /// <summary>
        /// The system broadcasts the DBT_QUERYCHANGECONFIG device event to request permission to change the current configuration (dock or undock).
        /// Any application can deny this request and cancel the change.
        /// </summary>
        QueryChangeConfig = 0x0017,

        /// <summary>
        /// The system broadcasts the DBT_CONFIGCHANGED device event to indicate that the current configuration has changed, due to a dock or undock.
        /// An application or driver that stores data in the registry under the HKEY_CURRENT_CONFIG key should update the data.
        /// </summary>
        ConfigChanged = 0x0018,

        /// <summary>
        /// The system broadcasts the DBT_CONFIGCHANGECANCELED device event when a request to change the current configuration (dock or undock) has been canceled.
        /// </summary>
        ConfigChangeCanceled = 0x0019,

        /// <summary>
        /// The system broadcasts the DBT_DEVICEARRIVAL device event when a device or piece of media has been inserted and becomes available.
        /// </summary>
        DeviceArrival = 0x8000,

        /// <summary>
        /// The system broadcasts the DBT_DEVICEQUERYREMOVE device event to request permission to remove a device or piece of media.
        /// This message is the last chance for applications and drivers to prepare for this removal.
        /// However, any application can deny this request and cancel the operation.
        /// </summary>
        DeviceQueryRemove = 0x8001,

        /// <summary>
        /// The system broadcasts the DBT_DEVICEQUERYREMOVEFAILED device event when a request to remove a device or piece of media has been canceled.
        /// </summary>
        DeviceQueryRemoveFailed = 0x8002,

        /// <summary>
        /// The system broadcasts the DBT_DEVICEREMOVEPENDING device event when a device or piece of media is being removed and is no longer available for use.
        /// </summary>
        DeviceRemovePending = 0x8003,

        /// <summary>
        /// The system broadcasts the DBT_DEVICEREMOVECOMPLETE device event when a device or piece of media has been physically removed.
        /// </summary>
        DeviceRemoveComplete = 0x8004,

        /// <summary>
        /// The system broadcasts the DBT_DEVICETYPESPECIFIC device event when a device-specific event occurs.
        /// </summary>
        DeviceTypeSpecific = 0x8005,
        /// <summary>
        /// The system sends the DBT_CUSTOMEVENT device event when a driver-defined custom event has occurred.
        /// </summary>
        CustomEvent = 0x8006,

        /// <summary>
        /// The DBT_USERDEFINED device event identifies a user-defined event.
        /// </summary>
        UserDefined = 0xFFFF
    }
}
