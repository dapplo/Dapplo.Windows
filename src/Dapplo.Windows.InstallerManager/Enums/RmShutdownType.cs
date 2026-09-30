// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.InstallerManager.Enums;

/// <summary>
///     Configures the shut down of applications, the values are flags which can be combined (RmShutdown takes "one or more RM_SHUTDOWN_TYPE options").
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_shutdown_type">RM_SHUTDOWN_TYPE enumeration</a>
/// </summary>
[Flags]
public enum RmShutdownType : uint
{
    /// <summary>
    ///     Graceful shutdown (no flags, this value is not part of the native enumeration): the Restart Manager asks the applications and services to shut down,
    ///     and RmShutdown fails with ERROR_FAIL_SHUTDOWN when one of them refuses. Nothing is killed, so no unsaved data is lost.
    /// </summary>
    Graceful = 0x0,

    /// <summary>
    ///     Force unresponsive applications and services to shut down after the timeout period.
    ///     An application that does not respond to a shutdown request by the Restart Manager is forced to shut down after 30 seconds.
    ///     A service that does not respond to a shutdown request is forced to shut down after 20 seconds.
    /// </summary>
    Force = 0x1,

    /// <summary>
    ///     Shut down applications if and only if all the applications have been registered for restart using the RegisterApplicationRestart function.
    /// </summary>
    OnlyRegistered = 0x10
}
