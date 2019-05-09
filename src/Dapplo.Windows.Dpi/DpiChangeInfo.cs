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

namespace Dapplo.Windows.Dpi
{
    /// <summary>
    /// Stores information about a DPI change
    /// </summary>
    public class DpiChangeInfo
    {
        /// <summary>
        /// The DPI from before the change
        /// </summary>
        public uint PreviousDpi { get; }

        /// <summary>
        /// The new DPI
        /// </summary>
        public uint NewDpi { get; }

        /// <summary>
        /// Creates a DpiChangeInfo
        /// </summary>
        /// <param name="previousDpi">uint</param>
        /// <param name="newDpi">uint</param>
        public DpiChangeInfo(uint previousDpi, uint newDpi)
        {
            PreviousDpi = previousDpi;
            NewDpi = newDpi;
        }
    }
}
