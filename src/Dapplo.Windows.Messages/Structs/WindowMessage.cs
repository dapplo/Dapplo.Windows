// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages.Structs;

/// <summary>
/// Represents a Windows message, including its window handle, message identifier, and associated parameters.
/// </summary>
/// <remarks>This struct is commonly used when processing Windows messages in low-level window procedures or
/// interop scenarios. The meaning of the parameters depends on the specific message identified by Msg.</remarks>
/// <param name="Hwnd">The handle to the window that receives the message.</param>
/// <param name="Msg">WindowsMessages enum value</param>
/// <param name="WParam">The additional message-specific information provided as the first parameter.</param>
/// <param name="LParam">The additional message-specific information provided as the second parameter.</param>
public record struct WindowMessage(nint Hwnd, WindowsMessages Msg, nint WParam, nint LParam)
{
    /// <summary>
    /// This has to be set to true to indicate that the message has been handled and should not be processed further by the default window procedure.
    /// </summary>
    public bool Handled { get; set; } = false;

    /// <summary>
    /// This property can be set to a specific value to indicate the result of processing the message.
    /// The meaning of this value depends on the message being processed and is typically used to provide feedback to the system or other applications about how the message was handled.
    /// </summary>
    public nuint Result { get; set; } = 0;
}

