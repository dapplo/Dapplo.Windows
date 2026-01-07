// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Kernel32.Structs;

namespace Dapplo.Windows.Kernel32;

/// <summary>
///     Restart Manager API functionality
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-portal">Restart Manager</a>
/// </summary>
public static class RestartManagerApi
{
    private const string Rstrtmgr = "rstrtmgr.dll";

    /// <summary>
    ///     Maximum length of a session key.
    /// </summary>
    public const int RmSessionKeyLen = 32; // sizeof(GUID)

    /// <summary>
    ///     Invalid session value.
    /// </summary>
    public const int RmInvalidSession = -1;

    /// <summary>
    ///     Invalid Terminal Services session ID.
    /// </summary>
    public const uint RmInvalidTsSession = 0xFFFFFFFF;

    /// <summary>
    ///     Starts a new Restart Manager session.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmstartsession">RmStartSession function</a>
    /// </summary>
    /// <param name="pSessionHandle">
    ///     A pointer to the handle of a Restart Manager session.
    ///     The session handle can be passed in subsequent calls to the Restart Manager API.
    /// </param>
    /// <param name="dwSessionFlags">Reserved. This parameter should be 0.</param>
    /// <param name="strSessionKey">
    ///     A null-terminated string that contains the session key to the new session.
    ///     The string must be allocated before calling this function and should be at least CCH_RM_SESSION_KEY characters in length.
    /// </param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success, or an error code on failure.
    /// </returns>
    [DllImport(Rstrtmgr, CharSet = CharSet.Unicode)]
    public static extern int RmStartSession(out int pSessionHandle, int dwSessionFlags, StringBuilder strSessionKey);

    /// <summary>
    ///     Ends the Restart Manager session.
    ///     This function should be called by the primary installer that has previously started the session by calling the RmStartSession function.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmendsession">RmEndSession function</a>
    /// </summary>
    /// <param name="dwSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success, or an error code on failure.
    /// </returns>
    [DllImport(Rstrtmgr)]
    public static extern int RmEndSession(int dwSessionHandle);

    /// <summary>
    ///     Registers resources to a Restart Manager session.
    ///     The Restart Manager uses the list of resources registered with the session to determine which applications and services must be shut down and restarted.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmregisterresources">RmRegisterResources function</a>
    /// </summary>
    /// <param name="dwSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <param name="nFiles">The number of files being registered.</param>
    /// <param name="rgsFilenames">
    ///     An array of null-terminated strings of full filename paths.
    ///     This parameter can be NULL if nFiles is 0.
    /// </param>
    /// <param name="nApplications">The number of processes being registered.</param>
    /// <param name="rgApplications">
    ///     An array of RM_UNIQUE_PROCESS structures.
    ///     This parameter can be NULL if nApplications is 0.
    /// </param>
    /// <param name="nServices">The number of services to be registered.</param>
    /// <param name="rgsServiceNames">
    ///     An array of null-terminated strings of service short names.
    ///     This parameter can be NULL if nServices is 0.
    /// </param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success, or an error code on failure.
    /// </returns>
    [DllImport(Rstrtmgr, CharSet = CharSet.Unicode)]
    public static extern int RmRegisterResources(
        int dwSessionHandle,
        uint nFiles,
        string[] rgsFilenames,
        uint nApplications,
        RmUniqueProcess[] rgApplications,
        uint nServices,
        string[] rgsServiceNames);

    /// <summary>
    ///     Gets a list of all applications and services that are currently using resources that have been registered with the Restart Manager session.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmgetlist">RmGetList function</a>
    /// </summary>
    /// <param name="dwSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <param name="pnProcInfoNeeded">
    ///     A pointer to an array size necessary to receive RM_PROCESS_INFO results.
    ///     If the buffer size is insufficient, this parameter receives the size required.
    /// </param>
    /// <param name="pnProcInfo">
    ///     A pointer to the total number of RM_PROCESS_INFO structures that are returned to the caller.
    /// </param>
    /// <param name="rgAffectedApps">
    ///     An array of RM_PROCESS_INFO structures that list the applications and services using resources that have been registered with the session.
    ///     This parameter can be NULL if lpdwRebootReasons is not NULL.
    /// </param>
    /// <param name="lpdwRebootReasons">
    ///     Pointer to location that receives a value of the RM_REBOOT_REASON enumeration that describes the reason a system restart is needed.
    /// </param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success.
    ///     Returns ERROR_MORE_DATA if the rgAffectedApps buffer is too small.
    ///     Returns an error code on other failures.
    /// </returns>
    [DllImport(Rstrtmgr)]
    public static extern int RmGetList(
        int dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RmProcessInfo[] rgAffectedApps,
        out RmRebootReason lpdwRebootReasons);

    /// <summary>
    ///     Initiates the shutdown of applications.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmshutdown">RmShutdown function</a>
    /// </summary>
    /// <param name="dwSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <param name="lActionFlags">
    ///     One or more RM_SHUTDOWN_TYPE values that configure the shut down of components.
    /// </param>
    /// <param name="fnStatus">
    ///     A pointer to a status message callback function that is used to communicate status while the RmShutdown function is executing.
    ///     This parameter can be NULL if you do not want to receive status updates.
    /// </param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success, or an error code on failure.
    /// </returns>
    [DllImport(Rstrtmgr)]
    public static extern int RmShutdown(
        int dwSessionHandle,
        RmShutdownType lActionFlags,
        RmStatusCallback fnStatus);

    /// <summary>
    ///     Restarts applications and services that have been shut down by the RmShutdown function and that have been registered to be restarted using the RegisterApplicationRestart function.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nf-restartmanager-rmrestart">RmRestart function</a>
    /// </summary>
    /// <param name="dwSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <param name="dwRestartFlags">Reserved. This parameter should be 0.</param>
    /// <param name="fnStatus">
    ///     A pointer to a status message callback function that is used to communicate status while the RmRestart function is executing.
    ///     This parameter can be NULL if you do not want to receive status updates.
    /// </param>
    /// <returns>
    ///     Returns ERROR_SUCCESS (0) on success, or an error code on failure.
    /// </returns>
    [DllImport(Rstrtmgr)]
    public static extern int RmRestart(
        int dwSessionHandle,
        int dwRestartFlags,
        RmStatusCallback fnStatus);

    /// <summary>
    ///     Callback function used by RmShutdown and RmRestart to report status updates.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/nc-restartmanager-rm_write_status_callback">RM_WRITE_STATUS_CALLBACK callback function</a>
    /// </summary>
    /// <param name="nPercentComplete">An integer value between 0 and 100 that indicates the percentage of the total number of applications that have either been shut down or restarted.</param>
    public delegate void RmStatusCallback(uint nPercentComplete);
}
