// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input;

/// <summary>
/// Reactive access to RawInput device information and changes.
/// This class provides an observable stream of device change events, and a cache of currently known devices.
/// </summary>
public static class RawInputDeviceMonitor
{
    private static readonly Dictionary<IntPtr, RawInputDeviceInformation> DeviceCache = [];

    /// <summary>
    /// The raw-input devices currently in the system
    /// </summary>
    public static IReadOnlyDictionary<IntPtr, RawInputDeviceInformation> Devices { get; } = new ReadOnlyDictionary<IntPtr, RawInputDeviceInformation>(DeviceCache);

    // Cache the internal IObservable, so subsequent calls to Listen() will return the same observable and share the same underlying hook.

    private static IObservable<RawInputDeviceChangeEventArgs> _rawInputDeviceObservable;

    /// <summary>
    /// Gets the shared observable for Raw Input.
    /// Multiple subscribers will share the same underlying hook, and the hook 
    /// is automatically disposed when the subscriber count reaches zero.
    /// </summary>
    public static IObservable<RawInputDeviceChangeEventArgs> Listen(params RawInputDevices[] devices)
    {
        if (_rawInputDeviceObservable != null)
        {
            RawInputApi.RegisterRawInput(SharedMessageWindow.Handle, RawInputDeviceFlags.DeviceNotify, devices);
            return _rawInputDeviceObservable;
        }
        
        _rawInputDeviceObservable = Observable.Create<RawInputDeviceChangeEventArgs>(observer =>
        {
            // Subscribe to the SharedMessageWindow
            var messageSubscription = SharedMessageWindow.Messages
                .Where(windowsMessage => windowsMessage.Msg == WindowsMessages.WM_INPUT_DEVICE_CHANGE) // filter for raw input devuce change messages
                .Subscribe(windowsMessage =>
                {
                    windowsMessage.Handled = true;
                    bool isNew = (int)windowsMessage.WParam == 1;
                    IntPtr deviceHandle = windowsMessage.LParam;
                    // Add to cache
                    if (isNew)
                    {
                        DeviceCache[deviceHandle] = RawInputApi.GetDeviceInformation(deviceHandle);
                    }
                    observer.OnNext(new RawInputDeviceChangeEventArgs
                    {
                        Added = isNew,
                        DeviceInformation = DeviceCache[deviceHandle]
                    });
                    // Remove from cache
                    if (!isNew)
                    {
                        DeviceCache.Remove(deviceHandle);
                    }
                });
            // Now register to the OS for raw input device notifications. This will trigger the WM_INPUT_DEVICE_CHANGE messages that we are subscribed to above.
            RawInputApi.RegisterRawInput(SharedMessageWindow.Handle, RawInputDeviceFlags.DeviceNotify, devices);

            // 4. Return the disposal logic
            return Disposable.Create(() =>
            {
                // Unregister raw input devices from the OS here if necessary
                // UnregisterRawInputDevices(...);

                _rawInputDeviceObservable = null; // Clear the cached observable so it can be recreated if Listen() is called again.
                // Dispose the SharedMessageWindow subscription
                messageSubscription.Dispose();
            });
        })
        // This is the magic part:
        // .Publish() multicasts the observable to all subscribers.
        // .RefCount() keeps track of subscribers and automatically disposes 
        // the inner subscription when the count reaches 0.
        .Publish()
        .RefCount();
        return _rawInputDeviceObservable;
    }
}
#endif