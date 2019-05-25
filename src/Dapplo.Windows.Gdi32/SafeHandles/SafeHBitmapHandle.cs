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
using System.Drawing;
using System.Security;

#endregion

namespace Dapplo.Windows.Gdi32.SafeHandles
{
    /// <summary>
    ///     A hbitmap SafeHandle implementation, use this for disposable usage of HBitmap
    /// </summary>
    public class SafeHBitmapHandle : SafeObjectHandle
    {
        /// <summary>
        ///     Default constructor is needed to support marshalling!!
        /// </summary>
        [SecurityCritical]
        public SafeHBitmapHandle() : base(true)
        {
        }

        /// <summary>
        ///     Create a SafeHBitmapHandle from an existing handle
        /// </summary>
        /// <param name="preexistingHandle">IntPtr to HBitmap</param>
        [SecurityCritical]
        public SafeHBitmapHandle(IntPtr preexistingHandle) : base(true)
        {
            SetHandle(preexistingHandle);
        }

        /// <summary>
        ///     Create a SafeHBitmapHandle from a Bitmap
        /// </summary>
        /// <param name="bitmap">Bitmap to call GetHbitmap on</param>
        [SecurityCritical]
        public SafeHBitmapHandle(Bitmap bitmap) : base(true)
        {
            SetHandle(bitmap.GetHbitmap());
        }
    }
}