// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     The transition state of the mouse buttons. This member can be one or more of the following values.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645578.aspx">RAWMOUSE structure</a>
/// </summary>
[Flags]
public enum MouseButtonStates : ushort
{
    /// <summary>
    ///     Left button changed to down.
    /// </summary>
    LeftButtonDown = 0x0001,
    /// <summary>
    ///     Left button changed to Up.
    /// </summary>
    LeftButtonUp = 0x0002,

    /// <summary>
    ///     Right button changed to down.
    /// </summary>
    RightButtonDown = 0x0004,
    /// <summary>
    ///     Right button changed to Up.
    /// </summary>
    RightButtonUp = 0x0008,

    /// <summary>
    ///     Middle button changed to down.
    /// </summary>
    MiddleButtonDown = 0x0010,
    /// <summary>
    ///     Middle button changed to up.
    /// </summary>
    MiddleButtonUp = 0x0020,

    /// <summary>
    /// XBUTTON1 changed to down (RI_MOUSE_BUTTON_4_DOWN).
    /// </summary>
    ButtonX1Down = 0x0040,

    /// <summary>
    /// XBUTTON1 changed to up (RI_MOUSE_BUTTON_4_UP).
    /// </summary>
    ButtonX1Up = 0x0080,

    /// <summary>
    /// XBUTTON2 changed to down (RI_MOUSE_BUTTON_5_DOWN).
    /// </summary>
    ButtonX2Down = 0x0100,

    /// <summary>
    /// XBUTTON2 changed to up (RI_MOUSE_BUTTON_5_UP).
    /// </summary>
    ButtonX2Up = 0x0200,

    /// <summary>
    /// Raw input comes from a mouse wheel.
    /// The wheel delta is stored in usButtonData.
    /// </summary>
    Wheel = 0x0400,

    /// <summary>
    /// Raw input comes from a horizontal mouse wheel (RI_MOUSE_HWHEEL).
    /// The wheel delta is stored in usButtonData.
    /// </summary>
    HorizontalWheel = 0x0800
}