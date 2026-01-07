// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Kernel32.Enums;

/// <summary>
///     Describes the current status of an application that is acted upon by the Restart Manager.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_app_status">RM_APP_STATUS enumeration</a>
/// </summary>
[Flags]
public enum RmAppStatus : uint
{
    /// <summary>
    ///     The application is in a state that is not described by any other enumerated state.
    /// </summary>
    RmStatusUnknown = 0x0,

    /// <summary>
    ///     The application is currently running.
    /// </summary>
    RmStatusRunning = 0x1,

    /// <summary>
    ///     The Restart Manager has stopped the application.
    /// </summary>
    RmStatusStopped = 0x2,

    /// <summary>
    ///     An action outside the Restart Manager has stopped the application.
    /// </summary>
    RmStatusStoppedOther = 0x4,

    /// <summary>
    ///     The Restart Manager has restarted the application.
    /// </summary>
    RmStatusRestarted = 0x8,

    /// <summary>
    ///     The Restart Manager encountered an error when stopping the application.
    /// </summary>
    RmStatusErrorOnStop = 0x10,

    /// <summary>
    ///     The Restart Manager encountered an error when restarting the application.
    /// </summary>
    RmStatusErrorOnRestart = 0x20,

    /// <summary>
    ///     Shutdown is masked by a filter.
    /// </summary>
    RmStatusShutdownMasked = 0x40,

    /// <summary>
    ///     Restart is masked by a filter.
    /// </summary>
    RmStatusRestartMasked = 0x80
}
