// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     This enum specifies various aspects of a keystroke. This member can be certain combinations of the following
///     values.
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646271(v=vs.85).aspx">KEYBDINPUT structure</a>
/// </summary>
[Flags]
public enum KeyEventFlags : uint
{
    /// <summary>
    ///     If specified, the scan code was preceded by a prefix byte that has the value 0xE0 (224).
    /// </summary>
    ExtendedKey = 0x0001,

    /// <summary>
    ///     If specified, the key is being released. If not specified, the key is being pressed.
    /// </summary>
    KeyUp = 0x0002,

    /// <summary>
    ///     If specified, the system synthesizes a VK_PACKET keystroke. The VirtualKeyCode parameter must be zero.
    ///     This flag can only be combined with the KeyUp flag. For more information, see the Remarks section.
    /// </summary>
    Unicode = 0x0004,

    /// <summary>
    ///     If specified, wScan identifies the key and VirtualKeyCode is ignored.
    /// </summary>
    Scancode = 0x0008
}