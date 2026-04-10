// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.SystemState.Enums;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// Provides access to Windows system state APIs, including thread execution state
/// and waitable timer functions.
/// </summary>
public static class SystemStateApi
{
    private const string Kernel32Dll = "kernel32.dll";

    /// <summary>
    /// Enables an application to inform the system that it is in use, thereby preventing the system
    /// from entering sleep or turning off the display while the application is running.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate">SetThreadExecutionState function</a>
    /// </summary>
    /// <param name="esFlags">The thread's execution requirements. Can be a combination of <see cref="ThreadExecutionStateFlags"/>.</param>
    /// <returns>
    /// If the function succeeds, the return value is the previous thread execution state.
    /// If the function fails, the return value is <c>0</c>.
    /// </returns>
    [DllImport(Kernel32Dll, SetLastError = true)]
    public static extern ThreadExecutionStateFlags SetThreadExecutionState(ThreadExecutionStateFlags esFlags);

    /// <summary>
    /// Creates or opens a waitable timer object.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createwaitabletimerw">CreateWaitableTimer function</a>
    /// </summary>
    /// <param name="lpTimerAttributes">
    /// A pointer to a SECURITY_ATTRIBUTES structure. If this parameter is <see cref="IntPtr.Zero"/>,
    /// the timer handle cannot be inherited by child processes.
    /// </param>
    /// <param name="bManualReset">
    /// If <c>true</c>, creates a manual-reset notification timer. If <c>false</c>, creates a synchronization timer.
    /// </param>
    /// <param name="lpTimerName">The name of the timer object. If <c>null</c>, creates an unnamed timer.</param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the timer object.
    /// If the function fails, the return value is <see cref="IntPtr.Zero"/>. Call GetLastError for extended error information.
    /// </returns>
    [DllImport(Kernel32Dll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, string lpTimerName);

    /// <summary>
    /// Opens an existing named waitable timer object.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-openwaitabletimerw">OpenWaitableTimer function</a>
    /// </summary>
    /// <param name="dwDesiredAccess">The access to the timer object. The TIMER_ALL_ACCESS (0x1F0003) flag is typically used.</param>
    /// <param name="bInheritHandle">
    /// If <c>true</c>, processes created by this process will inherit the handle.
    /// </param>
    /// <param name="lpTimerName">The name of the timer object to open.</param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the timer object.
    /// If the function fails, the return value is <see cref="IntPtr.Zero"/>.
    /// </returns>
    [DllImport(Kernel32Dll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr OpenWaitableTimer(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpTimerName);

    /// <summary>
    /// Activates the specified waitable timer. When the due time arrives, the timer is signaled
    /// and the optional completion routine is called.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-setwaitabletimer">SetWaitableTimer function</a>
    /// </summary>
    /// <param name="hTimer">A handle to the timer object. The CreateWaitableTimer or OpenWaitableTimer function returns this handle.</param>
    /// <param name="pDueTime">
    /// The time after which the state of the timer is to be set to signaled. This must be a negative value
    /// expressed in 100-nanosecond intervals (e.g., -10000000 for 1 second). A positive value specifies an
    /// absolute time in UTC.
    /// </param>
    /// <param name="lPeriod">
    /// The period of the timer, in milliseconds. If zero, the timer is signaled once. If greater than zero,
    /// the timer is periodic.
    /// </param>
    /// <param name="pfnCompletionRoutine">
    /// A pointer to an optional completion routine. If <see cref="IntPtr.Zero"/>, no routine is called.
    /// </param>
    /// <param name="lpArgToCompletionRoutine">
    /// A pointer to a structure that is passed to the completion routine. Ignored if <paramref name="pfnCompletionRoutine"/> is null.
    /// </param>
    /// <param name="fResume">
    /// If <c>true</c> and the system supports it, restores a system in suspended sleep or hibernation when the timer fires.
    /// Requires the SE_SYSTEMTIME_NAME privilege.
    /// </param>
    /// <returns><c>true</c> if the function succeeds; otherwise <c>false</c>.</returns>
    [DllImport(Kernel32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWaitableTimer(IntPtr hTimer, ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);

    /// <summary>
    /// Sets a cancel on a waitable timer, so it is no longer activated.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-cancelwaitabletimer">CancelWaitableTimer function</a>
    /// </summary>
    /// <param name="hTimer">A handle to the timer object.</param>
    /// <returns><c>true</c> if the function succeeds; otherwise <c>false</c>.</returns>
    [DllImport(Kernel32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CancelWaitableTimer(IntPtr hTimer);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">A valid handle to an open object.</param>
    /// <returns><c>true</c> if the function succeeds; otherwise <c>false</c>.</returns>
    [DllImport(Kernel32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Keeps the system awake and prevents the screen from turning off until <see cref="AllowSleep"/> is called.
    /// Equivalent to calling SetThreadExecutionState with ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED.
    /// </summary>
    /// <returns>The previous execution state, or <c>0</c> on failure.</returns>
    public static ThreadExecutionStateFlags PreventSleep() =>
        SetThreadExecutionState(
            ThreadExecutionStateFlags.ES_CONTINUOUS |
            ThreadExecutionStateFlags.ES_SYSTEM_REQUIRED |
            ThreadExecutionStateFlags.ES_DISPLAY_REQUIRED);

    /// <summary>
    /// Keeps the system awake (without keeping the screen on) until <see cref="AllowSleep"/> is called.
    /// Equivalent to calling SetThreadExecutionState with ES_CONTINUOUS | ES_SYSTEM_REQUIRED.
    /// </summary>
    /// <returns>The previous execution state, or <c>0</c> on failure.</returns>
    public static ThreadExecutionStateFlags PreventSystemSleep() =>
        SetThreadExecutionState(
            ThreadExecutionStateFlags.ES_CONTINUOUS |
            ThreadExecutionStateFlags.ES_SYSTEM_REQUIRED);

    /// <summary>
    /// Allows the system to sleep and the screen to turn off when idle.
    /// Equivalent to calling SetThreadExecutionState with ES_CONTINUOUS.
    /// </summary>
    /// <returns>The previous execution state, or <c>0</c> on failure.</returns>
    public static ThreadExecutionStateFlags AllowSleep() =>
        SetThreadExecutionState(ThreadExecutionStateFlags.ES_CONTINUOUS);
}
