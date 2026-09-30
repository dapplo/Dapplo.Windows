// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// This can be used to get a lock to the clipboard, and free it again.
/// </summary>
internal sealed class ClipboardSemaphore : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMilliseconds(100);
    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
    // To detect redundant calls
    private bool _disposedValue;

    /// <summary>
    /// Get a lock to the clipboard, the clipboard is opened on the calling thread and can only be used (and must be disposed) on this thread.
    /// </summary>
    /// <param name="hWnd">IntPtr with a hWnd for the potential new owner, default is the SharedMessageWindow</param>
    /// <param name="retries">int with number of retries, default is 5</param>
    /// <param name="retryInterval">TimeSpan for the time between retries, default is 100ms</param>
    /// <param name="timeout">optional TimeSpan for the timeout to get the in-process lock, default is 200ms</param>
    /// <returns>IClipboardAccessToken</returns>
    public IClipboardAccessToken Lock(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
    {
        // Set default retry interval
        retryInterval ??= DefaultRetryInterval;
        // Set default timeout interval
        timeout ??= DefaultTimeout;

        if (hWnd == IntPtr.Zero)
        {
            // The shared window is the owner, it's always available and it receives the delayed rendering messages
            hWnd = SharedMessageWindow.Handle;
        }

        // If a timeout is passed, use this in the wait
        if (!_semaphoreSlim.Wait(timeout.Value))
        {
            // Timeout
            return new ClipboardAccessToken
            {
                CanAccess = false,
                IsLockTimeout = true
            };
        }

        // From here on the semaphore is held: every path must either hand it over to a token, or release it.
        try
        {
            // Create the clipboard lock itself
            bool isOpened = false;
            do
            {
                if (OpenClipboard(hWnd))
                {
                    isOpened = true;
                    break;
                }
                retries--;
                // No reason to sleep, if there are no more retries
                if (retries >= 0)
                {
                    Thread.Sleep(retryInterval.Value);
                }

            } while (retries >= 0);

            if (!isOpened)
            {
                var blocker = ClipboardBlocker.Detect();
                _semaphoreSlim.Release();
                return ClipboardAccessToken.OpenTimeout(blocker);
            }
        }
        catch
        {
            _semaphoreSlim.Release();
            throw;
        }
        // Return a disposable which cleans up the current state.
        return CreateOpenToken(hWnd);
    }

    /// <summary>
    /// Lock the clipboard, return a disposable which can free this again.
    /// Only the waiting is asynchronous: every OpenClipboard attempt runs on the context of the caller (no ConfigureAwait(false)),
    /// so the clipboard is opened on the thread which continues after the await. Use and dispose the token on that thread.
    /// </summary>
    /// <param name="hWnd">IntPtr with the hWnd of the potential new owner, default is the SharedMessageWindow</param>
    /// <param name="retries">int with the number of retries, default is 5</param>
    /// <param name="retryInterval">optional TimeSpan between retries, default is 100ms</param>
    /// <param name="timeout">optional TimeSpan for the timeout to get the in-process lock, default is 200ms</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>ValueTask with IClipboardAccessToken</returns>
    public async ValueTask<IClipboardAccessToken> LockAsync(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        // Set default retry interval
        retryInterval ??= DefaultRetryInterval;
        // Set default timeout interval
        timeout ??= DefaultTimeout;

        if (hWnd == IntPtr.Zero)
        {
            // The shared window is the owner, it's always available and it receives the delayed rendering messages
            hWnd = SharedMessageWindow.Handle;
        }

        // Await the semaphore, until the timeout is triggered.
        // Don't use ConfigureAwait(false): OpenClipboard must run on the thread which will use the token.
        if (!await _semaphoreSlim.WaitAsync(timeout.Value, cancellationToken))
        {
            // Timeout
            return new ClipboardAccessToken
            {
                CanAccess = false,
                IsLockTimeout = true
            };
        }

        // From here on the semaphore is held: every path must either hand it over to a token, or release it.
        try
        {
            bool isOpened = false;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (OpenClipboard(hWnd))
                {
                    isOpened = true;
                    break;
                }
                retries--;
                // Break if there are no more retries
                if (retries < 0)
                {
                    break;
                }
                // Don't use ConfigureAwait(false), the next OpenClipboard attempt must run on the caller's context
                await Task.Delay(retryInterval.Value, cancellationToken);
            } while (true);

            if (!isOpened)
            {
                var blocker = ClipboardBlocker.Detect();
                _semaphoreSlim.Release();
                // Timeout
                return ClipboardAccessToken.OpenTimeout(blocker);
            }
        }
        catch
        {
            // e.g. OperationCanceledException
            _semaphoreSlim.Release();
            throw;
        }

        return CreateOpenToken(hWnd);
    }

    /// <summary>
    /// Wait for the clipboard asynchronously, then open it, run the work and close it again synchronously on one thread.
    /// Every await runs on the context of the caller, so <paramref name="work"/> runs there too (e.g. the UI thread).
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="work">Func which uses the clipboard, it must not await or switch threads</param>
    /// <param name="options">ClipboardAccessOptions</param>
    /// <param name="cancellationToken">CancellationToken, only used while waiting</param>
    /// <returns>Task with the result of the work</returns>
    public async Task<T> UseAsync<T>(Func<IClipboardAccessToken, T> work, ClipboardAccessOptions options, CancellationToken cancellationToken)
    {
        var hWnd = options.Owner;
        if (hWnd == IntPtr.Zero)
        {
            // The shared window is the owner, it's always available and it receives the delayed rendering messages
            hWnd = SharedMessageWindow.Handle;
        }

        // Don't use ConfigureAwait(false): the work runs on the context of the caller
        if (!await _semaphoreSlim.WaitAsync(options.LockTimeout, cancellationToken))
        {
            throw ClipboardAccessToken.CreateLockTimeoutException();
        }

        try
        {
            var retries = options.Retries;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Open, work and close without any await in between: all on this thread
                if (OpenClipboard(hWnd))
                {
                    var ownerThreadId = Environment.CurrentManagedThreadId;
                    var token = new ClipboardAccessToken(() =>
                    {
                        if (!CloseClipboard())
                        {
                            Trace.TraceWarning("Dapplo.Windows.Clipboard: CloseClipboard failed with error {0} on thread {1}.", Marshal.GetLastWin32Error(), ownerThreadId);
                        }
                    })
                    {
                        OwnerHandle = hWnd
                    };
                    try
                    {
                        var result = work(token);
                        if (result is Task)
                        {
                            throw new InvalidOperationException("The work passed to ClipboardNative.UseAsync returned a Task: the clipboard is closed when the work returns, never await while the clipboard is open. Read or write the clipboard synchronously, and do the asynchronous work before or after UseAsync.");
                        }
                        return result;
                    }
                    finally
                    {
                        token.Dispose();
                    }
                }
                retries--;
                if (retries < 0)
                {
                    var blocker = ClipboardBlocker.Detect();
                    throw ClipboardAccessToken.CreateOpenTimeoutException(blocker);
                }
                await Task.Delay(options.RetryInterval, cancellationToken);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Create the token for an opened clipboard, disposing it closes the clipboard and releases the semaphore (once).
    /// </summary>
    /// <param name="hWnd">IntPtr with the window handle which was used to open the clipboard</param>
    /// <returns>ClipboardAccessToken</returns>
    private ClipboardAccessToken CreateOpenToken(IntPtr hWnd)
    {
        var ownerThreadId = Environment.CurrentManagedThreadId;
        return new ClipboardAccessToken(() => {
            try
            {
                if (!CloseClipboard())
                {
                    Trace.TraceWarning("Dapplo.Windows.Clipboard: CloseClipboard failed with error {0}, the clipboard was opened on thread {1} and closed on thread {2}.",
                        Marshal.GetLastWin32Error(), ownerThreadId, Environment.CurrentManagedThreadId);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        })
        {
            OwnerHandle = hWnd
        };
    }


    /// <summary>
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
    ///     Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
    /// </summary>
    /// <param name="hWndNewOwner">IntPtr with the hWnd of the new owner. If this parameter is NULL, the open clipboard is associated with the current task.</param>
    /// <returns>true if the clipboard is opened</returns>
    [DllImport("user32", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    /// <summary>
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
    ///     Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
    /// </summary>
    /// <returns>true if the clipboard is closed</returns>
    [DllImport("user32", SetLastError = true)]
    private static extern bool CloseClipboard();

    /// <summary>
    ///     Dispose the current async lock, and it's underlying SemaphoreSlim
    /// </summary>
    private void DisposeInternal()
    {
        if (_disposedValue)
        {
            return;
        }
        _semaphoreSlim.Dispose();

        _disposedValue = true;
    }

    /// <summary>
    ///     Finalizer, as it would be bad to leave a SemaphoreSlim hanging around
    /// </summary>
    ~ClipboardSemaphore()
    {
        DisposeInternal();
    }

    /// <summary>
    ///     Implementation of the IDisposable
    /// </summary>
    public void Dispose()
    {
        DisposeInternal();
        // Make sure the finalizer for this instance is not called, as we already did what we need to do
        GC.SuppressFinalize(this);
    }
}