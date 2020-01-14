// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;

#if !NETSTANDARD2_0

namespace Dapplo.Windows.Devices
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
        ///     Private constructor to create the default DeviceNotifications observable
        /// </summary>
        static DeviceNotification()
        {
            OnNotification = CreateDeviceNotificationObservable();
        }

        /// <summary>
        /// Create a specific DeviceNotification IObservable
        /// </summary>
        /// <param name="deviceInterfaceClass">DeviceInterfaceClass</param>
        /// <returns>IObservable of DeviceNotificationEvent</returns>
        public static IObservable<DeviceNotificationEvent> CreateDeviceNotificationObservable(DeviceInterfaceClass deviceInterfaceClass = DeviceInterfaceClass.Unknown)
        {
            if (deviceInterfaceClass == DeviceInterfaceClass.Unknown && OnNotification != null)
            {
                return OnNotification;
            }

            return Observable.Create<DeviceNotificationEvent>(observer =>
                {
                    var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Create();

                    var deviceNotifyFlags = DeviceNotifyFlags.WindowHandle;
                    // Use the specified class
                    if (deviceInterfaceClass != DeviceInterfaceClass.Unknown)
                    {
                        devBroadcastDeviceInterface.DeviceClass = deviceInterfaceClass;
                    }
                    else
                    {
                        // React to all interface classes
                        deviceNotifyFlags = deviceNotifyFlags | DeviceNotifyFlags.AllInterfaceClasses;
                    }

                    var deviceNotificationHandle = RegisterDeviceNotification(WinProcHandler.Instance.Handle, devBroadcastDeviceInterface, deviceNotifyFlags);

                    if (deviceNotificationHandle == IntPtr.Zero)
                    {
                        observer.OnError(new Win32Exception());
                    }

                    // This handles the message
                    IntPtr WinProcClipboardMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        var windowsMessage = (WindowsMessages)msg;
                        if (windowsMessage != WindowsMessages.WM_DEVICECHANGE || lParam == IntPtr.Zero)
                        {
                            return IntPtr.Zero;
                        }

                        observer.OnNext(new DeviceNotificationEvent(wParam, lParam));

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
        /// <param name="observable">Optional IObservable</param>
        /// <returns>IObservable with DeviceInterfaceInfo</returns>
        public static IObservable<DeviceInterfaceChangeInfo> OnDeviceArrival(IObservable<DeviceNotificationEvent> observable = null)
        {
            
            return (observable ?? OnNotification)
                .Where(deviceNotificationEvent => deviceNotificationEvent.EventType == DeviceChangeEvent.DeviceArrival && deviceNotificationEvent.Is(DeviceBroadcastDeviceType.DeviceInterface))
                .Select(deviceNotificationEvent =>
                    {
                        deviceNotificationEvent.TryGetDevBroadcastDeviceInterface(out var devBroadcastDeviceInterface);
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
        /// <param name="observable">Optional IObservable</param>
        /// <returns>IObservable with DeviceInterfaceInfo</returns>
        public static IObservable<DeviceInterfaceChangeInfo> OnDeviceRemoved(IObservable<DeviceNotificationEvent> observable = null)
        {
            return (observable ?? OnNotification)
                .Where(deviceNotificationEvent => deviceNotificationEvent.EventType == DeviceChangeEvent.DeviceRemoveComplete && deviceNotificationEvent.Is(DeviceBroadcastDeviceType.DeviceInterface))
                .Select(deviceNotificationEvent =>
                {
                    deviceNotificationEvent.TryGetDevBroadcastDeviceInterface(out var devBroadcastDeviceInterface);
                    return new DeviceInterfaceChangeInfo
                    {
                        EventType = deviceNotificationEvent.EventType,
                        Device = devBroadcastDeviceInterface
                    };
                });
        }

        /// <summary>
        /// Get all Volume changes
        /// </summary>
        /// <param name="observable">Optional IObservable</param>
        /// <returns>IObservable with VolumeInfo</returns>
        public static IObservable<VolumeInfo> OnVolumeChanges(IObservable<DeviceNotificationEvent> observable = null)
        {
            return (observable ?? OnNotification)
                .Where(deviceNotificationEvent => deviceNotificationEvent.Is(DeviceBroadcastDeviceType.Volume))
                .Select(deviceNotificationEvent =>
                {
                    deviceNotificationEvent.TryGetDevBroadcastVolume(out var devBroadcastVolume);
                    return new VolumeInfo
                    {
                        EventType = deviceNotificationEvent.EventType,
                        Volume = devBroadcastVolume
                    };
                });
        }

        /// <summary>
        /// A simplification for volume added
        /// </summary>
        /// <param name="observable">Optional IObservable</param>
        /// <returns>IObservable with VolumeInfo</returns>
        public static IObservable<VolumeInfo> OnVolumeAdded(IObservable<DeviceNotificationEvent> observable = null)
        {
            return OnVolumeChanges(observable)
                .Where(volumeInfo => volumeInfo.EventType == DeviceChangeEvent.DeviceArrival);
        }

        /// <summary>
        /// A simplification for volume removed
        /// </summary>
        /// <param name="observable">Optional IObservable</param>
        /// <returns>IObservable with VolumeInfo</returns>
        public static IObservable<VolumeInfo> OnVolumeRemoved(IObservable<DeviceNotificationEvent> observable = null)
        {
            return OnVolumeChanges(observable)
                .Where(volumeInfo => volumeInfo.EventType == DeviceChangeEvent.DeviceRemoveComplete);
        }
    }
}

#endif