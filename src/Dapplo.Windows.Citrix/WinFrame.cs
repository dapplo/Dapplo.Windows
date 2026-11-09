// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Citrix.Enums;
using Dapplo.Windows.Citrix.Internals;
using Dapplo.Windows.Citrix.Structs;

namespace Dapplo.Windows.Citrix;

/// <summary>
///     Helper class for the WinFrame API (WFAPI), which is used by Citrix Virtual Apps and Desktops (XenApp / XenDesktop) inside a session.
///     It answers questions about the session the current process runs in. It doesn't help to find out if a window on the local desktop shows
///     a Citrix session, for that see RemoteDisplayWindow in Dapplo.Windows.
/// </summary>
/// <remarks>
///     Citrix ships WFAPI.DLL for 32-bit processes and WFAPI64.DLL for 64-bit processes, the right one is used automatically.
/// </remarks>
public static class WinFrame
{
    private const int CurrentSession = -1;
    private const uint CurrentSessionId = unchecked((uint)-1);
    private static readonly LogSource Log = new LogSource();
    private static readonly IntPtr CurrentServer = IntPtr.Zero;

    /// <summary>
    ///     The name of the WFAPI DLL which is used by this process: WFAPI for a 32-bit process, WFAPI64 for a 64-bit process
    /// </summary>
    public static string DllName => WfApi.DllName;

    /// <summary>
    ///     Checks if WinFrame, the API for Citrix, is available: the WFAPI DLL can be loaded and returns the connect state of the current session
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                return QuerySessionConnectState().HasValue;
            }
            catch (Exception ex) when (IsNotAvailable(ex))
            {
                Log.Debug().WriteLine("Couldn't use {0}.DLL, this only means that the process is not running on Citrix and could be okay. Error: {1}", WfApi.DllName, ex.Message);
            }
            return false;
        }
    }

    /// <summary>
    ///     Checks if the current process runs in a Citrix ICA (HDX) session. This first asks Windows (WTSClientProtocolType), which doesn't need
    ///     Citrix, and falls back to WFGetActiveProtocol when Windows reports the console: a Citrix VDA for desktop operating systems
    ///     (XenDesktop) reports its ICA sessions as console to Windows.
    /// </summary>
    public static bool IsIcaSession
    {
        get
        {
            ushort? windowsProtocol = null;
            try
            {
                windowsProtocol = WtsApi.QueryClientProtocolType();
            }
            catch (Exception ex) when (IsNotAvailable(ex))
            {
                Log.Debug().WriteLine("Couldn't use wtsapi32.dll: {0}", ex.Message);
            }
            switch (windowsProtocol)
            {
                case (ushort)SessionProtocols.Ica:
                    return true;
                case (ushort)SessionProtocols.Rdp:
                    return false;
            }
            return GetActiveProtocol() == SessionProtocols.Ica;
        }
    }

    /// <summary>
    ///     The protocol of the current session according to Citrix (WFGetActiveProtocol)
    /// </summary>
    /// <returns>SessionProtocols, or null when WFAPI isn't available (not on Citrix)</returns>
    public static SessionProtocols? GetActiveProtocol()
    {
        try
        {
            return (SessionProtocols)WfApi.GetActiveProtocol(CurrentSessionId);
        }
        catch (Exception ex) when (IsNotAvailable(ex))
        {
            Log.Debug().WriteLine("Couldn't use {0}.DLL: {1}", WfApi.DllName, ex.Message);
            return null;
        }
    }

    /// <summary>
    ///     Retrieve the ip-address of the client PC
    /// </summary>
    /// <returns>string with the ip address, null when it can't be retrieved or the address is neither IPv4 nor IPv6</returns>
    public static string GetClientIpAddress()
    {
        return TryQuerySessionInformation<ClientAddress>(InfoClasses.ClientAddress, out var clientAddress) ? clientAddress.IpAddress : null;
    }

    /// <summary>
    ///     Retrieve the name of the client PC
    /// </summary>
    /// <returns>string with the hostname of the client, null when it can't be retrieved</returns>
    public static string GetClientName()
    {
        return QuerySessionInformation(InfoClasses.ClientName);
    }

    /// <summary>
    ///     Retrieve the connect state of the current session
    /// </summary>
    /// <returns>Optional ConnectStates, null when it can't be retrieved</returns>
    public static ConnectStates? QuerySessionConnectState()
    {
        if (!WfApi.QuerySessionInformation(CurrentServer, CurrentSession, InfoClasses.ConnectState, out var state, out var bytesReturned))
        {
            return null;
        }
        try
        {
            // ppBuffer points to an INT with the connect state
            return state == IntPtr.Zero || bytesReturned < sizeof(int) ? null : (ConnectStates)Marshal.ReadInt32(state);
        }
        finally
        {
            WfApi.FreeMemory(state);
        }
    }

    /// <summary>
    ///     Retrieve the specified struct
    /// </summary>
    /// <typeparam name="T">type of the struct to return</typeparam>
    /// <param name="infoClass">InfoClasses which matches the struct</param>
    /// <returns>struct of type T, default when it can't be retrieved; use <see cref="TryQuerySessionInformation{T}"/> to tell these apart</returns>
    public static T QuerySessionInformation<T>(InfoClasses infoClass)
        where T : struct
    {
        TryQuerySessionInformation<T>(infoClass, out var result);
        return result;
    }

    /// <summary>
    ///     Retrieve the specified struct
    /// </summary>
    /// <typeparam name="T">type of the struct to return</typeparam>
    /// <param name="infoClass">InfoClasses which matches the struct</param>
    /// <param name="result">struct of type T, default when false is returned</param>
    /// <returns>true when the information was retrieved</returns>
    public static bool TryQuerySessionInformation<T>(InfoClasses infoClass, out T result)
        where T : struct
    {
        result = default;
        if (!WfApi.QuerySessionInformation(CurrentServer, CurrentSession, infoClass, out var addr, out var bytesReturned))
        {
            return false;
        }
        try
        {
            if (addr == IntPtr.Zero || bytesReturned < Marshal.SizeOf<T>())
            {
                return false;
            }
            result = Marshal.PtrToStructure<T>(addr);
            return true;
        }
        finally
        {
            WfApi.FreeMemory(addr);
        }
    }

    /// <summary>
    /// This function waits for an event (ICA session create/delete/connect, user logon/loff, and so on) before it returns.
    /// </summary>
    /// <param name="eventMask">EventMask with event to wait for</param>
    /// <returns>EventMask with the events that happened</returns>
    public static EventMask WaitSystemEvent(EventMask eventMask)
    {
        if (WfApi.WaitSystemEvent(CurrentServer, eventMask, out var result))
        {
            return result;
        }
        // WFWaitSystemEvent sets the last error (SetLastError = true on the import), Win32Exception() picks it up
        throw new Win32Exception();
    }

    /// <summary>
    ///     Retrieve a string value from the WFQuerySessionInformation
    /// </summary>
    /// <param name="infoClass">InfoClasses of a string value, e.g. ClientName</param>
    /// <returns>string with the value, null when it can't be retrieved</returns>
    public static string QuerySessionInformation(InfoClasses infoClass)
    {
        if (!WfApi.QuerySessionInformation(CurrentServer, CurrentSession, infoClass, out var addr, out _))
        {
            return null;
        }
        try
        {
            // Always the Unicode version: WFQuerySessionInformationW
            return addr == IntPtr.Zero ? null : Marshal.PtrToStringUni(addr);
        }
        finally
        {
            WfApi.FreeMemory(addr);
        }
    }

    private static bool IsNotAvailable(Exception ex) => ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;
}
