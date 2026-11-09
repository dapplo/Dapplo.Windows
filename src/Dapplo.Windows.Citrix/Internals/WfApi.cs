// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Citrix.Enums;

namespace Dapplo.Windows.Citrix.Internals;

/// <summary>
///     The WFAPI functions for the bitness of the process: Citrix ships WFAPI.DLL for 32-bit processes and WFAPI64.DLL for 64-bit processes
///     (the 64-bit VDA has both), so a 64-bit process must not import from WFAPI.DLL.
/// </summary>
internal static class WfApi
{
    private static readonly bool Is64Bit = IntPtr.Size == 8;

    /// <summary>
    ///     The name of the DLL which this process uses
    /// </summary>
    internal static string DllName => Is64Bit ? Wfapi64.DllName : Wfapi32.DllName;

    internal static bool QuerySessionInformation(IntPtr hServer, int sessionId, InfoClasses infoClass, out IntPtr buffer, out int bytesReturned)
        => Is64Bit
            ? Wfapi64.WFQuerySessionInformationW(hServer, sessionId, infoClass, out buffer, out bytesReturned)
            : Wfapi32.WFQuerySessionInformationW(hServer, sessionId, infoClass, out buffer, out bytesReturned);

    internal static void FreeMemory(IntPtr memory)
    {
        if (memory == IntPtr.Zero)
        {
            return;
        }
        if (Is64Bit)
        {
            Wfapi64.WFFreeMemory(memory);
        }
        else
        {
            Wfapi32.WFFreeMemory(memory);
        }
    }

    internal static bool WaitSystemEvent(IntPtr hServer, EventMask eventMask, out EventMask eventFlags)
        => Is64Bit
            ? Wfapi64.WFWaitSystemEvent(hServer, eventMask, out eventFlags)
            : Wfapi32.WFWaitSystemEvent(hServer, eventMask, out eventFlags);

    internal static uint GetActiveProtocol(uint sessionId)
        => Is64Bit ? Wfapi64.WFGetActiveProtocol(sessionId) : Wfapi32.WFGetActiveProtocol(sessionId);

    /// <summary>
    ///     WFAPI.DLL, 32-bit
    /// </summary>
    private static class Wfapi32
    {
        internal const string DllName = "WFAPI";

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WFQuerySessionInformationW(IntPtr hServer, int iSessionId, InfoClasses infotype, out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport(DllName)]
        internal static extern void WFFreeMemory(IntPtr pMemory);

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WFWaitSystemEvent(IntPtr hServer, EventMask eventMask, out EventMask pEventFlags);

        [DllImport(DllName)]
        internal static extern uint WFGetActiveProtocol(uint sessionId);
    }

    /// <summary>
    ///     WFAPI64.DLL, 64-bit
    /// </summary>
    private static class Wfapi64
    {
        internal const string DllName = "WFAPI64";

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WFQuerySessionInformationW(IntPtr hServer, int iSessionId, InfoClasses infotype, out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport(DllName)]
        internal static extern void WFFreeMemory(IntPtr pMemory);

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WFWaitSystemEvent(IntPtr hServer, EventMask eventMask, out EventMask pEventFlags);

        [DllImport(DllName)]
        internal static extern uint WFGetActiveProtocol(uint sessionId);
    }
}
