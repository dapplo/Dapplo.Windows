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

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    ///     An enum specifying the type of input event used for the SendInput call.
    ///     This specifies which structure type of the union supplied to SendInput is used.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646270(v=vs.85).aspx">INPUT structure</a>
    /// </summary>
    public enum InputTypes : uint
    {
        /// <summary>
        ///     The event is a mouse event.
        /// </summary>
        Mouse = 0,

        /// <summary>
        ///     The event is a keyboard event.
        /// </summary>
        Keyboard = 1,

        /// <summary>
        ///     The event is a hardware event.
        /// </summary>
        Hardware = 2
    }
}