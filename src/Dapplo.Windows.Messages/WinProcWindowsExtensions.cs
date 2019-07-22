//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019 Dapplo
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


using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;
#if !NETSTANDARD2_0
#region using

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interop;

#endregion

namespace Dapplo.Windows.Messages
{
    /// <summary>
    ///     A monitor for window messages
    /// </summary>
    public static class WinProcWindowsExtensions
    {
        /// <summary>
        /// Registers the device or type of device for which a window will receive notifications.
        /// </summary>
        /// <param name="hRecipient">IntPtr</param>
        /// <param name="notificationFilter">DevBroadcastDeviceInterface</param>
        /// <param name="flags">DeviceNotifyFlags</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, in DevBroadcastDeviceInterface notificationFilter, DeviceNotifyFlags flags);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        internal static extern bool UnregisterDeviceNotification(IntPtr handle);

        /// <summary>
        ///     Create an observable for the specified window
        /// </summary>
        public static IObservable<WindowMessageInfo> WinProcMessages(this Window window)
        {
            return WinProcMessages(window, null);
        }

        /// <summary>
        ///     Create an observable for the specified HwndSource
        /// </summary>
        public static IObservable<WindowMessageInfo> WinProcMessages(this HwndSource hwndSource)
        {
            return WinProcMessages(null, hwndSource);
        }

        /// <summary>
        ///     Create an observable for devices for the specified window
        /// </summary>
        public static IObservable<WindowMessageInfo> DeviceNotifications(this Window window)
        {
            return WinProcMessages(window, null, hWnd =>
            {
                var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Create();
                var deviceNotifyFlags = DeviceNotifyFlags.WindowHandle | DeviceNotifyFlags.AllInterfaceClasses;

                var deviceNotificationHandle = RegisterDeviceNotification(hWnd, devBroadcastDeviceInterface, deviceNotifyFlags);
                return deviceNotificationHandle;
            }, o => UnregisterDeviceNotification((IntPtr) o)).Where(windowMessageInfo => windowMessageInfo.Message == WindowsMessages.WM_DEVICECHANGE);
        }

        /// <summary>
        /// Create an observable for the specified window or HwndSource
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="hWndSource">HwndSource</param>
        /// <returns>IObservable</returns>
        private static IObservable<WindowMessageInfo> WinProcMessages(Window window, HwndSource hWndSource, Func<IntPtr, object> before = null, Action<object> dispose = null)
        {
            if (window == null && hWndSource == null)
            {
                throw new NotSupportedException("One of Window or HwndSource must be supplied");
            }

            // TODO: Use a cache for the observable?

            return Observable.Create<WindowMessageInfo>(observer =>
                {
                    // This handles the message, and generates the observable OnNext
                    IntPtr WindowMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        observer.OnNext(WindowMessageInfo.Create(hWnd, msg, wParam, lParam));
                        // ReSharper disable once AccessToDisposedClosure
                        if (hWndSource.IsDisposed)
                        {
                            observer.OnCompleted();
                        }
                        return IntPtr.Zero;
                    }

                    object state = null;

                    void HwndSourceDisposedHandle(object sender, EventArgs e)
                    {
                        observer.OnCompleted();
                    }
                    
                    void RegisterHwndSource()
                    {
                        state = before?.Invoke(hWndSource.Handle);
                        hWndSource.Disposed += HwndSourceDisposedHandle;
                        hWndSource.AddHook(WindowMessageHandler);
                    }

                    if (window != null)
                    {
                        hWndSource = window.ToHwndSource();
                    }
                    if (hWndSource != null) { 
                        RegisterHwndSource();
                    }
                    else
                    {
                        // No, try to get it later
                        window.SourceInitialized += (sender, args) =>
                        {
                            hWndSource = window.ToHwndSource();
                            RegisterHwndSource();
                            // Simulate the WM_NCCREATE
                            observer.OnNext(WindowMessageInfo.Create(hWndSource.Handle, (int)WindowsMessages.WM_NCCREATE, IntPtr.Zero, IntPtr.Zero));
                        };
                    }

                    return Disposable.Create(() =>
                    {
                        dispose?.Invoke(state);
                        hWndSource.Disposed -= HwndSourceDisposedHandle;
                        hWndSource.RemoveHook(WindowMessageHandler);
                        hWndSource.Dispose();
                    });
                })
                // Make sure there is always a value produced when connecting
                .Publish()
                .RefCount();
        }

        /// <summary>
        /// Create a HwndSource for the specified Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>HwndSource</returns>
        private static HwndSource ToHwndSource(this Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }
            return HwndSource.FromHwnd(windowHandle);
        }
    }
}
#endif