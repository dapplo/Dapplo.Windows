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

using System;

namespace Dapplo.Windows.Shell32.Enums
{
    /// <summary>
    /// A value that specifies an edge of the screen.
    /// </summary>
    [Flags]
    public enum AppBarStates
    {
        /// <summary>
        /// ABS_MANUAL - No automatic function
        /// </summary>
        None = 0,
        /// <summary>
        /// ABS_AUTOHIDE - Autohides the AppBar
        /// </summary>
        AutoHide = 1,
        /// <summary>
        /// ABS_ALWAYSONTOP - Make sure the AppBar is always on top 
        /// </summary>
        AllwaysOnTop = 2
    }
}
