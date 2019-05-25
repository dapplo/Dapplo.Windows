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

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com
{
    /// <summary>
    ///     Implementation of the IDisposableCom, this is internal to prevent other code to use it directly
    /// </summary>
    /// <typeparam name="T">Type of the com object</typeparam>
    internal class DisposableComImplementation<T> : IDisposableCom<T>
    {
        public DisposableComImplementation(T obj)
        {
            ComObject = obj;
        }

        public T ComObject { get; private set; }

        /// <summary>
        ///     Cleans up the COM object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Release the COM reference
        /// </summary>
        /// <param name="disposing"><see langword="true" /> if this was called from the<see cref="IDisposable" /> interface.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            // Do not catch an exception from this.
            // You may want to remove these guards depending on
            // what you think the semantics should be.
            if (!Equals(ComObject, default(T)) && Marshal.IsComObject(ComObject))
            {
                Marshal.ReleaseComObject(ComObject);
            }
            ComObject = default;
        }
    }
}