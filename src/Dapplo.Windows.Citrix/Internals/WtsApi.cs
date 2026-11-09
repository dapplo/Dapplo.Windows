// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Internals;

/// <summary>
///     The part of wtsapi32.dll which is needed to find the protocol of the current session without Citrix
/// </summary>
internal static class WtsApi
{
    private const int WtsCurrentSession = -1;
    private const int WtsClientProtocolType = 16;

    /// <summary>
    ///     WTSQuerySessionInformation with WTSClientProtocolType for the current session: 0 console, 1 ICA, 2 RDP
    /// </summary>
    /// <returns>ushort with the protocol, or null when it can't be retrieved</returns>
    internal static ushort? QueryClientProtocolType()
    {
        if (!WTSQuerySessionInformationW(IntPtr.Zero, WtsCurrentSession, WtsClientProtocolType, out var buffer, out var bytesReturned))
        {
            return null;
        }
        try
        {
            return bytesReturned >= sizeof(ushort) && buffer != IntPtr.Zero ? (ushort)Marshal.ReadInt16(buffer) : null;
        }
        finally
        {
            WTSFreeMemory(buffer);
        }
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSQuerySessionInformationW(IntPtr hServer, int sessionId, int wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);
}
