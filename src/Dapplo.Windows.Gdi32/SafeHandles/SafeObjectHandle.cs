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

using Microsoft.Win32.SafeHandles;

#endregion

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     Abstract class SafeObjectHandle which contains all handles that are cleaned with DeleteObject
    /// </summary>
    public abstract class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        ///     Create SafeObjectHandle
        /// </summary>
        /// <param name="ownsHandle">True if the class owns the handle</param>
        protected SafeObjectHandle(bool ownsHandle) : base(ownsHandle)
        {
        }

        /// <summary>
        ///     Call DeleteObject
        /// </summary>
        /// <returns>true if this worked</returns>
        protected override bool ReleaseHandle()
        {
            return Gdi32Api.DeleteObject(handle);
        }
    }
}