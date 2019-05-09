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

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     Flags for the MonitorFromRect / MonitorFromWindow "flags" field
    ///     see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
    ///     or see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145064(v=vs.85).aspx">MonitorFromWindow function</a>
    /// </summary>
    [Flags]
    public enum MonitorFrom : uint
    {
        /// <summary>
        ///     Returns a handle to the display monitor that is nearest to the rectangle.
        /// </summary>
        DefaultToNearest = 0,

        /// <summary>
        ///     Returns NULL. (why??)
        /// </summary>
        DefaultToNull = 1,

        /// <summary>
        ///     Returns a handle to the primary display monitor.
        /// </summary>
        DefaultToPrimary = 2
    }
}