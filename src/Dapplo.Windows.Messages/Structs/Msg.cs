// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Messages.Structs;

/// <summary>
/// This structure represents the message information of Windows
/// See <a href="https://docs.microsoft.com/en-us/windows/desktop/api/winuser/ns-winuser-tagmsg">tagMSG structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public readonly struct Msg
{
    private readonly IntPtr _hWnd;
    private readonly WindowsMessages _message;
    private readonly UIntPtr _wParam;
    private readonly UIntPtr _lParam;
    private readonly uint _time;
    private readonly NativePoint _pt;

    /// <summary>
    /// Identifies the window whose window procedure receives the message.
    /// </summary>
    public IntPtr Handle => _hWnd;

    /// <summary>
    /// The message identifier.
    /// </summary>
    public WindowsMessages Message => _message;

    /// <summary>
    /// Additional information about the message. The exact meaning depends on the value of the message member.
    /// </summary>
    public UIntPtr wParam => _wParam;

    /// <summary>
    /// Additional information about the message. The exact meaning depends on the value of the message member.
    /// </summary>
    public UIntPtr lParam => _lParam;

    /// <summary>
    /// Time of the message
    /// </summary>
    public DateTimeOffset Time => DateTimeOffset.Now.Subtract(TimeSpan.FromMilliseconds(Environment.TickCount - _time));

    /// <summary>
    /// The cursor position, in screen coordinates, when the message was posted.
    /// </summary>
    public NativePoint CursorPosition => _pt;

}