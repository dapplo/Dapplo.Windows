// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Dapplo.Windows.SystemState.Enums;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// Provides access to Windows power management API functions.
/// </summary>
public static class PowerManagementApi
{
    private const string PowrprofDll = "powrprof.dll";
    private const string User32Dll = "user32.dll";

    /// <summary>
    /// Suspends the system by transitioning it to sleep mode or hibernation.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate">SetSuspendState function</a>
    /// </summary>
    /// <param name="hibernate">
    /// If <c>true</c>, the system hibernates. If <c>false</c>, the system is suspended.
    /// </param>
    /// <param name="forceCritical">
    /// If <c>true</c>, the system is suspended or hibernated immediately without sending the WM_POWERBROADCAST message.
    /// If <c>false</c>, the function broadcasts a WM_POWERBROADCAST message with the PBT_APMSUSPEND parameter value.
    /// Applications that have registered for power notification will have the opportunity to prevent the suspend.
    /// </param>
    /// <param name="disableWakeEvent">
    /// If <c>true</c>, the system disables all wake events. If <c>false</c>, enabled wake events remain enabled.
    /// </param>
    /// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
    [DllImport(PowrprofDll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

    /// <summary>
    /// Logs off the interactive user, shuts down the system, or shuts down and restarts the system.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-exitwindowsex">ExitWindowsEx function</a>
    /// </summary>
    /// <param name="uFlags">The shutdown type. One or more <see cref="ExitWindowsFlags"/> values.</param>
    /// <param name="dwReason">
    /// The reason for initiating the shutdown. This parameter must be one of the system shutdown reason codes.
    /// If this parameter is zero, the SHTDN_REASON_FLAG_PLANNED reason code will not be set, and therefore the default
    /// action is to create an "unplanned" shutdown.
    /// </param>
    /// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
    [DllImport(User32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ExitWindowsEx(ExitWindowsFlags uFlags, uint dwReason = 0);

    /// <summary>
    /// Locks the workstation's display.
    /// Locking a workstation protects it from unauthorized use.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-lockworkstation">LockWorkStation function</a>
    /// </summary>
    /// <returns>
    /// Because the function executes asynchronously, a return value of <c>true</c> indicates that the operation
    /// has been initiated. If <c>false</c>, call GetLastError.
    /// </returns>
    [DllImport(User32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LockWorkStation();

    /// <summary>
    /// Suspends the system (puts it to sleep).
    /// </summary>
    /// <param name="disableWakeEvent">If <c>true</c>, disables all wake events.</param>
    /// <returns><c>true</c> if the system was suspended successfully.</returns>
    public static bool Sleep(bool disableWakeEvent = false) =>
        SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: disableWakeEvent);

    /// <summary>
    /// Hibernates the system.
    /// </summary>
    /// <param name="disableWakeEvent">If <c>true</c>, disables all wake events.</param>
    /// <returns><c>true</c> if the system was hibernated successfully.</returns>
    public static bool Hibernate(bool disableWakeEvent = false) =>
        SetSuspendState(hibernate: true, forceCritical: false, disableWakeEvent: disableWakeEvent);

    /// <summary>
    /// Shuts down the system.
    /// The calling process must have the SE_SHUTDOWN_NAME privilege.
    /// </summary>
    /// <param name="force">If <c>true</c>, forces running applications to close.</param>
    /// <returns><c>true</c> if the operation was initiated successfully.</returns>
    public static bool Shutdown(bool force = false)
    {
        var flags = ExitWindowsFlags.EWX_SHUTDOWN;
        if (force)
        {
            flags |= ExitWindowsFlags.EWX_FORCE;
        }
        return ExitWindowsEx(flags);
    }

    /// <summary>
    /// Restarts the system.
    /// The calling process must have the SE_SHUTDOWN_NAME privilege.
    /// </summary>
    /// <param name="force">If <c>true</c>, forces running applications to close.</param>
    /// <returns><c>true</c> if the operation was initiated successfully.</returns>
    public static bool Restart(bool force = false)
    {
        var flags = ExitWindowsFlags.EWX_REBOOT;
        if (force)
        {
            flags |= ExitWindowsFlags.EWX_FORCE;
        }
        return ExitWindowsEx(flags);
    }

    /// <summary>
    /// Logs off the current user.
    /// </summary>
    /// <param name="force">If <c>true</c>, forces running applications to close.</param>
    /// <returns><c>true</c> if the operation was initiated successfully.</returns>
    public static bool LogOff(bool force = false)
    {
        var flags = ExitWindowsFlags.EWX_LOGOFF;
        if (force)
        {
            flags |= ExitWindowsFlags.EWX_FORCE;
        }
        return ExitWindowsEx(flags);
    }
}
