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
        /// Create an observable for the specified window or HwndSource
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="hWndSource">HwndSource</param>
        /// <returns>IObservable</returns>
        private static IObservable<WindowMessageInfo> WinProcMessages(Window window, HwndSource hWndSource)
        {
            if (window == null && hWndSource == null)
            {
                throw new NotSupportedException("One of Window or HwndSource must be supplied");
            }
            if (window != null && hWndSource != null)
            {
                throw new NotSupportedException("Either Window or HwndSource must be supplied");
            }

            return Observable.Create<WindowMessageInfo>(observer =>
                {
                    // This handles the message, and generates the observable OnNext
                    IntPtr WindowMessageHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        observer.OnNext(WindowMessageInfo.Create(hwnd, msg, wParam, lParam));
                        // ReSharper disable once AccessToDisposedClosure
                        if (hWndSource.IsDisposed)
                        {
                            observer.OnCompleted();
                        }
                        return IntPtr.Zero;
                    }

                    void HwndSourceDisposedHandle(object sender, EventArgs e)
                    {
                        observer.OnCompleted();
                    }
                    
                    void RegisterHwndSource()
                    {
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