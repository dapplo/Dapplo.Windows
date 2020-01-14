// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Devices.Enums
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerdevicenotificationw">RegisterDeviceNotificationW function</a>
    /// </summary>
    [Flags]
    public enum DeviceNotifyFlags : uint
    {
        /// <summary>
        /// The hRecipient parameter is a window handle.
        /// </summary>
        WindowHandle = 0x00000000,

        /// <summary>
        /// The hRecipient parameter is a service status handle.
        /// </summary>
        ServiceHandle = 0x00000001,

        /// <summary>
        /// Notifies the recipient of device interface events for all device interface classes. (The dbcc_classguid member is ignored.)
        /// This value can be used only if the dbch_devicetype member is DBT_DEVTYP_DEVICEINTERFACE.
        /// </summary>
        AllInterfaceClasses = 0x00000004,
    }
}
