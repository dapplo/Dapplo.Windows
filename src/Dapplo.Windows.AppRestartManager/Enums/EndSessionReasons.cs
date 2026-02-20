// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.AppRestartManager.Enums;

/// <summary>
///     Flags for the WM_ENDSESSION message indicating the type of session end event.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/shutdown/wm-endsession">WM_ENDSESSION message</a>
/// </summary>
[Flags]
public enum EndSessionReasons : uint
{
    /// <summary>
    ///     The system is shutting down or restarting (reason was not specified).
    /// </summary>
    None = 0,

    /// <summary>
    ///     The application is using a file that must be replaced, the system is being serviced, or system resources are exhausted.
    ///     This flag is set when the ENDSESSION_CLOSEAPP flag is set.
    /// </summary>
    ENDSESSION_CLOSEAPP = 0x00000001,

    /// <summary>
    ///     The application is forced to shut down because a system component is being updated or a critical system event requires the application to close.
    /// </summary>
    ENDSESSION_CRITICAL = 0x40000000,

    /// <summary>
    ///     The user is logging off.
    /// </summary>
    ENDSESSION_LOGOFF = 0x80000000
}
