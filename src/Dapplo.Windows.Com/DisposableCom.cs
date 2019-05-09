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

#endregion

namespace Dapplo.Windows.Com
{
    /// <summary>
    ///     A factory for IDisposableCom
    /// </summary>
    public static class DisposableCom
    {
        /// <summary>
        ///     Create a ComDisposable for the supplied type object
        /// </summary>
        /// <typeparam name="T">Type for the com object</typeparam>
        /// <param name="comObject">the com object itself</param>
        /// <returns>IDisposableCom of type T</returns>
        public static IDisposableCom<T> Create<T>(T comObject)
        {
            if (Equals(comObject, default(T)))
            {
                return null;
            }

            return new DisposableComImplementation<T>(comObject);
        }
    }
}