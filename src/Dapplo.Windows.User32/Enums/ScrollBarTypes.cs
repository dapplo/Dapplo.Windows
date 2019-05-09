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

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See <a href="http://pinvoke.net/default.aspx/Enums/SBOrientation.html">here</a>
    /// </summary>
    public enum ScrollBarTypes
    {
        /// <summary>
        ///     The horizontal scroll bar of the specified window
        /// </summary>
        Horizontal = 0,

        /// <summary>
        ///     The vertical scroll bar of the specified window
        /// </summary>
        Vertical = 1,

        /// <summary>
        ///     A scroll bar control
        /// </summary>
        Control = 2,

        /// <summary>
        ///     The horizontal and vertical scroll bars of the specified window
        /// </summary>
        Both = 3
    }
}