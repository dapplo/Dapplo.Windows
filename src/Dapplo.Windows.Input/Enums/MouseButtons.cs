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

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    /// Defines which mouse buttons to use
    /// </summary>
    [Flags]
    public enum MouseButtons
    {
        /// <summary>
        /// No button
        /// </summary>
        None,
        /// <summary>
        /// Left mouse button
        /// </summary>
        Left = 0x100000,
        /// <summary>
        /// Right mouse button
        /// </summary>
        Right = 0x200000,
        /// <summary>
        /// Middle mouse button
        /// </summary>
        Middle = 0x400000,
        /// <summary>
        /// Extra button 1
        /// </summary>
        XButton1 = 0x800000,
        /// <summary>
        /// Extra button 2
        /// </summary>
        XButton2 = 0x1000000,
    }
}
