// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Input
{
    /// <summary>
    /// Reactive access to RawInput
    /// </summary>
    public static class RawInputMonitor
    {
        private static readonly Dictionary<IntPtr, RawInputDeviceInformation> DeviceCache = new Dictionary<IntPtr, RawInputDeviceInformation>();
        private static readonly ISubject<RawInputDeviceChangeEventArgs> DeviceChangeSubject = new Subject<RawInputDeviceChangeEventArgs>();
        private static readonly ISubject<RawInputEventArgs> RawInputSubject = new Subject<RawInputEventArgs>();

        static RawInputMonitor()
        {
            WinProcHandler.Instance.Subscribe(new WinProcHandlerHook(HandleRawInputMessages));
        }

        /// <summary>
        /// An observable which can be subscribed to be informed of device changes.
        /// </summary>
        public static IObservable<RawInputDeviceChangeEventArgs> OnDeviceChanges => DeviceChangeSubject;

        /// <summary>
        /// An observable which can be subscribed to be informed of raw input.
        /// </summary>
        public static IObservable<RawInputEventArgs> OnRawInput => RawInputSubject;

        /// <summary>
        /// The raw-input devices currently in the system
        /// </summary>
        public static IReadOnlyDictionary<IntPtr, RawInputDeviceInformation> Devices { get; } = new ReadOnlyDictionary<IntPtr, RawInputDeviceInformation>(DeviceCache);

        /// <summary>
        /// A local function which handles the RawInput messages
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="msg">int</param>
        /// <param name="wParam">IntPtr</param>
        /// <param name="lParam">IntPtr</param>
        /// <param name="handled">ref bool</param>
        /// <returns>IntPtr</returns>
        private static IntPtr HandleRawInputMessages(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var windowsMessage = (WindowsMessages)msg;

            switch (windowsMessage)
            {
                case WindowsMessages.WM_INPUT_DEVICE_CHANGE:
                    bool isNew = wParam.ToInt64() == 1;
                    IntPtr deviceHandle = lParam;
                    // Add to cache
                    if (isNew)
                    {
                        DeviceCache[deviceHandle] = RawInputApi.GetDeviceInformation(deviceHandle);
                    }
                    DeviceChangeSubject.OnNext(new RawInputDeviceChangeEventArgs
                    {
                        Added = isNew,
                        DeviceInformation = DeviceCache[deviceHandle]
                    });
                    // Remove from cache
                    if (!isNew)
                    {
                        DeviceCache.Remove(deviceHandle);
                    }
                    break;
                case WindowsMessages.WM_INPUT:
                    int outSize;
                    int size = Marshal.SizeOf(typeof(RawInput));

                    outSize = RawInputApi.GetRawInputData(lParam, RawInputDataCommands.Input, out var rawInput, ref size, Marshal.SizeOf(typeof(RawInputHeader)));
                    if (outSize != -1)
                    {
                        RawInputSubject.OnNext(new RawInputEventArgs
                        {
                            IsForeground = wParam.ToInt64() == 0,
                            RawInput = rawInput
                        });
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        ///     Create an observable to monitor raw-input device changes
        /// </summary>
        public static IObservable<RawInputEventArgs> MonitorRawInput(params RawInputDevices[] devices)
        {
            RawInputApi.RegisterRawInput(WinProcHandler.Instance.Handle, RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DeviceNotify, devices);
            return OnRawInput;
        }

        /// <summary>
        ///     Create an observable to monitor raw-input device changes
        /// </summary>
        public static IObservable<RawInputDeviceChangeEventArgs> MonitorRawInputDeviceChanges(params RawInputDevices[] devices)
        {
            RawInputApi.RegisterRawInput(WinProcHandler.Instance.Handle, RawInputDeviceFlags.DeviceNotify, devices);
            return OnDeviceChanges;
        }
    }
}
#endif