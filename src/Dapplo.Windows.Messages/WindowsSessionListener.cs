// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Messages.Enums;

namespace Dapplo.Windows.Messages;

/// <summary>
///     A listener for Windows session change events.
///     Currently handles lock/unlock and logon/logoff events.
///     Other session change events (console connect/disconnect, remote connect/disconnect, etc.) are not exposed but can be added in the future.
/// </summary>
public class WindowsSessionListener : IDisposable
{
    private const int RegistrationRetryIntervalMilliseconds = 2000;
    private const int MaxRegistrationAttempts = 60;
    private IDisposable _subscription;
    private volatile bool _isPaused;
    private volatile bool _isDisposed;
    private readonly object _lock = new object();
    // The following fields are only used on the thread of the SharedMessageWindow
    private bool _isActive;
    private volatile bool _isRegistered;
    private int _registrationAttempts;
    private Timer _retryTimer;

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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

    /// <summary>
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wtsapi32/nf-wtsapi32-wtsunregistersessionnotification">WTSUnRegisterSessionNotification function</a>
    /// Unregisters the specified window so that it receives no further session change notifications.
    /// </summary>
    /// <param name="hWnd">Handle to the window to stop receiving session change notifications</param>
    /// <returns>Returns true if successful</returns>
    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
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
    ///     Event fired when registering for session notifications (WTSRegisterSessionNotification) finally failed.
    ///     Registration can fail early at logon (RPC_S_INVALID_BINDING) when the Remote Desktop Services are not started yet,
    ///     therefore a failed registration is retried every 2 seconds for about 2 minutes before this event is raised.
    ///     The event is raised on the thread of the SharedMessageWindow, the exception is a <see cref="Win32Exception"/>.
    /// </summary>
    public event EventHandler<ErrorEventArgs> RegistrationFailed;

    /// <summary>
    ///     True when the listener is registered for session notifications.
    /// </summary>
    public bool IsRegistered => _isRegistered;

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
                    _isActive = true;
                    _registrationAttempts = 0;
                    TryRegister(hwnd);
                },
                onTeardown: hwnd =>
                {
                    _isActive = false;
                    _retryTimer?.Dispose();
                    _retryTimer = null;
                    if (_isRegistered)
                    {
                        _isRegistered = false;
                        WTSUnRegisterSessionNotification(hwnd);
                    }
                }
            )
            .Subscribe(m =>
            {
                if (_isPaused || m.Msg != WindowsMessages.WM_WTSSESSION_CHANGE)
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
    ///     Register for session notifications, runs on the thread of the SharedMessageWindow and never throws.
    ///     A failure is retried with a timer, and reported with RegistrationFailed after the last attempt.
    /// </summary>
    /// <param name="hwnd">The handle of the SharedMessageWindow</param>
    private void TryRegister(nint hwnd)
    {
        if (!_isActive || _isRegistered)
        {
            return;
        }
        _registrationAttempts++;
        if (WTSRegisterSessionNotification(hwnd, NOTIFY_FOR_THIS_SESSION))
        {
            _isRegistered = true;
            return;
        }
        var error = Marshal.GetLastWin32Error();
        if (_registrationAttempts < MaxRegistrationAttempts)
        {
            _retryTimer?.Dispose();
            _retryTimer = new Timer(_ => RetryRegistration(), null, RegistrationRetryIntervalMilliseconds, Timeout.Infinite);
            return;
        }

        var exception = new Win32Exception(error, $"WTSRegisterSessionNotification failed after {_registrationAttempts} attempts (error {error}): {new Win32Exception(error).Message}");
        try
        {
            RegistrationFailed?.Invoke(this, new ErrorEventArgs(exception));
        }
        catch (Exception ex)
        {
            // Never throw on the window thread
            Trace.TraceError("WindowsSessionListener: a RegistrationFailed handler threw an exception: {0}", ex);
        }
    }

    /// <summary>
    ///     Called from the retry timer (thread pool), marshals the registration to the window thread
    /// </summary>
    private void RetryRegistration()
    {
        try
        {
            SharedMessageWindow.Invoke(TryRegister);
        }
        catch (Exception ex)
        {
            Trace.TraceError("WindowsSessionListener: retrying the session notification registration failed: {0}", ex);
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
