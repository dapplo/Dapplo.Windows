// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Kernel32.Structs;

/// <summary>
///     Describes an application that is to be registered with the Restart Manager.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ns-restartmanager-rm_process_info">RM_PROCESS_INFO structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Sonar Code Smell", "S1144:Unused private types or members should be removed", Justification = "Interop!")]
public struct RmProcessInfo
{
    private const int RmMaxAppName = 255;
    private const int RmMaxSvcName = 63;
    private const int CchRmMaxAppName = RmMaxAppName + 1;
    private const int CchRmMaxSvcName = RmMaxSvcName + 1;

    /// <summary>
    ///     Contains an RmUniqueProcess structure that uniquely identifies the application by its PID and the time the process began.
    /// </summary>
    public RmUniqueProcess Process;

    /// <summary>
    ///     If the process is a service, this parameter returns the long name for the service.
    ///     If the process is not a service, this parameter returns the user-friendly name for the application.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxAppName)]
    public string strAppName;

    /// <summary>
    ///     If the process is a service, this is the short name for the service.
    ///     This member is not used if the process is not a service.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxSvcName)]
    public string strServiceShortName;

    /// <summary>
    ///     Contains an RM_APP_TYPE enumeration value that specifies the type of application as RmUnknownApp, RmMainWindow, RmOtherWindow, RmService, RmExplorer or RmCritical.
    /// </summary>
    public RmAppType ApplicationType;

    /// <summary>
    ///     Contains a bit mask that describes the current status of the application.
    /// </summary>
    public RmAppStatus AppStatus;

    /// <summary>
    ///     Contains the Terminal Services session ID of the process.
    ///     If the terminal session of the process cannot be determined, the value of this member is set to RM_INVALID_SESSION (-1).
    ///     This member is not used if the process is a service or a system critical process.
    /// </summary>
    public uint TSSessionId;

    /// <summary>
    ///     TRUE if the application can be restarted by the Restart Manager; otherwise, FALSE.
    ///     This member is always TRUE if the process is a service.
    ///     This member is always FALSE if the process is a critical system process.
    /// </summary>
    [MarshalAs(UnmanagedType.Bool)]
    public bool bRestartable;
}
