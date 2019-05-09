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
}