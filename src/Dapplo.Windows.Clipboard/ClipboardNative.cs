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

using System;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard.Internals;
#if !NETSTANDARD2_0
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Dapplo.Windows.Messages;
#endif

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Provides low level access to the Windows clipboard
    /// </summary>
    public static class ClipboardNative
    {
        // "Global" clipboard lock
        private static readonly ClipboardSemaphore ClipboardLockProvider = new ClipboardSemaphore();

#if !NETSTANDARD2_0
        // for now this is not supported in netstandard 2.0

        // This maintains the sequence
        private static uint _previousSequence = uint.MinValue;

        /// <summary>
        ///     Private constructor to create the observable
        /// </summary>
        static ClipboardNative()
        {
            OnUpdate = Observable.Create<ClipboardUpdateInformation>(observer =>
            {
                // This handles the message
                IntPtr WinProcClipboardMessageHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var windowsMessage = (WindowsMessages)msg;
                    if (windowsMessage != WindowsMessages.WM_CLIPBOARDUPDATE)
                    {
                        return IntPtr.Zero;
                    }

                    var clipboardUpdateInformationInfo = ClipboardUpdateInformation.Create(hwnd);

                    // Make sure we don't trigger multiple times, this happend while developing.
                    if (clipboardUpdateInformationInfo.Id > _previousSequence)
                    {
                        _previousSequence = clipboardUpdateInformationInfo.Id;
                        observer.OnNext(clipboardUpdateInformationInfo);
                    }

                    return IntPtr.Zero;
                }

                var hookSubscription = WinProcHandler.Instance.Subscribe(WinProcClipboardMessageHandler);
                if (!NativeMethods.AddClipboardFormatListener(WinProcHandler.Instance.Handle))
                {
                    observer.OnError(new Win32Exception());
                }
                else
                {
                    // Make sure the current contents are always published
                    observer.OnNext(ClipboardUpdateInformation.Create());
                }

                return Disposable.Create(() =>
                {
                    NativeMethods.RemoveClipboardFormatListener(WinProcHandler.Instance.Handle);
                    hookSubscription.Dispose();
                });
            })
            // Make sure there is always a value produced when connecting
            .Publish()
            .RefCount();
        }

        /// <summary>
        ///     This observable publishes the current clipboard contents after every paste action.
        ///     Best to use SubscribeOn with the UI SynchronizationContext.
        /// </summary>
        public static IObservable<ClipboardUpdateInformation> OnUpdate { get; }
#endif

        /// <summary>
        /// Get access, a global lock, to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="timeout">Timeout for getting the lock</param>
        /// <returns>IClipboard, which will unlock when Dispose is called</returns>
        public static IClipboardAccessToken Access(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            return ClipboardLockProvider.Lock(hWnd, retries, retryInterval, timeout);
        }

        /// <summary>
        /// Get access, a global lock, to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle</param>
        /// <param name="retries">int with the amount of lock attempts are made</param>
        /// <param name="retryInterval">Timespan between retries, default 200ms</param>
        /// <param name="timeout">Timespan to wait for a lock, default 1000ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IClipboard in a Task, which will unlock when Dispose is called</returns>
        public static ValueTask<IClipboardAccessToken> AccessAsync(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ClipboardLockProvider.LockAsync(hWnd, retries, retryInterval, timeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the current owner
        /// </summary>
        public static IntPtr CurrentOwner => NativeMethods.GetClipboardOwner();

        /// <summary>
        /// Retrieves the current clipboard sequence number via GetClipboardSequenceNumber
        /// This returns 0 if there is no WINSTA_ACCESSCLIPBOARD
        /// </summary>
        public static uint SequenceNumber => NativeMethods.GetClipboardSequenceNumber();
    }
}