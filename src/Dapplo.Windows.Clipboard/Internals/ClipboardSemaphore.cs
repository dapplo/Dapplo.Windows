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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Clipboard.Internals
{
    /// <summary>
    /// This can be used to get a lock to the clipboard, and free it again.
    /// </summary>
    internal sealed class ClipboardSemaphore : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMilliseconds(200);
        private static readonly LogSource Log = new LogSource();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        // To detect redundant calls
        private bool _disposedValue;

        /// <summary>
        /// Get a lock to the clipboard
        /// </summary>
        /// <param name="hWnd">IntPtr with a hWnd for the potential new owner</param>
        /// <param name="retries">int with number of retries, default is 5</param>
        /// <param name="retryInterval">TimeSpan for the time between retries</param>
        /// <param name="timeout">optional TimeSpan for the timeout, default is 400ms</param>
        /// <returns>IClipboardLock</returns>
        public IClipboardAccessToken Lock(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null)
        {
            // Set default retry interval
            retryInterval = retryInterval ?? DefaultRetryInterval;
            // Set default timeout interval
            timeout = timeout ?? DefaultTimeout;

#if !NETSTANDARD2_0
            if (hWnd == IntPtr.Zero)
            {
                // Take the default
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Taking windows handle {0} from the WinProcHandler", WinProcHandler.Instance.Handle);
                }

                hWnd = WinProcHandler.Instance.Handle;
            }
#endif

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
                return new ClipboardAccessToken
                {
                    CanAccess = false,
                    IsOpenTimeout = true
                };
            }
            // Return a disposable which cleans up the current state.
            return new ClipboardAccessToken(() => {
                CloseClipboard();
                _semaphoreSlim.Release();
            });
        }

        /// <summary>
        /// Lock the clipboard, return a disposable which can free this again.
        /// </summary>
        /// <param name="hWnd">IntPtr with the hWnd of the potential new owner</param>
        /// <param name="retries">int with the number of retries</param>
        /// <param name="retryInterval">optional TimeSpan</param>
        /// <param name="timeout">optional TimeSpan for the timeout, default is 400ms</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task with IClipboardLock</returns>
        public async ValueTask<IClipboardAccessToken> LockAsync(IntPtr hWnd = default, int retries = 5, TimeSpan? retryInterval = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            // Set default retry interval
            retryInterval = retryInterval ?? DefaultRetryInterval;
            // Set default timeout interval
            timeout = timeout ?? DefaultTimeout;

#if !NETSTANDARD2_0
            if (hWnd == IntPtr.Zero)
            {
                // Take the default
                hWnd = WinProcHandler.Instance.Handle;
            }
#endif

            // Await the semaphore, until the timeout is triggered
            if (!await _semaphoreSlim.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false))
            {
                // Timeout
                return new ClipboardAccessToken
                {
                    CanAccess = false,
                    IsLockTimeout = true
                };
            }

            bool isOpened = false;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (OpenClipboard(hWnd))
                {
                    isOpened = true;
                    break;
                }
                retries--;
                // No reason to delay, if there are no more retries
                if (retries >= 0)
                {
                    await Task.Delay(retryInterval.Value, cancellationToken).ConfigureAwait(false);
                }
            } while (retries >= 0);

            if (!isOpened)
            {
                // Timeout
                return new ClipboardAccessToken
                {
                    CanAccess = false,
                    IsOpenTimeout = true
                };
            }

            return new ClipboardAccessToken(() => {
                CloseClipboard();
                _semaphoreSlim.Release();
            });
        }


#region Native

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

#endregion

#region IDisposable Support

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

#endregion
    }
}
