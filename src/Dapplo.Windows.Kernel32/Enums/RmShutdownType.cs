// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Kernel32.Enums;

/// <summary>
///     Configures the shut down of applications.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_shutdown_type">RM_SHUTDOWN_TYPE enumeration</a>
/// </summary>
public enum RmShutdownType : uint
{
    /// <summary>
    ///     Force unresponsive applications and services to shut down after the timeout period.
    ///     An application that does not respond to a shutdown request by the Restart Manager is forced to shut down after 30 seconds.
    ///     A service that does not respond to a shutdown request is forced to shut down after 20 seconds.
    /// </summary>
    RmForceShutdown = 0x1,

    /// <summary>
    ///     Shut down applications if and only if all the applications have been registered for restart using the RegisterApplicationRestart function.
    /// </summary>
    RmShutdownOnlyRegistered = 0x10
}
