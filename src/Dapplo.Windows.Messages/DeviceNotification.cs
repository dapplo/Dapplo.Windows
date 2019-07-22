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
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;

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
        ///     This observable publishes the current clipboard contents after every paste action.
        ///     Best to use SubscribeOn with the UI SynchronizationContext.
        /// </summary>
        public static IObservable<DeviceNotificationEvent> OnNotification { get; }

        /// <summary>
        ///     Private constructor to create the DeviceNotifications observable
        /// </summary>
        static DeviceNotification()
        {
            OnNotification = Observable.Create<DeviceNotificationEvent>(observer =>
            {
                var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Create();
                var deviceNotifyFlags = DeviceNotifyFlags.WindowHandle | DeviceNotifyFlags.AllInterfaceClasses;

                var deviceNotificationHandle = RegisterDeviceNotification(WinProcHandler.Instance.Handle, devBroadcastDeviceInterface, deviceNotifyFlags);

                if (deviceNotificationHandle == IntPtr.Zero)
                {
                    observer.OnError(new Win32Exception());
                }

                // This handles the message
                IntPtr WinProcClipboardMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var windowsMessage = (WindowsMessages)msg;
                    if (windowsMessage != WindowsMessages.WM_NCDESTROY)
                    {
                        return IntPtr.Zero;
                    }

                    observer.OnNext(new DeviceNotificationEvent
                    {
                        EventType = (DeviceChangeEvent) wParam.ToInt32(),
                        DeviceBroadcastPtr = lParam
                    });

                    return IntPtr.Zero;
                }

                var hook = new WinProcHandlerHook(WinProcClipboardMessageHandler);
                var hookSubscription = WinProcHandler.Instance.Subscribe(hook);
                return hook.Disposable = Disposable.Create(() =>
                {
                    UnregisterDeviceNotification(deviceNotificationHandle);
                    hookSubscription.Dispose();
                });
            })
            // Make sure there is always a value produced when connecting
            .Publish()
            .RefCount();
        }

        /// <summary>
        /// A simplification for on device arrival
        /// </summary>
        /// <returns>IObservable with DeviceInterfaceInfo</returns>
        public static IObservable<DeviceInterfaceChangeInfo> OnDeviceArrival()
        {
            return OnNotification
                .Where(deviceNotificationEvent => deviceNotificationEvent.EventType == DeviceChangeEvent.DeviceArrival)
                .Select(deviceNotificationEvent =>
                    {
                        deviceNotificationEvent.TryGetDeviceBroadcastDeviceType(out var devBroadcastDeviceInterface);
                        return new DeviceInterfaceChangeInfo
                        {
                            EventType = deviceNotificationEvent.EventType,
                            Device = devBroadcastDeviceInterface
                        };
                    });
        }

        /// <summary>
        /// A simplification for on device remove complete
        /// </summary>
        /// <returns>IObservable with DeviceInterfaceInfo</returns>
        public static IObservable<DeviceInterfaceChangeInfo> OnDeviceRemoved()
        {
            return OnNotification
                .Where(deviceNotificationEvent => deviceNotificationEvent.EventType == DeviceChangeEvent.DeviceRemoveComplete)
                .Select(deviceNotificationEvent =>
                {
                    deviceNotificationEvent.TryGetDeviceBroadcastDeviceType(out var devBroadcastDeviceInterface);
                    return new DeviceInterfaceChangeInfo
                    {
                        EventType = deviceNotificationEvent.EventType,
                        Device = devBroadcastDeviceInterface
                    };
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
    }
}

#endif