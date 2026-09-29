// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    private const string Advapi32Dll = "advapi32.dll";
    private const string Kernel32Dll = "kernel32.dll";
    private const string SeShutdownName = "SeShutdownPrivilege";
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint SePrivilegeEnabled = 0x00000002;
    private const int ErrorNotAllAssigned = 1300;

    /// <summary>
    /// The shutdown reason which is used by <see cref="Shutdown"/> and <see cref="Restart"/>:
    /// SHTDN_REASON_MAJOR_OTHER | SHTDN_REASON_MINOR_OTHER | SHTDN_REASON_FLAG_PLANNED, which logs a planned shutdown.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/shutdown/system-shutdown-reason-codes">System Shutdown Reason Codes</a>
    /// </summary>
    public const uint ShutdownReasonPlannedOther = 0x80000000;

    // TOKEN_PRIVILEGES with one LUID_AND_ATTRIBUTES, the LUID is 4 byte aligned
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public long Luid;
        public uint Attributes;
    }

    [DllImport(Advapi32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport(Advapi32Dll, EntryPoint = "LookupPrivilegeValueW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string systemName, string name, out long luid);

    [DllImport(Advapi32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport(Kernel32Dll)]
    private static extern IntPtr GetCurrentProcess();

    // No SetLastError: this must not overwrite the last error of the privilege adjustment
    [DllImport(Kernel32Dll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

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
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.U1)] bool hibernate,
        [MarshalAs(UnmanagedType.U1)] bool forceCritical,
        [MarshalAs(UnmanagedType.U1)] bool disableWakeEvent);

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
    /// Enables the SE_SHUTDOWN_NAME privilege for the current process. Interactive users hold this privilege, but it is disabled by default,
    /// and ExitWindowsEx (shutdown / restart) or InitiateSystemShutdown require it to be enabled.
    /// </summary>
    /// <returns><c>true</c> if the privilege is enabled, <c>false</c> if the user doesn't hold it or the token could not be adjusted (see Marshal.GetLastWin32Error).</returns>
    public static bool EnableShutdownPrivilege()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var tokenHandle))
        {
            return false;
        }
        try
        {
            if (!LookupPrivilegeValue(null, SeShutdownName, out var luid))
            {
                return false;
            }
            var tokenPrivileges = new TokenPrivileges
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SePrivilegeEnabled
            };
            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                return false;
            }
            // AdjustTokenPrivileges also succeeds when the privilege is not held, this is signalled with ERROR_NOT_ALL_ASSIGNED
            return Marshal.GetLastWin32Error() != ErrorNotAllAssigned;
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }

    /// <summary>
    /// Shuts down the system and turns off the power (EWX_POWEROFF).
    /// This enables the SE_SHUTDOWN_NAME privilege of the process, which the user must hold.
    /// </summary>
    /// <param name="force">If <c>true</c>, forces running applications to close (EWX_FORCE), which can cause them to lose data.
    /// Otherwise applications which don't respond are only terminated after a timeout (EWX_FORCEIFHUNG).</param>
    /// <param name="reason">The shutdown reason which is logged, default is a planned shutdown with <see cref="ShutdownReasonPlannedOther"/></param>
    /// <returns><c>true</c> if the operation was initiated successfully, otherwise see Marshal.GetLastWin32Error (e.g. 1314 ERROR_PRIVILEGE_NOT_HELD).</returns>
    public static bool Shutdown(bool force = false, uint reason = ShutdownReasonPlannedOther)
    {
        return ExitWindows(ExitWindowsFlags.EWX_POWEROFF, force, reason);
    }

    /// <summary>
    /// Restarts the system (EWX_REBOOT).
    /// This enables the SE_SHUTDOWN_NAME privilege of the process, which the user must hold.
    /// </summary>
    /// <param name="force">If <c>true</c>, forces running applications to close (EWX_FORCE), which can cause them to lose data.
    /// Otherwise applications which don't respond are only terminated after a timeout (EWX_FORCEIFHUNG).</param>
    /// <param name="reason">The shutdown reason which is logged, default is a planned shutdown with <see cref="ShutdownReasonPlannedOther"/></param>
    /// <returns><c>true</c> if the operation was initiated successfully, otherwise see Marshal.GetLastWin32Error (e.g. 1314 ERROR_PRIVILEGE_NOT_HELD).</returns>
    public static bool Restart(bool force = false, uint reason = ShutdownReasonPlannedOther)
    {
        return ExitWindows(ExitWindowsFlags.EWX_REBOOT, force, reason);
    }

    /// <summary>
    /// Enable the shutdown privilege and call ExitWindowsEx
    /// </summary>
    private static bool ExitWindows(ExitWindowsFlags flags, bool force, uint reason)
    {
        if (!EnableShutdownPrivilege())
        {
            // The last error of the privilege adjustment is preserved for the caller
            return false;
        }
        flags |= force ? ExitWindowsFlags.EWX_FORCE : ExitWindowsFlags.EWX_FORCEIFHUNG;
        return ExitWindowsEx(flags, reason);
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
