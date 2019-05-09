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

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     Get/Set WindowLong Enum See: http://msdn.microsoft.com/en-us/library/ms633591.aspx
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowLongIndex
    {
        /// <summary>
        ///     Sets a new extended window style.
        /// </summary>
        GWL_EXSTYLE = -20,

        /// <summary>
        ///     Sets a new application instance handle.
        /// </summary>
        GWL_HINSTANCE = -6,

        /// <summary>
        ///     Sets a new identifier of the child window. The window cannot be a top-level window.
        /// </summary>
        GWL_ID = -12,

        /// <summary>
        ///     Sets a new window style.
        /// </summary>
        GWL_STYLE = -16,

        /// <summary>
        ///     Sets the user data associated with the window.
        ///     This data is intended for use by the application that created the window.
        ///     Its value is initially zero.
        /// </summary>
        GWL_USERDATA = -21,

        /// <summary>
        ///     Sets a new address for the window procedure.
        ///     You cannot change this attribute if the window does not belong to the same process as the calling thread.
        /// </summary>
        GWL_WNDPROC = -4
    }
}