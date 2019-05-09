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
            WinProcHandler.Instance.Subscribe(HandleRawInputMessages);
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
        /// <param name="hwnd">IntPtr</param>
        /// <param name="msg">int</param>
        /// <param name="wParam">IntPtr</param>
        /// <param name="lParam">IntPtr</param>
        /// <param name="handled">ref bool</param>
        /// <returns>IntPtr</returns>
        private static IntPtr HandleRawInputMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
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
        ///     Create an observable to monitor rawinput device changes
        /// </summary>
        public static IObservable<RawInputEventArgs> MonitorRawInput(params RawInputDevices[] devices)
        {
            RawInputApi.RegisterRawInput(WinProcHandler.Instance.Handle, RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DeviceNotify, devices);
            return OnRawInput;
        }

        /// <summary>
        ///     Create an observable to monitor rawinput device changes
        /// </summary>
        public static IObservable<RawInputDeviceChangeEventArgs> MonitorRawInputDeviceChanges(params RawInputDevices[] devices)
        {
            RawInputApi.RegisterRawInput(WinProcHandler.Instance.Handle, RawInputDeviceFlags.DeviceNotify, devices);
            return OnDeviceChanges;
        }
    }
}
#endif