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

using System.Diagnostics.Contracts;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Helper method for the NativeSizeExensions struct
    /// </summary>
    public static class NativeSizeExensions
    {
        /// <summary>
        /// Create a new NativeSize, from the supplied one, using the specified width
        /// </summary>
        /// <param name="size">NativeSize</param>
        /// <param name="width">int</param>
        /// <returns>NativeSize</returns>
        [Pure]
        public static NativeSize ChangeWidth(this NativeSize size, int width)
        {
            return new NativeSize(width, size.Height);
        }

        /// <summary>
        /// Create a new NativeSize, from the supplied one, using the specified height
        /// </summary>
        /// <param name="size">NativeSize</param>
        /// <param name="height">int</param>
        /// <returns>NativeSize</returns>
        [Pure]
        public static NativeSize ChangeHeight(this NativeSize size, int height)
        {
            return new NativeSize(size.Width, height);
        }
    }
}