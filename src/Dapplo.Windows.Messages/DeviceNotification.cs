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

#if !NETSTANDARD2_0
using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;
using Microsoft.Win32;

namespace Dapplo.Windows.Messages
{
    /// <summary>
    /// 
    /// </summary>
    public static class DeviceNotification
    {
        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerdevicenotificationw">RegisterDeviceNotificationW function</a>
        /// Registers the device or type of device for which a window will receive notifications.
        /// </summary>
        /// <param name="hRecipient">IntPtr</param>
        /// <param name="notificationFilter">DevBroadcastDeviceInterface</param>
        /// <param name="flags">DeviceNotifyFlags</param>
        /// <returns>IntPtr with the device notification handle</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, in DevBroadcastDeviceInterface notificationFilter, DeviceNotifyFlags flags);

        /// <summary>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unregisterdevicenotification">UnregisterDeviceNotification function</a>
        /// </summary>
        /// <param name="handle">IntPtr with device notification handle from RegisterDeviceNotification</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        /// <summary>
        ///     Create an observable for devices for the specified window
        /// </summary>
        public static IObservable<DeviceNotificationEvent> DeviceNotifications(this Window window)
        {
            // TODO: Change to service handle, instead of WindowHandle which would prevent a lot of window complexity
            return WinProcWindowsExtensions.WinProcMessages(window, null, hWnd => {
                    var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Create();
                    var deviceNotifyFlags = DeviceNotifyFlags.WindowHandle | DeviceNotifyFlags.AllInterfaceClasses;

                    var deviceNotificationHandle = RegisterDeviceNotification(hWnd, devBroadcastDeviceInterface, deviceNotifyFlags);
                    return deviceNotificationHandle;
                }, deviceNotificationHandle => UnregisterDeviceNotification(deviceNotificationHandle))
                .Where(windowMessageInfo => windowMessageInfo.Message == WindowsMessages.WM_DEVICECHANGE)
                .Select(info => new DeviceNotificationEvent
                {
                    EventType = (DeviceChangeEvent)info.WordParam.ToInt32(),
                    DeviceBroadcastPtr = info.LongParam
                });
        }

        /// <summary>
        /// Get the DevBroadcastDeviceInterface
        /// </summary>
        /// <param name="deviceNotificationEvent">DeviceNotificationEvent</param>
        /// <param name="devBroadcastDeviceInterface">out DevBroadcastDeviceInterface</param>
        /// <returns>bool true the value could be converted</returns>
        public static bool TryGetDeviceBroadcastDeviceType(this DeviceNotificationEvent deviceNotificationEvent, out DevBroadcastDeviceInterface devBroadcastDeviceInterface)
        {
            var header = Marshal.PtrToStructure<DevBroadcastHeader>(deviceNotificationEvent.DeviceBroadcastPtr);
            if (header.DeviceType != DeviceBroadcastDeviceType.DeviceInterface)
            {
                devBroadcastDeviceInterface = default;
                return false;
            }
            devBroadcastDeviceInterface = Marshal.PtrToStructure<DevBroadcastDeviceInterface>(deviceNotificationEvent.DeviceBroadcastPtr);
            return true;
        }

        /// <summary>
        /// Generate a friendly device name from a DevBroadcastDeviceInterface
        /// </summary>
        /// <param name="devBroadcastDeviceInterface">DevBroadcastDeviceInterface</param>
        /// <returns>string</returns>
        public static string FriendlyDeviceName(this DevBroadcastDeviceInterface devBroadcastDeviceInterface)
        {
            string[] parts = devBroadcastDeviceInterface.Name.Split('#');
            if (parts.Length < 3)
            {
                return devBroadcastDeviceInterface.Name;
            }

            string devType = parts[0].Substring(parts[0].IndexOf(@"?\", StringComparison.Ordinal) + 2);
            string deviceInstanceId = parts[1];
            string deviceUniqueId = parts[2];
            string regPath = @"SYSTEM\CurrentControlSet\Enum\" + devType + "\\" + deviceInstanceId + "\\" + deviceUniqueId;
            using (var key = Registry.LocalMachine.OpenSubKey(regPath))
            {
                if (key == null)
                {
                    return devBroadcastDeviceInterface.Name;
                }

                if (key.GetValue("FriendlyName") is string result)
                {
                    return result;
                }
                result = key.GetValue("DeviceDesc") as string;
                if (result != null)
                {
                    // Example: @msclmd.inf,%scmspivcarddevicename%;Identifizierungsgerät (NIST SP 800-73 [PIV])
                    var semiColonIndex = result.LastIndexOf(';');
                    if (semiColonIndex >= 0)
                    {
                        return result.Substring(semiColonIndex + 1);
                    }
                    return result;
                }
            }
            return devBroadcastDeviceInterface.Name;
        }
    }
}

#endif