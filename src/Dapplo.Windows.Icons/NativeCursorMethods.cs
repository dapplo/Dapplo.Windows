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

using System.Runtime.InteropServices;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Win32 native methods for Icon
    /// </summary>
    public static class NativeCursorMethods
    {
        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648389(v=vs.85).aspx">GetCursorInfo function</a>
        ///     Retrieves information about the global cursor.
        /// </summary>
        /// <param name="cursorInfo">CursorInfo structure to fill</param>
        /// <returns>bool</returns>
        [DllImport(User32Api.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorInfo(ref CursorInfo cursorInfo);
    }
}
