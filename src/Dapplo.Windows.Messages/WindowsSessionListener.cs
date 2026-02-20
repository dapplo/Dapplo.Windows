// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Native;

namespace Dapplo.Windows.Messages;

/// <summary>
///     A listener for Windows session change events.
///     Currently handles lock/unlock and logon/logoff events.
///     Other session change events (console connect/disconnect, remote connect/disconnect, etc.) are not exposed but can be added in the future.
/// </summary>
public class WindowsSessionListener : IDisposable
{
    private IDisposable _subscription;
    private volatile bool _isPaused;
    private volatile bool _isDisposed;
    private readonly object _lock = new object();

    /// <summary>
    ///     Flags for WtsRegisterSessionNotification
    /// </summary>
    private const int NOTIFY_FOR_THIS_SESSION = 0;

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsregistersessionnotification">WTSRegisterSessionNotification function</a>
    /// Registers the specified window to receive session change notifications.
    /// </summary>
    /// <param name="hWnd">Handle to the window to receive session change notifications</param>
    /// <param name="dwFlags">Specifies which session notifications to receive</param>
    /// <returns>Returns true if successful</returns>
    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsunregistersessionnotification">WTSUnRegisterSessionNotification function</a>
    /// Unregisters the specified window so that it receives no further session change notifications.
    /// </summary>
    /// <param name="hWnd">Handle to the window to stop receiving session change notifications</param>
    /// <returns>Returns true if successful</returns>
    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

    /// <summary>
    ///     Event fired when a session lock or unlock occurs
    /// </summary>
    public event EventHandler<SessionChangeEventArgs> SessionLockChange;

    /// <summary>
    ///     Event fired when a session logon or logoff occurs
    /// </summary>
    public event EventHandler<SessionChangeEventArgs> SessionLogonChange;

    /// <summary>
    ///     Starts listening for session change events
    /// </summary>
    public void Start()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(WindowsSessionListener));
        }

        lock (_lock)
        {
            if (_subscription != null)
            {
                return; // Already started
            }

            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(WindowsSessionListener));
            }

            _isPaused = false;

            _subscription = SharedMessageWindow.Listen(
                onSetup: hwnd =>
                {
                    if (!WTSRegisterSessionNotification((IntPtr)hwnd, NOTIFY_FOR_THIS_SESSION))
                    {
                        throw new InvalidOperationException("Failed to register for session notifications");
                    }
                },
                onTeardown: hwnd => WTSUnRegisterSessionNotification((IntPtr)hwnd)
            )
            .Subscribe(m =>
            {
                if (_isPaused || m.Msg != (uint)WindowsMessages.WM_WTSSESSION_CHANGE)
                {
                    return;
                }

                var eventType = (WtsSessionChangeEvents)(int)m.WParam;
                var sessionId = (int)m.LParam;

                var args = new SessionChangeEventArgs(eventType, sessionId);

                switch (eventType)
                {
                    case WtsSessionChangeEvents.WTS_SESSION_LOCK:
                    case WtsSessionChangeEvents.WTS_SESSION_UNLOCK:
                        SessionLockChange?.Invoke(this, args);
                        break;

                    case WtsSessionChangeEvents.WTS_SESSION_LOGON:
                    case WtsSessionChangeEvents.WTS_SESSION_LOGOFF:
                        SessionLogonChange?.Invoke(this, args);
                        break;
                }
            });
        }
    }

    /// <summary>
    ///     Pauses listening for session change events
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
    }

    /// <summary>
    ///     Resumes listening for session change events after being paused
    /// </summary>
    public void Resume()
    {
        _isPaused = false;
    }

    /// <summary>
    ///     Stops listening for session change events
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
            _isPaused = false;
        }
    }

    /// <summary>
    ///     Disposes the listener and stops listening for events
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
#endif
