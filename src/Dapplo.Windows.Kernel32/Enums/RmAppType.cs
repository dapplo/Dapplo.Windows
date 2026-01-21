// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Kernel32.Enums;

/// <summary>
///     Specifies the type of application that is described by the RmProcessInfo structure.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_app_type">RM_APP_TYPE enumeration</a>
/// </summary>
public enum RmAppType
{
    /// <summary>
    ///     The application cannot be classified as any other type. An application of this type can only be shut down by a forced shutdown.
    /// </summary>
    RmUnknownApp = 0,

    /// <summary>
    ///     A Windows application run as a stand-alone process that displays a top-level window.
    /// </summary>
    RmMainWindow = 1,

    /// <summary>
    ///     A Windows application that does not run as a stand-alone process and does not display a top-level window.
    /// </summary>
    RmOtherWindow = 2,

    /// <summary>
    ///     The application is a Windows service.
    /// </summary>
    RmService = 3,

    /// <summary>
    ///     The application is Windows Explorer.
    /// </summary>
    RmExplorer = 4,

    /// <summary>
    ///     The application is a stand-alone console application.
    /// </summary>
    RmConsole = 5,

    /// <summary>
    ///     A system restart is required to complete the installation because a process cannot be shut down.
    /// </summary>
    RmCritical = 1000
}
