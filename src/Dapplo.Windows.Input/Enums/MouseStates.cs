// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     The mouse state. This member can be any reasonable combination of the following.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645578.aspx">RAWMOUSE structure</a>
/// </summary>
[Flags]
public enum MouseStates : ushort
{
    /// <summary>
    ///     Mouse movement data is relative to the last mouse position.
    /// </summary>
    MoveRelative = 0x0000,

    /// <summary>
    ///     Mouse movement data is based on absolute position.
    /// </summary>
    MoveAbsolute = 0x0001,

    /// <summary>
    ///     Mouse coordinates are mapped to the virtual desktop (for a multiple monitor system).
    /// </summary>
    VirtualDesktop = 0x0002,

    /// <summary>
    ///     The left button was released.
    /// </summary>
    AttributesChanged = 0x0004
}