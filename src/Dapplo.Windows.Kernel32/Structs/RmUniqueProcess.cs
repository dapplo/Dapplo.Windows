// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Kernel32.Structs;

/// <summary>
///     Uniquely identifies a process by its PID and the time the process began.
///     An array of RmUniqueProcess structures can be passed to the RmRegisterResources function.
///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/restartmanager/ns-restartmanager-rm_unique_process">RM_UNIQUE_PROCESS structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct RmUniqueProcess
{
    /// <summary>
    ///     The process identifier (PID).
    /// </summary>
    public int dwProcessId;

    /// <summary>
    ///     The creation time of the process. The time is provided as a FILETIME structure.
    /// </summary>
    public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
}
