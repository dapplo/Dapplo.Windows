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
    ///     See See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144950(v=vs.85).aspx">GetWindowRgn function</a>
    /// </summary>
    public enum RegionResults
    {
        /// <summary>
        ///     The specified window does not have a region, or an error occurred while attempting to return the region.
        /// </summary>
        Error = 0,

        /// <summary>
        ///     The region is empty.
        /// </summary>
        NullRegion = 1,

        /// <summary>
        ///     The region is a single rectangle.
        /// </summary>
        SimpleRegion = 2,

        /// <summary>
        ///     The region is more than one rectangle.
        /// </summary>
        ComplexRegion = 3
    }
}