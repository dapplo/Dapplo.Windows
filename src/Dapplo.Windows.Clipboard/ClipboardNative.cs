// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;

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
        ///     or when all formats need to be rendered (WM_RENDERALLFORMATS, when the SharedMessageWindow is destroyed: at process exit or with SharedMessageWindow.Shutdown),
        ///     and needs to place the data via <see cref="ClipboardRenderFormatRequest.AccessToken"/>.
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
        /// Use the clipboard: wait asynchronously until it can be opened, then open it, run <paramref name="work"/> and close it again,
        /// synchronously on one thread. This is the recommended way to access the clipboard from async code.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Works on any thread, no STA thread is needed.</item>
        /// <item>Only the waiting is asynchronous. The awaits run on the context of the caller, so the work runs there too (e.g. the UI thread).</item>
        /// <item><paramref name="work"/> must not be async: never await while the clipboard is open, the token is only valid until the work returns.
        /// Read the data into memory and process it after UseAsync returns; prepare the data to write before calling UseAsync.</item>
        /// <item>Don't call <see cref="Access"/>, <see cref="AccessAsync"/> or UseAsync inside the work, the in-process lock isn't reentrant.</item>
        /// </list>
        /// </remarks>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="work">Func which reads or writes the clipboard via the token and returns a result</param>
        /// <param name="options">optional ClipboardAccessOptions (owner, retries, retry interval, lock timeout)</param>
        /// <param name="cancellationToken">CancellationToken, cancels the waiting</param>
        /// <returns>Task with the result of the work</returns>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened, or the in-process lock timed out</exception>
        public static Task<T> UseAsync<T>(Func<IClipboardAccessToken, T> work, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            options ??= new ClipboardAccessOptions();
            options.Validate();
            return ClipboardLockProvider.UseAsync(work, options, cancellationToken);
        }

        /// <summary>
        /// Use the clipboard, see <see cref="UseAsync{T}(Func{IClipboardAccessToken, T}, ClipboardAccessOptions, CancellationToken)"/>.
        /// </summary>
        /// <param name="work">Action which reads or writes the clipboard via the token, it must not be async</param>
        /// <param name="options">optional ClipboardAccessOptions (owner, retries, retry interval, lock timeout)</param>
        /// <param name="cancellationToken">CancellationToken, cancels the waiting</param>
        /// <returns>Task</returns>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened, or the in-process lock timed out</exception>
        public static Task UseAsync(Action<IClipboardAccessToken> work, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            return UseAsync<bool>(token =>
            {
                work(token);
                return true;
            }, options, cancellationToken);
        }

        /// <summary>
        /// Not supported: the work must not be async, the clipboard is closed when it returns. This overload only exists to turn an async lambda into a compile error.
        /// </summary>
        [Obsolete("The work must not be async: never await while the clipboard is open. Read or write synchronously and do the asynchronous work before or after UseAsync.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task UseAsync(Func<IClipboardAccessToken, Task> work, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The work must not be async.");
        }

        /// <summary>
        /// Not supported: the work must not be async, the clipboard is closed when it returns. This overload only exists to turn an async lambda into a compile error.
        /// </summary>
        [Obsolete("The work must not be async: never await while the clipboard is open. Read or write synchronously and do the asynchronous work before or after UseAsync.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<T> UseAsync<T>(Func<IClipboardAccessToken, Task<T>> work, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The work must not be async.");
        }

        /// <summary>
        /// Replace the content of the clipboard with the contents: opens the clipboard, clears it, places all formats and closes it again.
        /// This is the recommended way to write to the clipboard, prepare the contents before calling this so the clipboard is only open briefly.
        /// </summary>
        /// <param name="contents">ClipboardContents with all formats</param>
        /// <param name="hWnd">IntPtr with the windows handle which becomes the owner, default is the SharedMessageWindow (needed for delayed rendering)</param>
        /// <param name="retries">int with the amount of open attempts which are made, default 5</param>
        /// <param name="retryInterval">Timespan between retries, default 100ms</param>
        /// <param name="timeout">Timeout for getting the in-process lock, default 200ms</param>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened</exception>
        public static void ReplaceContents(ClipboardContents contents, IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }
            using var clipboardAccessToken = Access(hWnd, retries, retryInterval, timeout);
            clipboardAccessToken.ReplaceContents(contents);
        }

        /// <summary>
        /// Replace the content of the clipboard with the contents, see <see cref="ReplaceContents(ClipboardContents, IntPtr, int, TimeSpan?, TimeSpan?)"/>.
        /// Only the waiting for the clipboard is asynchronous, the clipboard is opened, written and closed on the thread which continues after the await.
        /// </summary>
        /// <param name="contents">ClipboardContents with all formats</param>
        /// <param name="hWnd">IntPtr with the windows handle which becomes the owner, default is the SharedMessageWindow (needed for delayed rendering)</param>
        /// <param name="retries">int with the amount of open attempts which are made, default 5</param>
        /// <param name="retryInterval">Timespan between retries, default 100ms</param>
        /// <param name="timeout">Timespan to wait for the in-process lock, default 200ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened</exception>
        public static async Task ReplaceContentsAsync(ClipboardContents contents, IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }
            // No ConfigureAwait(false): the clipboard is opened on the context of the caller, and must be used and closed there
            using var clipboardAccessToken = await AccessAsync(hWnd, retries, retryInterval, timeout, cancellationToken);
            clipboardAccessToken.ReplaceContents(contents);
        }

        /// <summary>
        /// The window which has the clipboard open right now (GetOpenClipboardWindow), IntPtr.Zero when the clipboard isn't open
        /// or was opened without a window. Use it to tell the user which application blocks the clipboard,
        /// see also <see cref="IClipboardAccessToken.BlockingWindow"/> and <see cref="ClipboardAccessDeniedException.BlockingWindow"/>.
        /// </summary>
        public static IntPtr OpenClipboardWindow => NativeMethods.GetOpenClipboardWindow();

        /// <summary>
        /// Read the formats into memory in one short clipboard session, see <see cref="ClipboardSnapshotExtensions.ReadSnapshot"/>.
        /// Decode or send the data afterwards, while the clipboard is available for other applications again.
        /// </summary>
        /// <param name="formats">The formats to read, null reads every format which is stored in memory (handle formats like CF_BITMAP, CF_ENHMETAFILE and CF_PALETTE are skipped).
        /// Pass the formats you need: reading all formats makes the copying application render every delayed rendered format.
        /// <see cref="AvailableFormats(IEnumerable{string}, int)"/> selects the available ones without opening the clipboard.</param>
        /// <param name="options">optional ClipboardAccessOptions</param>
        /// <param name="cancellationToken">CancellationToken, cancels the waiting for the clipboard</param>
        /// <returns>Task with the ClipboardSnapshot</returns>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened</exception>
        public static Task<ClipboardSnapshot> ReadSnapshotAsync(IEnumerable<string> formats = null, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            return ReadSnapshotAsync(formats, long.MaxValue, options, cancellationToken);
        }

        /// <summary>
        /// Read the formats into memory in one short clipboard session, formats larger than <paramref name="maxBytesPerFormat"/> are skipped
        /// (see <see cref="ClipboardSnapshot.SkippedFormats"/>).
        /// </summary>
        /// <param name="formats">The formats to read, null reads every format which is stored in memory</param>
        /// <param name="maxBytesPerFormat">long with the maximum size of one format in bytes</param>
        /// <param name="options">optional ClipboardAccessOptions</param>
        /// <param name="cancellationToken">CancellationToken, cancels the waiting for the clipboard</param>
        /// <returns>Task with the ClipboardSnapshot</returns>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard couldn't be opened</exception>
        public static Task<ClipboardSnapshot> ReadSnapshotAsync(IEnumerable<string> formats, long maxBytesPerFormat, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            // Resolve (and register) the format names before the clipboard is opened
            var formatList = formats?.ToList();
            return UseAsync(clipboard => clipboard.ReadSnapshot(formatList, maxBytesPerFormat), options, cancellationToken);
        }

        /// <summary>
        /// Get the OLE data object of the clipboard (OleGetClipboard), for what the Win32 clipboard API can't read: formats with an index,
        /// IStream data and virtual files (FileGroupDescriptorW + FileContents, e.g. Outlook attachments).
        /// </summary>
        /// <remarks>
        /// OLE requires an STA thread on which OLE is initialized (every WinForms / WPF UI thread is one), unlike the rest of this library.
        /// Use the reader on that thread, keep the usage short and dispose it: the data object is a snapshot of the clipboard at this moment.
        /// </remarks>
        /// <param name="retries">int with the number of retries when another application has the clipboard open (CLIPBRD_E_CANT_OPEN), default 5</param>
        /// <param name="retryInterval">TimeSpan between the retries, default 100ms. The retries block the calling thread, like <see cref="Access"/>.</param>
        /// <returns>DataObjectReader, dispose it to release the data object</returns>
        /// <exception cref="InvalidOperationException">When called on a thread which isn't STA, or on which OLE isn't initialized</exception>
        /// <exception cref="ClipboardAccessDeniedException">When the clipboard stays open by another application, with the blocking window</exception>
        /// <exception cref="COMException">When OleGetClipboard fails otherwise</exception>
        public static DataObjectReader GetOleDataObject(int retries = 5, TimeSpan? retryInterval = null)
        {
            if (retries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retries), retries, "Retries must not be negative.");
            }
            retryInterval ??= TimeSpan.FromMilliseconds(100);
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("OleGetClipboard needs an STA thread with OLE initialized, e.g. the UI thread. The Win32 clipboard API of ClipboardNative works on any thread.");
            }
            const int clipboardCantOpen = unchecked((int)0x800401D0);
            int hResult;
            System.Runtime.InteropServices.ComTypes.IDataObject dataObject;
            while (true)
            {
                hResult = OleGetClipboard(out dataObject);
                if (hResult != clipboardCantOpen)
                {
                    break;
                }
                if (retries-- <= 0)
                {
                    var blocker = ClipboardBlocker.Detect();
                    throw ClipboardAccessToken.CreateOpenTimeoutException(blocker);
                }
                Thread.Sleep(retryInterval.Value);
            }
            // CO_E_NOTINITIALIZED
            if (hResult == unchecked((int)0x800401F0))
            {
                throw new InvalidOperationException("OLE is not initialized on this thread: call OleInitialize first (a WinForms [STAThread] UI thread and WPF do this).");
            }
            if (hResult != 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }
            return new DataObjectReader(dataObject, true) { IsFromClipboard = true };
        }

        [DllImport("ole32")]
        private static extern int OleGetClipboard(out System.Runtime.InteropServices.ComTypes.IDataObject dataObject);

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

        /// <summary>
        /// Test if the clipboard has virtual files (FileGroupDescriptorW or the ANSI FileGroupDescriptor), e.g. Outlook attachments,
        /// without opening the clipboard. Reading them needs OLE, see <see cref="ClipboardSnapshot.TryUseVirtualFiles{T}"/>.
        /// </summary>
        /// <returns>bool</returns>
        public static bool HasVirtualFiles() => HasFormat(DataObjectReader.FileGroupDescriptorWFormat) || HasFormat(DataObjectReader.FileGroupDescriptorFormat);

        /// <summary>
        /// The formats of <paramref name="preferred"/> which are available on the clipboard right now, in the order of <paramref name="preferred"/>,
        /// at most <paramref name="max"/>. The clipboard isn't opened (IsClipboardFormatAvailable), so this never blocks or fails because another
        /// application has it open. Formats which Windows synthesizes (e.g. CF_DIB from CF_BITMAP, CF_UNICODETEXT from CF_TEXT) count as available.
        /// </summary>
        /// <remarks>
        /// Reading a format makes the application which copied render it, which can be slow (e.g. large images in several formats). Use this to
        /// pass only the formats you need to <see cref="ReadSnapshotAsync(IEnumerable{string}, ClipboardAccessOptions, CancellationToken)"/>, e.g.
        /// the best two image formats: the second one is a fallback when the first can't be decoded.
        /// The clipboard can change between this call and the read: the snapshot then simply has fewer formats.
        /// </remarks>
        /// <param name="preferred">the format names in the order of preference, null or empty names are ignored, duplicates are returned once</param>
        /// <param name="max">int with the maximum number of formats to return, default all</param>
        /// <returns>list with the available format names, as passed in <paramref name="preferred"/></returns>
        /// <exception cref="ArgumentNullException">When <paramref name="preferred"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="max"/> is negative</exception>
        public static IReadOnlyList<string> AvailableFormats(IEnumerable<string> preferred, int max = int.MaxValue)
        {
            if (preferred == null)
            {
                throw new ArgumentNullException(nameof(preferred));
            }
            if (max < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "The maximum must not be negative.");
            }
            var result = new List<string>();
            var seen = new HashSet<uint>();
            foreach (var format in preferred)
            {
                if (result.Count >= max)
                {
                    break;
                }
                if (string.IsNullOrEmpty(format))
                {
                    continue;
                }
                var formatId = ClipboardFormatExtensions.MapFormatToId(format);
                if (formatId != 0 && seen.Add(formatId) && NativeMethods.IsClipboardFormatAvailable(formatId))
                {
                    result.Add(format);
                }
            }
            return result;
        }
    }
}
