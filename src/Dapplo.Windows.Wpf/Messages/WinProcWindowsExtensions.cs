// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enums;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Wpf.Messages
{
    /// <summary>
    ///     A monitor for window messages
    /// </summary>
    /// <remarks>
    ///     Setting <see cref="WindowMessage.Handled"/> and <see cref="WindowMessage.Result"/> synchronously in OnNext (on the UI thread)
    ///     returns the result to Windows, and stops the following hooks and the default window procedure from processing it.
    ///     The HwndSource is never disposed by these extensions, the sequence completes when the HwndSource is disposed.
    /// </remarks>
    public static class WinProcWindowsExtensions
    {
        /// <summary>
        ///     Create an observable for the specified window, if the window has no handle yet the hook is added when the source is initialized.
        /// </summary>
        public static IObservable<WindowMessage> WinProcMessages(this Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            return CreateWinProcMessages(window, null);
        }

        /// <summary>
        ///     Create an observable for the specified HwndSource
        /// </summary>
        public static IObservable<WindowMessage> WinProcMessages(this HwndSource hWndSource)
        {
            if (hWndSource == null)
            {
                throw new ArgumentNullException(nameof(hWndSource));
            }
            return CreateWinProcMessages(null, hWndSource);
        }

        /// <summary>
        /// Create an observable for the specified window or HwndSource
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="suppliedHwndSource">HwndSource</param>
        /// <returns>IObservable</returns>
        private static IObservable<WindowMessage> CreateWinProcMessages(Window window, HwndSource suppliedHwndSource)
        {
            return Observable.Create<WindowMessage>(observer =>
            {
                HwndSource hWndSource = null;
                var isDisposed = false;

                // This handles the message, and generates the observable OnNext
                IntPtr WindowMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var message = new WindowMessage(hWnd, (WindowsMessages)msg, wParam, lParam);
                    observer.OnNext(message);
                    if (!message.Handled)
                    {
                        return IntPtr.Zero;
                    }
                    handled = true;
                    return message.Result;
                }

                void HwndSourceDisposedHandle(object sender, EventArgs e)
                {
                    observer.OnCompleted();
                }

                void RegisterHwndSource(HwndSource source)
                {
                    hWndSource = source;
                    hWndSource.Disposed += HwndSourceDisposedHandle;
                    hWndSource.AddHook(WindowMessageHandler);
                }

                void SourceInitialized(object sender, EventArgs args)
                {
                    window.SourceInitialized -= SourceInitialized;
                    var source = window.ToHwndSource();
                    if (isDisposed || source == null)
                    {
                        return;
                    }
                    RegisterHwndSource(source);
                    // Simulate the WM_NCCREATE
                    observer.OnNext(new WindowMessage(source.Handle, WindowsMessages.WM_NCCREATE, IntPtr.Zero, IntPtr.Zero));
                }

                var initialSource = suppliedHwndSource ?? window?.ToHwndSource();
                if (initialSource != null)
                {
                    if (initialSource.IsDisposed)
                    {
                        observer.OnCompleted();
                        return Disposable.Empty;
                    }
                    RegisterHwndSource(initialSource);
                }
                else if (window != null)
                {
                    // No handle yet, try to get it later
                    window.SourceInitialized += SourceInitialized;
                }

                return Disposable.Create(() =>
                {
                    isDisposed = true;
                    if (window != null)
                    {
                        window.SourceInitialized -= SourceInitialized;
                    }
                    if (hWndSource == null)
                    {
                        return;
                    }
                    hWndSource.Disposed -= HwndSourceDisposedHandle;
                    // Only remove our hook: the HwndSource belongs to the window (or to the caller), it's not ours to dispose
                    if (!hWndSource.IsDisposed)
                    {
                        hWndSource.RemoveHook(WindowMessageHandler);
                    }
                });
            });
        }

        /// <summary>
        /// Get the (existing) HwndSource of the specified Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>HwndSource or null when the window has no handle yet</returns>
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
