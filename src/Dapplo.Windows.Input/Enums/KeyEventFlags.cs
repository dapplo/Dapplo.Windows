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
}