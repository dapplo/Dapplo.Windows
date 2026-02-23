// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Messages.Enumerations;

#if !NETSTANDARD2_0
using System.ComponentModel;
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
        private static readonly ClipboardSemaphore ClipboardLockProvider = new();

#if !NETSTANDARD2_0
        // for now this is not supported in netstandard 2.0

        // This maintains the sequence
        private static uint _previousSequence = uint.MinValue;

        private static IObservable<ClipboardUpdateInformation> _onUpdate;
        private static IObservable<ClipboardRenderFormatRequest> _onRenderFormat;

        /// <summary>
        ///     This observable publishes the current clipboard contents after every paste action.
        ///     Best to use SubscribeOn with the UI SynchronizationContext.
        /// </summary>
        public static IObservable<ClipboardUpdateInformation> OnUpdate {
            get
            {
                return _onUpdate ??= Observable.Create<ClipboardUpdateInformation>(observer =>
                    {
                        return SharedMessageWindow.Listen(
                            onSetup: hwnd =>
                            {
                                if (!NativeMethods.AddClipboardFormatListener((IntPtr)hwnd))
                                {
                                    observer.OnError(new Win32Exception());
                                }
                                else
                                {
                                    // Make sure the current contents are always published
                                    observer.OnNext(ClipboardUpdateInformation.Create((IntPtr)hwnd));
                                }
                            },
                            onTeardown: hwnd => NativeMethods.RemoveClipboardFormatListener((IntPtr)hwnd)
                        )
                        .Where(m => m.Msg == WindowsMessages.WM_CLIPBOARDUPDATE)
                        .Subscribe(m =>
                        {
                            var clipboardUpdateInformationInfo = ClipboardUpdateInformation.Create((IntPtr)m.Hwnd);

                            // Make sure we don't trigger multiple times, this happened while developing.
                            if (clipboardUpdateInformationInfo.Id > _previousSequence)
                            {
                                _previousSequence = clipboardUpdateInformationInfo.Id;
                                observer.OnNext(clipboardUpdateInformationInfo);
                            }
                        }, observer.OnError, observer.OnCompleted);
                    })
                    // Make sure there is always a value produced when connecting
                    .Publish()
                    .RefCount();
            }

        }

        /// <summary>
        ///     This observable can be subscribed to be informed if a certain clipboard format is requested
        ///     Best to use SubscribeOn with the UI SynchronizationContext.
        /// </summary>
        public static IObservable<ClipboardRenderFormatRequest> OnRenderFormat
        {
            get
            {
                return _onRenderFormat ??= Observable.Create<ClipboardRenderFormatRequest>(observer =>
                {
                    return SharedMessageWindow.Listen()
                        .Where(m => m.Msg == WindowsMessages.WM_RENDERALLFORMATS ||
                                    m.Msg == WindowsMessages.WM_RENDERFORMAT ||
                                    m.Msg == WindowsMessages.WM_DESTROYCLIPBOARD)
                        .Subscribe(m =>
                        {
                            switch (m.Msg)
                            {
                                case WindowsMessages.WM_RENDERALLFORMATS:
                                    var requestRenderFormat = new ClipboardRenderFormatRequest
                                    {
                                        AccessToken = Access()
                                    };
                                    observer.OnNext(requestRenderFormat);
                                    break;
                                case WindowsMessages.WM_RENDERFORMAT:
                                    var clipboardFormatId = (uint)m.WParam;
                                    var requestSingleFormat = new ClipboardRenderFormatRequest
                                    {
                                        RequestedFormatId = clipboardFormatId,
                                        // According to https://docs.microsoft.com/en-us/windows/win32/dataxchg/wm-renderformat the clipboard must not be open
                                        AccessToken = new ClipboardAccessToken {
                                            CanAccess = true,
                                            IsLockTimeout = true
                                        }
                                    };
                                    observer.OnNext(requestSingleFormat);
                                    break;
                                case WindowsMessages.WM_DESTROYCLIPBOARD:
                                    var requestDestroyClipboard = new ClipboardRenderFormatRequest
                                    {
                                        IsDestroyClipboard = true
                                    };
                                    observer.OnNext(requestDestroyClipboard);
                                    break;
                            }
                        }, observer.OnError, observer.OnCompleted);
                })
                // Make sure there is always a value produced when connecting
                .Publish()
                .RefCount();
            }

        }
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

        /// <summary>
        /// Test if the specified format is available on the clipboard
        /// </summary>
        /// <param name="formatId">uint</param>
        /// <returns>bool</returns>
        public static bool HasFormat(uint formatId) => NativeMethods.IsClipboardFormatAvailable(formatId);

        /// <summary>
        /// Test if the specified format is available on the clipboard
        /// </summary>
        /// <param name="format">string</param>
        /// <returns>bool</returns>
        public static bool HasFormat(string format) => NativeMethods.IsClipboardFormatAvailable(ClipboardFormatExtensions.MapFormatToId(format));
    }
}