// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages.Structs;

/// <summary>
/// Represents a message indicating that a Windows session is ending, including the message type and the reason for session termination.
/// </summary>
/// <remarks>Use this type to encapsulate information about session end events received from the Windows message
/// loop. The message can be handled to prevent further processing by the default window procedure.</remarks>
/// <param name="Msg">The Windows message type associated with the session end event.</param>
/// <param name="EndSessionReason">The reason for the session termination, specifying why the session is ending.</param>
public record struct EndSessionMessage(WindowsMessages Msg, EndSessionReasons EndSessionReason)
{
    /// <summary>
    /// This has to be set to true to indicate that the message has been handled and should not be processed further by the default window procedure.
    /// </summary>
    public bool Handled { get; set; } = false;

    /// <summary>
    /// This result is passed back to the system to indicate the outcome of processing the session end message.
    /// </summary>
    public nuint Result { get; set; } = 0;
}

