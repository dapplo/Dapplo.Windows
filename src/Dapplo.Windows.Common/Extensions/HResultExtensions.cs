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
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

#endregion

namespace Dapplo.Windows.Common.Extensions
{
    /// <summary>
    ///     Extensions to handle the HResult
    /// </summary>
    public static class HResultExtensions
    {
        /// <summary>
        ///     Test if the HResult respresents a fail
        /// </summary>
        /// <param name="hResult">HResult</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool Failed(this HResult hResult)
        {
            return hResult < 0;
        }

        /// <summary>
        ///     Test if the HResult respresents a success
        /// </summary>
        /// <param name="hResult">HResult</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool Succeeded(this HResult hResult)
        {
            return hResult >= HResult.S_OK;
		}

        /// <summary>
        ///     Throw an exception on Failure
        /// </summary>
        /// <param name="hResult">HResult</param>
        public static void ThrowOnFailure(this HResult hResult)
        {
            if (Failed(hResult))
            {
                throw Marshal.GetExceptionForHR((int) hResult);
            }
        }
    }
}