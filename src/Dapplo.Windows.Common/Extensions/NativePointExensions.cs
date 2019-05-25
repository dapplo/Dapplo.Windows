#region Copyright (C) 2016-2019 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2019 Dapplo
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
#endregion

#region using

using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativePoint struct
    /// </summary>
    public static class NativePointExensions
    {
        /// <summary>
        /// Create a new NativePoint, from the supplied one, using the specified X coordinate
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <param name="x">int</param>
        /// <returns>NativePoint</returns>
        [Pure]
        public static NativePoint ChangeX(this NativePoint point, int x)
        {
            return new NativePoint(x, point.Y);
        }

        /// <summary>
        /// Create a new NativePoint, from the supplied one, using the specified Y coordinate
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <param name="y">int</param>
        /// <returns>NativePoint</returns>
        [Pure]
        public static NativePoint ChangeY(this NativePoint point, int y)
        {
            return new NativePoint(point.X, y);
        }

        /// <summary>
        /// Create a new NativePoint, from the supplied one, using the specified offset
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <param name="offset">NativePoint</param>
        /// <returns>NativePoint</returns>
        [Pure]
        public static NativePoint Offset(this NativePoint point, NativePoint offset)
        {
            return new NativePoint(point.X + offset.X, point.Y + offset.Y);
        }

        /// <summary>
        /// Create a new NativePoint, from the supplied one, using the specified offset
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <param name="offsetX">int</param>
        /// <param name="offsetY">int</param>
        /// <returns>NativePoint</returns>
        [Pure]
        public static NativePoint Offset(this NativePoint point, int? offsetX = null, int? offsetY = null)
        {
            return new NativePoint(point.X + offsetX ?? 0, point.Y + offsetY ?? 0);
        }
    }
}