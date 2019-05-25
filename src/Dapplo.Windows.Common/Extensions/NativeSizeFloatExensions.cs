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
using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativeSizeFloatExensions struct
    /// </summary>
    public static class NativeSizeFloatExensions
    {
        /// <summary>
        /// Create a new NativeSizeFloat, from the supplied one, using the specified width
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <param name="width">float</param>
        /// <returns>NativeSizeFloat</returns>
        [Pure]
        public static NativeSizeFloat ChangeWidth(this NativeSizeFloat size, float width)
        {
            return new NativeSizeFloat(width, size.Height);
        }

        /// <summary>
        /// Create a new NativeSizeFloat, from the supplied one, using the specified height
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <param name="height">float</param>
        /// <returns>NativeSizeFloat</returns>
        [Pure]
        public static NativeSizeFloat ChangeHeight(this NativeSizeFloat size, float height)
        {
            return new NativeSizeFloat(size.Width, height);
        }

        /// <summary>
        /// Create a NativeSize, using rounded values, from the specified NativeSizeFloat
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <returns>NativeSize</returns>
        [Pure]
        public static NativeSize Round(this NativeSizeFloat size)
        {
            return new NativeSize((int)Math.Round(size.Width), (int)Math.Round(size.Height));
        }
    }
}