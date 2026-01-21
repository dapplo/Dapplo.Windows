// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Kernel32.Enums;

/// <summary>
///     Flags for the RegisterApplicationRestart function.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-registerapplicationrestart">RegisterApplicationRestart function</a>
/// </summary>
[Flags]
public enum ApplicationRestartFlags : uint
{
    /// <summary>
    ///     No flags set. The application will be restarted with default behavior.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Do not restart the process if it terminates due to an unhandled exception.
    /// </summary>
    RestartNoCrash = 1,

    /// <summary>
    ///     Do not restart the process if it terminates due to the application not responding.
    /// </summary>
    RestartNoHang = 2,

    /// <summary>
    ///     Do not restart the process if it terminates due to the installation of an update.
    /// </summary>
    RestartNoPatch = 4,

    /// <summary>
    ///     Do not restart the process if the computer is restarted as the result of an update.
    /// </summary>
    RestartNoReboot = 8
}
