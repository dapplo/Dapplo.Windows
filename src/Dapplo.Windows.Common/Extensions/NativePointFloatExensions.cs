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

using Dapplo.Windows.Common.Structs;
using System;
using System.Diagnostics.Contracts;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativePointFloat struct
    /// </summary>
    public static class NativePointFloatExensions
    {
        /// <summary>
        /// Create a new NativePointFloat, from the supplied one, using the specified X coordinate
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        /// <param name="x">float</param>
        /// <returns>NativePointFloat</returns>
        [Pure]
        public static NativePointFloat ChangeX(this NativePointFloat point, float x)
        {
            return new NativePointFloat(x, point.Y);
        }

        /// <summary>
        /// Create a new NativePointFloat, from the supplied one, using the specified Y coordinate
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        /// <param name="y">float</param>
        /// <returns>NativePointFloat</returns>
        [Pure]
        public static NativePointFloat ChangeY(this NativePointFloat point, float y)
        {
            return new NativePointFloat(point.X, y);
        }

        /// <summary>
        /// Create a new NativePointFloat, from the supplied one, using the specified offset
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        /// <param name="offsetX">float</param>
        /// <param name="offsetY">float</param>
        /// <returns>NativePointFloat</returns>
        [Pure]
        public static NativePointFloat Offset(this NativePointFloat point, float? offsetX = null, float? offsetY = null)
        {
            return new NativePointFloat(point.X + offsetX ?? 0, point.Y + offsetY ?? 0);
        }

        /// <summary>
        /// Create a new NativePointFloat, from the supplied one, using the specified offset
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        /// <param name="offset">NativePointFloat</param>
        /// <returns>NativePointFloat</returns>
        [Pure]
        public static NativePointFloat Offset(this NativePointFloat point, NativePointFloat offset)
        {
            return new NativePointFloat(point.X + offset.X, point.Y + offset.Y);
        }

        /// <summary>
        /// Create a NativePoint, using rounded values, from the specified NativePointFloat
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        /// <returns>NativePoint</returns>
        [Pure]
        public static NativePoint Round(this NativePointFloat point)
        {
            return new NativePoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }
    }
}