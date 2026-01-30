// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages;

/// <summary>
///     A listener for Windows session change events (lock/unlock, logon/logoff)
/// </summary>
public class WindowsSessionListener : IDisposable
{
    private IDisposable _subscription;
    private bool _isPaused;
    private bool _isDisposed;

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

        if (_subscription != null)
        {
            return; // Already started
        }

        _isPaused = false;
        _subscription = WinProcHandler.Instance.Subscribe(new WinProcHandlerHook(HandleSessionChange));
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
        _subscription?.Dispose();
        _subscription = null;
        _isPaused = false;
    }

    /// <summary>
    ///     Handles the WM_WTSSESSION_CHANGE message
    /// </summary>
    private IntPtr HandleSessionChange(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (_isPaused || (uint)msg != (uint)WindowsMessages.WM_WTSSESSION_CHANGE)
        {
            return IntPtr.Zero;
        }

        var eventType = (WtsSessionChangeEvents)wparam.ToInt32();
        var sessionId = lparam.ToInt32();

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

        return IntPtr.Zero;
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

        Stop();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
#endif
