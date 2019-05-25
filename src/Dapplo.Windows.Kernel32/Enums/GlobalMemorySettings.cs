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

namespace Dapplo.Windows.Kernel32.Enums
{
    /// <summary>
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa366574(v=vs.85).aspx">GlobalAlloc function</a>
    /// </summary>
    [Flags]
    public enum GlobalMemorySettings : uint
    {
        /// <summary>
        /// Allocates fixed memory. The return value is a pointer.
        /// </summary>
        Fixed = 0,
        /// <summary>
        /// Allocates movable memory. Memory blocks are never moved in physical memory, but they can be moved within the default heap.
        /// The return value is a handle to the memory object. To translate the handle into a pointer, use the GlobalLock function.
        /// This value cannot be combined with Fixed.
        /// </summary>
        Movable = 0x02,
        /// <summary>
        /// Initializes memory contents to zero.
        /// </summary>
        ZeroInit = 0x40
    }
}
