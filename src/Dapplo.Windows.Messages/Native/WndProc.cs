// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Messages.Native;

/// <summary>
/// Represents a method that processes messages sent to a window in a Windows application.
/// </summary>
/// <remarks>This delegate is typically used to define a window procedure (WndProc) that handles messages from the
/// operating system or other applications. Implementations should return an appropriate result based on the message
/// processed. For more information about window procedures and message handling, see the Windows API
/// documentation.</remarks>
/// <param name="hWnd">A handle to the window that is receiving the message.</param>
/// <param name="msg">The message identifier that specifies the type of message being sent.</param>
/// <param name="wParam">Additional message-specific information. The meaning depends on the value of the msg parameter.</param>
/// <param name="lParam">Additional message-specific information. The meaning depends on the value of the msg parameter.</param>
/// <returns>A value that indicates the result of the message processing, as defined by the message being handled.</returns>
public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);
