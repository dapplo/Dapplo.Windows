// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages.Structs;

/// <summary>
/// Represents a Windows message, including its window handle, message identifier, and associated parameters.
/// Subscribers can mark the message as handled and supply the LRESULT which is returned to Windows.
/// </summary>
/// <remarks>
/// This is a class (reference type), so every subscriber sees the same instance and <see cref="Handled"/> and <see cref="Result"/>
/// set by one subscriber are visible to the window procedure and to later subscribers.
/// <para>
/// <see cref="Handled"/> and <see cref="Result"/> are only honoured when they are set synchronously inside <c>OnNext</c>, on the thread of the window
/// which received the message. As soon as there is an <c>ObserveOn</c>, <c>SubscribeOn</c>, <c>Throttle</c>, <c>Delay</c>, an <c>await</c> or any other
/// thread or time hop, the window procedure has already returned and the reply has already been sent to Windows.
/// </para>
/// The meaning of the parameters depends on the specific message identified by <see cref="Msg"/>.
/// </remarks>
public sealed class WindowMessage
{
    /// <summary>
    /// Create a WindowMessage
    /// </summary>
    /// <param name="hwnd">The handle to the window that receives the message.</param>
    /// <param name="msg">WindowsMessages enum value</param>
    /// <param name="wParam">The additional message-specific information provided as the first parameter.</param>
    /// <param name="lParam">The additional message-specific information provided as the second parameter.</param>
    public WindowMessage(nint hwnd, WindowsMessages msg, nint wParam, nint lParam)
    {
        Hwnd = hwnd;
        Msg = msg;
        WParam = wParam;
        LParam = lParam;
    }

    /// <summary>
    /// The handle to the window that receives the message.
    /// </summary>
    public nint Hwnd { get; }

    /// <summary>
    /// The message identifier.
    /// </summary>
    public WindowsMessages Msg { get; }

    /// <summary>
    /// The additional message-specific information provided as the first parameter.
    /// </summary>
    public nint WParam { get; }

    /// <summary>
    /// The additional message-specific information provided as the second parameter.
    /// </summary>
    public nint LParam { get; }

    /// <summary>
    /// Set this to true to indicate that the message has been handled: the window procedure then returns <see cref="Result"/>
    /// instead of calling the default window procedure.
    /// Only honoured when set synchronously inside OnNext on the window thread.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// The LRESULT which is returned to Windows when <see cref="Handled"/> is true. LRESULT is a signed pointer sized value (LONG_PTR),
    /// its meaning depends on the message being processed.
    /// Only honoured when set synchronously inside OnNext on the window thread.
    /// </summary>
    public nint Result { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"WindowMessage {{ Hwnd = 0x{((long)Hwnd):X}, Msg = {Msg}, WParam = 0x{((long)WParam):X}, LParam = 0x{((long)LParam):X}, Handled = {Handled}, Result = {(long)Result} }}";
    }
}
