// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Messages.Enumerations;

/// <summary>
///     Session change events for WM_WTSSESSION_CHANGE message
///     See <a href="https://learn.microsoft.com/en-us/windows/win32/termserv/wm-wtssession-change">WM_WTSSESSION_CHANGE</a>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum WtsSessionChangeEvents
{
    /// <summary>
    /// A session was connected to the console terminal.
    /// </summary>
    WTS_CONSOLE_CONNECT = 0x1,

    /// <summary>
    /// A session was disconnected from the console terminal.
    /// </summary>
    WTS_CONSOLE_DISCONNECT = 0x2,

    /// <summary>
    /// A session was connected to the remote terminal.
    /// </summary>
    WTS_REMOTE_CONNECT = 0x3,

    /// <summary>
    /// A session was disconnected from the remote terminal.
    /// </summary>
    WTS_REMOTE_DISCONNECT = 0x4,

    /// <summary>
    /// A user has logged on to the session.
    /// </summary>
    WTS_SESSION_LOGON = 0x5,

    /// <summary>
    /// A user has logged off the session.
    /// </summary>
    WTS_SESSION_LOGOFF = 0x6,

    /// <summary>
    /// A session has been locked.
    /// </summary>
    WTS_SESSION_LOCK = 0x7,

    /// <summary>
    /// A session has been unlocked.
    /// </summary>
    WTS_SESSION_UNLOCK = 0x8,

    /// <summary>
    /// A session has changed its remote controlled status.
    /// </summary>
    WTS_SESSION_REMOTE_CONTROL = 0x9,

    /// <summary>
    /// A session was created.
    /// </summary>
    WTS_SESSION_CREATE = 0xA,

    /// <summary>
    /// A session was terminated.
    /// </summary>
    WTS_SESSION_TERMINATE = 0xB
}
