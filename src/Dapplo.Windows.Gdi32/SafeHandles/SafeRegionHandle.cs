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
using System.Security;

#endregion

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A hRegion SafeHandle implementation
    /// </summary>
    public class SafeRegionHandle : SafeObjectHandle
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeRegionHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeRegionHandle from an existing handle
        /// </summary>
        /// <param name="preexistingHandle">IntPtr to region</param>
        [SecurityCritical]
        public SafeRegionHandle(IntPtr preexistingHandle) : base(true)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        ///     Directly call Gdi32.CreateRectRgn
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns>SafeRegionHandle</returns>
        public static SafeRegionHandle CreateRectRgn(int left, int top, int right, int bottom)
        {
            return Gdi32Api.CreateRectRgn(left, top, right, bottom);
        }
    }
}