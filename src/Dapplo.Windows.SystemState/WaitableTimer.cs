// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// A managed wrapper around a Windows waitable timer that can optionally wake the system from sleep or hibernation.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/sync/waitable-timer-objects">Waitable Timer Objects</a>
/// The native handle is owned by a <see cref="SafeWaitHandle"/>, so it is released even when Dispose is forgotten,
/// and disposing while another thread waits does not close the handle under the wait.
/// </summary>
public sealed class WaitableTimer : IDisposable
{
    private readonly SafeWaitHandle _handle;
    private readonly TimerWaitHandle _waitHandle;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the timer has been created successfully and is not disposed.
    /// </summary>
    public bool IsValid => !_handle.IsInvalid && !_handle.IsClosed;

    /// <summary>
    /// Gets a <see cref="System.Threading.WaitHandle"/> for the timer, e.g. for <c>WaitHandle.WaitAny</c>
    /// or <c>ThreadPool.RegisterWaitForSingleObject</c>.
    /// It is owned by this WaitableTimer, don't dispose it.
    /// </summary>
    public WaitHandle WaitHandle
    {
        get
        {
            ThrowIfDisposed();
            return _waitHandle;
        }
    }

    /// <summary>
    /// Creates a new unnamed waitable timer.
    /// </summary>
    /// <param name="manualReset">
    /// If <c>true</c>, creates a manual-reset notification timer.
    /// If <c>false</c>, creates a synchronization timer.
    /// </param>
    public WaitableTimer(bool manualReset = false)
    {
        _handle = SystemStateApi.CreateWaitableTimer(IntPtr.Zero, manualReset, null);
        if (_handle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create waitable timer. Error: {Marshal.GetLastWin32Error()}");
        }
        _waitHandle = new TimerWaitHandle(_handle);
    }

    /// <summary>
    /// Creates or opens a named waitable timer.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="manualReset">
    /// If <c>true</c>, creates a manual-reset notification timer.
    /// If <c>false</c>, creates a synchronization timer.
    /// </param>
    public WaitableTimer(string name, bool manualReset = false)
    {
        _handle = SystemStateApi.CreateWaitableTimer(IntPtr.Zero, manualReset, name);
        if (_handle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create waitable timer '{name}'. Error: {Marshal.GetLastWin32Error()}");
        }
        _waitHandle = new TimerWaitHandle(_handle);
    }

    /// <summary>
    /// Sets the timer to fire once after the specified delay.
    /// </summary>
    /// <param name="delay">The delay before the timer fires.</param>
    /// <param name="wakeSystem">
    /// If <c>true</c>, the system will be woken from sleep or hibernation when the timer fires.
    /// No privilege is needed, but this only works when the "Allow wake timers" power setting is enabled.
    /// </param>
    /// <returns><c>true</c> if the timer was set successfully.</returns>
    public bool SetOnce(TimeSpan delay, bool wakeSystem = false)
    {
        ThrowIfDisposed();
        // Convert TimeSpan to 100-nanosecond intervals (negative = relative time)
        long dueTime = -delay.Ticks;
        return SystemStateApi.SetWaitableTimer(_handle, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, wakeSystem);
    }

    /// <summary>
    /// Sets the timer to fire at the specified absolute UTC time.
    /// </summary>
    /// <param name="dueTime">The point in time at which the timer should fire, the offset is taken into account (it is converted to UTC).</param>
    /// <param name="wakeSystem">
    /// If <c>true</c>, the system will be woken from sleep or hibernation when the timer fires.
    /// No privilege is needed, but this only works when the "Allow wake timers" power setting is enabled.
    /// </param>
    /// <returns><c>true</c> if the timer was set successfully.</returns>
    public bool SetAt(DateTimeOffset dueTime, bool wakeSystem = false)
    {
        ThrowIfDisposed();
        long fileTime = dueTime.ToFileTime();
        return SystemStateApi.SetWaitableTimer(_handle, ref fileTime, 0, IntPtr.Zero, IntPtr.Zero, wakeSystem);
    }

    /// <summary>
    /// Sets the timer to fire periodically.
    /// </summary>
    /// <param name="initialDelay">The delay before the first firing.</param>
    /// <param name="period">The period between subsequent firings, in milliseconds.</param>
    /// <param name="wakeSystem">
    /// If <c>true</c>, the system will be woken from sleep or hibernation on the first firing.
    /// No privilege is needed, but this only works when the "Allow wake timers" power setting is enabled.
    /// </param>
    /// <returns><c>true</c> if the timer was set successfully.</returns>
    public bool SetPeriodic(TimeSpan initialDelay, int period, bool wakeSystem = false)
    {
        ThrowIfDisposed();
        long dueTime = -initialDelay.Ticks;
        return SystemStateApi.SetWaitableTimer(_handle, ref dueTime, period, IntPtr.Zero, IntPtr.Zero, wakeSystem);
    }

    /// <summary>
    /// Cancels the timer so it no longer fires.
    /// </summary>
    /// <returns><c>true</c> if the cancel was successful.</returns>
    public bool Cancel()
    {
        ThrowIfDisposed();
        return SystemStateApi.CancelWaitableTimer(_handle);
    }

    /// <summary>
    /// Waits for the timer to be signaled.
    /// </summary>
    /// <param name="timeout">Maximum time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
    /// <returns><c>true</c> if the timer was signaled; <c>false</c> if the wait timed out.</returns>
    public bool Wait(TimeSpan timeout)
    {
        ThrowIfDisposed();
        // WaitOne keeps a reference on the SafeWaitHandle, a concurrent Dispose closes the handle only after the wait returns
        return _waitHandle.WaitOne(timeout);
    }

    /// <summary>
    /// Waits indefinitely for the timer to be signaled.
    /// </summary>
    public void Wait()
    {
        Wait(Timeout.InfiniteTimeSpan);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WaitableTimer));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Disposing the WaitHandle disposes the owned SafeWaitHandle, which closes the timer handle
        _waitHandle?.Dispose();
        _handle.Dispose();
    }

    /// <summary>
    /// A WaitHandle for the waitable timer, which uses the SafeWaitHandle of the WaitableTimer.
    /// </summary>
    private sealed class TimerWaitHandle : WaitHandle
    {
        public TimerWaitHandle(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }
    }
}
