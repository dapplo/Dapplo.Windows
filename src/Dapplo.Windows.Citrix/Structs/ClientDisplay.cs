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

using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Citrix.Structs
{
    /// <summary>
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientDisplay
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClientDisplay
    {
        private readonly uint _horizontalResolution;
        private readonly uint _verticalResolution;
        private readonly uint _colorDepth;

        /// <summary>
        ///     Return the client's display size
        /// </summary>
        public NativeSize ClientSize => new NativeSize((int)_horizontalResolution, (int)_verticalResolution);

        /// <summary>
        ///     Returns the number of colors the client can display
        /// </summary>
        public uint ColorDepth
        {
            get
            {
                switch (_colorDepth)
                {
                    case 1:
                        return 4;
                    case 2:
                        return 8;
                    case 4:
                        return 16;
                    case 8:
                        return 24;
                    case 16:
                        return 32;
                }
                return _colorDepth;
            }
        }
    }
}