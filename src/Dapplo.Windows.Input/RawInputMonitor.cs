// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#if !NETSTANDARD2_0
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input;

/// <summary>
/// Reactive access to RawInput
/// </summary>
public static class RawInputMonitor
{
    private static IObservable<RawInputEventArgs> _rawInputObservable;

    /// <summary>
    /// Gets the shared observable for Raw Input.
    /// Multiple subscribers will share the same underlying hook, and the hook 
    /// is automatically disposed when the subscriber count reaches zero.
    /// </summary>
    public static IObservable<RawInputEventArgs> Listen(params RawInputDevices[] devices)
    {
        if (_rawInputObservable != null)
        {
            RawInputApi.RegisterRawInput(SharedMessageWindow.Handle, RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DeviceNotify, devices);
            return _rawInputObservable;
        }

        _rawInputObservable = Observable.Create<RawInputEventArgs>(observer =>
        {
            // Subscribe to the SharedMessageWindow for handling the WM_INPUT
            var messageSubscription = SharedMessageWindow.Messages
                .Where(windowsMessage => windowsMessage.Msg == WindowsMessages.WM_INPUT) // filter for raw input
                .Subscribe(windowsMessage =>
                {
                    windowsMessage.Handled = true;
                    int outSize;
                    int size = Marshal.SizeOf<RawInput>();

                    outSize = RawInputApi.GetRawInputData(windowsMessage.LParam, RawInputDataCommands.Input, out var rawInput, ref size, Marshal.SizeOf<RawInputHeader>());
                    if (outSize != -1)
                    {
                        observer.OnNext(new RawInputEventArgs
                        {
                            IsForeground = (int)windowsMessage.WParam == 0,
                            RawInput = rawInput
                        });
                    }
                });

            RawInputApi.RegisterRawInput(SharedMessageWindow.Handle, RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DeviceNotify, devices);

            // Return the disposal logic
            return Disposable.Create(() =>
            {
                // Unregister raw input devices from the OS here if necessary
                // UnregisterRawInputDevices(...);
                _rawInputObservable = null; // Clear the cached observable so it can be recreated if Listen() is called again.
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
        return _rawInputObservable;
    }
}
#endif