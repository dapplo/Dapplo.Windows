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
    /// XBUTTON1 changed to down.
    /// </summary>
    ButtonX1Down = 0x0040,
    /// <summary>
    /// XBUTTON1 changed to up.
    /// </summary>
    Buttonx1Up = 0x0080,
    /// <summary>
    /// XBUTTON2 changed to down.
    /// </summary>
    ButtonX2Down = 0x0100,
    /// <summary>
    /// XBUTTON2 changed to up.
    /// </summary>
    Buttonx2Up = 0x0200,

    /// <summary>
    /// Copy of LeftButtonDown
    /// </summary>
    Button1Down = LeftButtonDown,
    /// <summary>
    /// Copy of LeftButtonUp
    /// </summary>
    Button1Up = LeftButtonUp,
    /// <summary>
    /// Copy of RightButtonDown
    /// </summary>
    Button2Down = RightButtonDown,
    /// <summary>
    /// Copy of RightButtonUp
    /// </summary>
    Button2Up = RightButtonUp,
    /// <summary>
    /// Copy of MiddleButtonDown
    /// </summary>
    Button3Down = MiddleButtonDown,
    /// <summary>
    /// Copy of MiddleButtonUp
    /// </summary>
    Button3Up = MiddleButtonUp,

    /// <summary>
    /// Copy of Buttonx1Down
    /// </summary>
    Button4Down = 0x0040,
    /// <summary>
    /// Copy of Buttonx1Up
    /// </summary>
    Button4Up = 0x0080,
    /// <summary>
    /// Copy of Buttonx2Down
    /// </summary>
    Button5Down = 0x0100,
    /// <summary>
    /// Copy of Buttonx2Up
    /// </summary>
    Button6Up = 0x0200,

    /// <summary>
    /// Raw input comes from a mouse wheel.
    /// The wheel delta is stored in usButtonData.
    /// </summary>
    Wheel = 0x0400
}