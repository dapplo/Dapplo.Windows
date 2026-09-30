// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Native;

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
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        /// <summary>
        ///     This observable publishes a DeviceNotificationEvent for every WM_DEVICECHANGE, for all device interface classes.
        ///     The events are produced on the thread of the SharedMessageWindow, use ObserveOn with the UI SynchronizationContext to process them on the UI thread.
        ///     The DeviceNotificationEvent is a copy of the information, so it can be used after the message was processed.
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
                        deviceNotifyFlags |= DeviceNotifyFlags.AllInterfaceClasses;
                    }

                    IntPtr deviceNotificationHandle = IntPtr.Zero;

                    return SharedMessageWindow.Listen(
                        onSetup: hwnd =>
                        {
                            deviceNotificationHandle = RegisterDeviceNotification((IntPtr)hwnd, devBroadcastDeviceInterface, deviceNotifyFlags);
                            if (deviceNotificationHandle == IntPtr.Zero)
                            {
                                // Listen passes this to the subscriber with OnError, and doesn't call onTeardown
                                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterDeviceNotification failed");
                            }
                        },
                        onTeardown: hwnd =>
                        {
                            if (deviceNotificationHandle != IntPtr.Zero)
                            {
                                UnregisterDeviceNotification(deviceNotificationHandle);
                                deviceNotificationHandle = IntPtr.Zero;
                            }
                        }
                    )
                    .Where(m => m.Msg == WindowsMessages.WM_DEVICECHANGE && m.LParam != 0)
                    .Subscribe(m =>
                    {
                        // lParam is only valid while the message is processed: DeviceNotificationEvent copies everything synchronously here
                        observer.OnNext(new DeviceNotificationEvent((IntPtr)m.WParam, (IntPtr)m.LParam));
                    }, observer.OnError, observer.OnCompleted);
                })
                // Share one registration between all subscribers, it is removed when the last subscriber disposes
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
