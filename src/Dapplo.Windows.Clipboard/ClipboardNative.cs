// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Provides low level access to the Windows clipboard
    /// </summary>
    public static class ClipboardNative
    {
        // "Global" clipboard lock
        private static readonly ClipboardSemaphore ClipboardLockProvider = new();

        // One shared clipboard format listener for all subscribers, created thread-safe
        private static readonly Lazy<IObservable<ClipboardUpdateInformation>> ClipboardUpdates = new(CreateClipboardUpdates, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        ///     This observable publishes information on the clipboard contents after every clipboard change.
        ///     Every subscriber first receives the current state, and then an update for every change.
        /// </summary>
        /// <remarks>
        ///     The information is collected without opening the clipboard, see <see cref="ClipboardUpdateInformation"/>.
        ///     Updates are published on the SharedMessageWindow thread, don't block it: use ObserveOn to process them elsewhere,
        ///     especially if you want to read the clipboard content with <see cref="Access"/> or <see cref="AccessAsync"/>.
        /// </remarks>
        public static IObservable<ClipboardUpdateInformation> OnUpdate =>
            Observable.Defer(() => ClipboardUpdates.Value.StartWith(ClipboardUpdateInformation.Create()));

        /// <summary>
        /// Create the shared observable for the clipboard updates
        /// </summary>
        private static IObservable<ClipboardUpdateInformation> CreateClipboardUpdates()
        {
            return Observable.Create<ClipboardUpdateInformation>(observer =>
                {
                    // Only used on the SharedMessageWindow thread
                    uint previousSequence = 0;
                    return SharedMessageWindow.Listen(
                            onSetup: hwnd =>
                            {
                                if (!NativeMethods.AddClipboardFormatListener(hwnd))
                                {
                                    throw new Win32Exception();
                                }
                            },
                            onTeardown: hwnd => NativeMethods.RemoveClipboardFormatListener(hwnd)
                        )
                        .Where(m => m.Msg == WindowsMessages.WM_CLIPBOARDUPDATE)
                        .Subscribe(m =>
                        {
                            // This runs inside the window procedure: never throw, never open the clipboard or sleep here
                            ClipboardUpdateInformation clipboardUpdateInformation;
                            try
                            {
                                clipboardUpdateInformation = ClipboardUpdateInformation.Create();
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Dapplo.Windows.Clipboard: retrieving the clipboard update information failed: {0}", ex);
                                return;
                            }

                            // Make sure we don't trigger multiple times for the same change, a sequence of 0 means it's not available
                            if (clipboardUpdateInformation.Id != 0 && clipboardUpdateInformation.Id == previousSequence)
                            {
                                return;
                            }
                            previousSequence = clipboardUpdateInformation.Id;
                            try
                            {
                                observer.OnNext(clipboardUpdateInformation);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Dapplo.Windows.Clipboard: an OnUpdate subscriber failed: {0}", ex);
                            }
                        }, observer.OnError, observer.OnCompleted);
                })
                .Publish()
                .RefCount();
        }

        /// <summary>
        ///     Register a renderer for a clipboard format which is placed on the clipboard with delayed rendering (SetDelayedRenderedContent).
        ///     The renderer is called synchronously on the SharedMessageWindow thread when the format is requested (WM_RENDERFORMAT),
        ///     or when all formats need to be rendered (WM_RENDERALLFORMATS), and needs to place the data via <see cref="ClipboardRenderFormatRequest.AccessToken"/>.
        /// </summary>
        /// <remarks>
        ///     The renderer must render directly: don't switch threads, await or open the clipboard yourself, the token is only valid while the renderer runs.
        ///     Only one renderer per format can be registered at a time.
        /// </remarks>
        /// <param name="formatId">uint with the clipboard format ID</param>
        /// <param name="renderer">Action which places the data for the requested format on the clipboard</param>
        /// <returns>IDisposable, dispose to unregister the renderer</returns>
        public static IDisposable RegisterDelayedRenderer(uint formatId, Action<ClipboardRenderFormatRequest> renderer)
        {
            return DelayedRenderers.Register(formatId, renderer);
        }

        /// <summary>
        ///     Register a renderer for a clipboard format which is placed on the clipboard with delayed rendering, see <see cref="RegisterDelayedRenderer(uint, Action{ClipboardRenderFormatRequest})"/>.
        /// </summary>
        /// <param name="format">string with the clipboard format</param>
        /// <param name="renderer">Action which places the data for the requested format on the clipboard</param>
        /// <returns>IDisposable, dispose to unregister the renderer</returns>
        public static IDisposable RegisterDelayedRenderer(string format, Action<ClipboardRenderFormatRequest> renderer)
        {
            return DelayedRenderers.Register(ClipboardFormatExtensions.MapFormatToId(format), renderer);
        }

        /// <summary>
        ///     Register a renderer for a clipboard format which is placed on the clipboard with delayed rendering, see <see cref="RegisterDelayedRenderer(uint, Action{ClipboardRenderFormatRequest})"/>.
        /// </summary>
        /// <param name="format">StandardClipboardFormats with the clipboard format</param>
        /// <param name="renderer">Action which places the data for the requested format on the clipboard</param>
        /// <returns>IDisposable, dispose to unregister the renderer</returns>
        public static IDisposable RegisterDelayedRenderer(StandardClipboardFormats format, Action<ClipboardRenderFormatRequest> renderer)
        {
            return DelayedRenderers.Register((uint)format, renderer);
        }

        /// <summary>
        /// Get access, a global lock, to the clipboard.
        /// The clipboard is opened on the calling thread, use the token only on this thread and dispose it there.
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle which becomes the owner when the content is cleared, default is the SharedMessageWindow</param>
        /// <param name="retries">int with the amount of open attempts which are made, default 5</param>
        /// <param name="retryInterval">Timespan between retries, default 100ms</param>
        /// <param name="timeout">Timeout for getting the in-process lock, default 200ms</param>
        /// <returns>IClipboardAccessToken, which will unlock when Dispose is called</returns>
        public static IClipboardAccessToken Access(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            return ClipboardLockProvider.Lock(hWnd, retries, retryInterval, timeout);
        }

        /// <summary>
        /// Get access, a global lock, to the clipboard.
        /// Only the waiting is asynchronous, the clipboard is opened on the thread which continues after the await (the SynchronizationContext of the caller).
        /// Use the token only on that thread and dispose it there: don't await while holding the token.
        /// </summary>
        /// <param name="hWnd">IntPtr with the windows handle which becomes the owner when the content is cleared, default is the SharedMessageWindow</param>
        /// <param name="retries">int with the amount of open attempts which are made, default 5</param>
        /// <param name="retryInterval">Timespan between retries, default 100ms</param>
        /// <param name="timeout">Timespan to wait for the in-process lock, default 200ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IClipboardAccessToken in a ValueTask, which will unlock when Dispose is called</returns>
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
        /// <param name="format">StandardClipboardFormats</param>
        /// <returns>bool</returns>
        public static bool HasFormat(StandardClipboardFormats format) => NativeMethods.IsClipboardFormatAvailable((uint)format);

        /// <summary>
        /// Test if the specified format is available on the clipboard
        /// </summary>
        /// <param name="format">string</param>
        /// <returns>bool</returns>
        public static bool HasFormat(string format) => NativeMethods.IsClipboardFormatAvailable(ClipboardFormatExtensions.MapFormatToId(format));
    }
}
