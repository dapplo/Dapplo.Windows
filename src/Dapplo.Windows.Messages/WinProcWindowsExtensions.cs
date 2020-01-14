// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System.Reactive;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interop;

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
            return WinProcMessages<Unit>(window, null);
        }

        /// <summary>
        ///     Create an observable for the specified HwndSource
        /// </summary>
        public static IObservable<WindowMessageInfo> WinProcMessages(this HwndSource hWndSource)
        {
            return WinProcMessages<Unit>(null, hWndSource);
        }

        /// <summary>
        /// Create an observable for the specified window or HwndSource
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="hWndSource">HwndSource</param>
        /// <param name="before">Func which does something as soon as the observable is created</param>
        /// <param name="disposeAction">Action which disposes something when the observable is disposed</param>
        /// <returns>IObservable</returns>
        internal static IObservable<WindowMessageInfo> WinProcMessages<TState>(Window window, HwndSource hWndSource, Func<IntPtr, TState> before = null, Action<TState> disposeAction = null)
        {
            if (window == null && hWndSource == null)
            {
                throw new NotSupportedException("One of Window or HwndSource must be supplied");
            }

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

                    TState state = default;

                    void HwndSourceDisposedHandle(object sender, EventArgs e)
                    {
                        observer.OnCompleted();
                    }
                    
                    void RegisterHwndSource()
                    {
                        if (before != null)
                        {
                            state = before(hWndSource.Handle);
                        }
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
                    else if (window != null)
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
                        disposeAction?.Invoke(state);
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