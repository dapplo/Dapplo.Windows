// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.User32;

/// <summary>
///     The special window handle values of the Windows API, e.g. for the hWndInsertAfter of
///     <see cref="User32Api.SetWindowPos"/>, the hWnd of <see cref="User32Api.PostMessage(IntPtr, uint, IntPtr, IntPtr)"/>
///     or the parent of CreateWindowEx and <see cref="User32Api.FindWindowEx"/>.
///     These are fields and not constants, because C# has no IntPtr constants; don't compare handles with them by reference but with ==.
/// </summary>
public static class WindowHandles
{
    /// <summary>
    ///     HWND_TOP (0): SetWindowPos places the window at the top of the Z-order (of its topmost or non-topmost group).
    /// </summary>
    public static readonly IntPtr HWND_TOP = IntPtr.Zero;

    /// <summary>
    ///     HWND_BOTTOM (1): SetWindowPos places the window at the bottom of the Z-order.
    ///     A topmost window loses its topmost status and is placed at the bottom of all other windows.
    /// </summary>
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

    /// <summary>
    ///     HWND_TOPMOST (-1): SetWindowPos places the window above all non-topmost windows, the window stays above them even when it's deactivated ("always on top").
    /// </summary>
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    /// <summary>
    ///     HWND_NOTOPMOST (-2): SetWindowPos places the window above all non-topmost windows, which is behind all topmost windows.
    ///     This removes the topmost status, it has no effect when the window is already non-topmost.
    /// </summary>
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    /// <summary>
    ///     HWND_MESSAGE (-3): the parent for a message-only window (CreateWindowEx, SetParent), or with FindWindowEx to search the message-only windows.
    /// </summary>
    public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

    /// <summary>
    ///     HWND_BROADCAST (0xFFFF): PostMessage, SendMessage and SendMessageTimeout deliver the message to all top-level windows,
    ///     including disabled or invisible unowned windows, overlapped windows and pop-up windows, but not to child windows or message-only windows.
    ///     Only broadcast messages registered with RegisterWindowMessage, or system messages meant for broadcasting.
    /// </summary>
    public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
}
