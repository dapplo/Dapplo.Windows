// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages;

/// <summary>
///     Event arguments for session change events
/// </summary>
public class SessionChangeEventArgs : EventArgs
{
    /// <summary>
    ///     The type of session change that occurred
    /// </summary>
    public WtsSessionChangeEvents EventType { get; }

    /// <summary>
    ///     The session ID that was affected
    /// </summary>
    public int SessionId { get; }

    /// <summary>
    ///     Creates a new instance of SessionChangeEventArgs
    /// </summary>
    /// <param name="eventType">The type of session change</param>
    /// <param name="sessionId">The session ID</param>
    public SessionChangeEventArgs(WtsSessionChangeEvents eventType, int sessionId)
    {
        EventType = eventType;
        SessionId = sessionId;
    }
}
#endif
