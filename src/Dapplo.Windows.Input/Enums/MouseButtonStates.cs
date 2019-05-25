//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;

#endregion

namespace Dapplo.Windows.Input.Enums
{
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
}