// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Messages.Structs;

/// <summary>
/// Represents a Windows message, including its window handle, message identifier, and associated parameters.
/// </summary>
/// <remarks>This struct is commonly used when processing Windows messages in low-level window procedures or
/// interop scenarios. The meaning of the parameters depends on the specific message identified by Msg.</remarks>
/// <param name="Hwnd">The handle to the window that receives the message.</param>
/// <param name="Msg">The identifier of the Windows message.</param>
/// <param name="WParam">The additional message-specific information provided as the first parameter.</param>
/// <param name="LParam">The additional message-specific information provided as the second parameter.</param>
public readonly record struct WindowMessage(nint Hwnd, uint Msg, nint WParam, nint LParam);

