// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// A managed wrapper around a Windows waitable timer that can optionally wake the system from sleep or hibernation.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/sync/waitable-timer-objects">Waitable Timer Objects</a>
/// </summary>
public sealed class WaitableTimer : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the timer has been created successfully.
    /// </summary>
    public bool IsValid => _handle != IntPtr.Zero;

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
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create waitable timer. Error: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
        }
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
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create waitable timer '{name}'. Error: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
        }
    }

    /// <summary>
    /// Sets the timer to fire once after the specified delay.
    /// </summary>
    /// <param name="delay">The delay before the timer fires.</param>
    /// <param name="wakeSystem">
    /// If <c>true</c>, the system will be woken from sleep or hibernation when the timer fires.
    /// Requires the SE_SYSTEMTIME_NAME privilege.
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
    /// <param name="dueTime">The UTC time at which the timer should fire.</param>
    /// <param name="wakeSystem">
    /// If <c>true</c>, the system will be woken from sleep or hibernation when the timer fires.
    /// Requires the SE_SYSTEMTIME_NAME privilege.
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
    /// Requires the SE_SYSTEMTIME_NAME privilege.
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
        using var waitHandle = new WaitableTimerWaitHandle(_handle);
        return waitHandle.WaitOne(timeout);
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
        if (_handle != IntPtr.Zero)
        {
            SystemStateApi.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// A WaitHandle wrapper for the waitable timer that does not own the handle (no close on finalize).
    /// </summary>
    private sealed class WaitableTimerWaitHandle : WaitHandle
    {
        public WaitableTimerWaitHandle(IntPtr handle)
        {
            SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle, ownsHandle: false);
        }
    }
}
