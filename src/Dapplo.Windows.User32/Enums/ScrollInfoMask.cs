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
    ///     The ScrollInfoMask enum is used for retrieving the SCROLLINFO via the GetScrollInfo
    ///     See <a href="http://pinvoke.net/default.aspx/Enums/ScrollInfoMask.html">here</a>
    /// </summary>
    [Flags]
    public enum ScrollInfoMask
    {
        /// <summary>
        ///     Copies the scroll range to the Minimum and Maximum members of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Range = 0x1,

        /// <summary>
        ///     Copies the scroll page to the PageSize member of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Page = 0x2,

        /// <summary>
        ///     Copies the scroll position to the Position member of the SCROLLINFO structure pointed to by lpsi.
        /// </summary>
        Pos = 0x4,

        /// <summary>
        ///     Unknown
        /// </summary>
        DisableNoScroll = 0x8,

        /// <summary>
        ///     Copies the current scroll box tracking position to the TrackingPosition member of the SCROLLINFO structure pointed
        ///     to by
        ///     lpsi.
        /// </summary>
        Trackpos = 0x10,

        /// <summary>
        ///     All of the above
        /// </summary>
        All = Range | Page | Pos | Trackpos
    }
}