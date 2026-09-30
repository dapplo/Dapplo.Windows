// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure (WF_SESSION_TIME) is returned when WFQuerySessionInformation is called with WFInfoClasses.SessionTime.
///     All values are LARGE_INTEGER FILETIME values (100-nanosecond intervals since 1601-01-01 UTC).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SessionTime
{
    private readonly long _connectTime;
    private readonly long _disconnectTime;
    private readonly long _lastInputTime;
    private readonly long _logonTime;
    private readonly long _currentTime;

    /// <summary>
    ///     Return the time the session was (last) connected, in UTC, or null when not available
    /// </summary>
    public DateTime? ConnectTime => FromFileTime(_connectTime);

    /// <summary>
    ///     Return the last disconnect time, in UTC, or null when the session was never disconnected
    /// </summary>
    public DateTime? DisconnectTime => FromFileTime(_disconnectTime);

    /// <summary>
    ///     Return the last input time, in UTC, or null when not available
    /// </summary>
    public DateTime? LastInputTime => FromFileTime(_lastInputTime);

    /// <summary>
    ///     Return the logon time, in UTC, or null when not available
    /// </summary>
    public DateTime? LogonTime => FromFileTime(_logonTime);

    /// <summary>
    ///     Return the current time, in UTC, or null when not available
    /// </summary>
    public DateTime? CurrentTime => FromFileTime(_currentTime);

    private static DateTime? FromFileTime(long fileTime) => fileTime <= 0 ? null : DateTime.FromFileTimeUtc(fileTime);
}
