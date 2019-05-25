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

#endregion

namespace Dapplo.Windows.Kernel32.Enums
{
    /// <summary>
    ///     The directories to search. This parameter can be any combination of the following values.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh310515(v=vs.85).aspx">SetDefaultDllDirectories function</a>
    /// </summary>
    [Flags]
    public enum DefaultDllDirectories
    {
        /// <summary>
        /// If this value is used, the application's installation directory is searched.
        /// </summary>
        SearchApplicationDirectory = 0x200,
        /// <summary>
        /// If this value is used, any path explicitly added using the AddDllDirectory or SetDllDirectory function is searched. If more than one directory has been added, the order in which those directories are searched is unspecified.
        /// </summary>
        SearchUserDirectories = 0x400,
        /// <summary>
        /// If this value is used, %windows%\system32 is searched.
        /// </summary>
        SearchSystem32Directory = 0x800,
        /// <summary>
        /// This value is a combination of LOAD_LIBRARY_SEARCH_APPLICATION_DIR, LOAD_LIBRARY_SEARCH_SYSTEM32, and LOAD_LIBRARY_SEARCH_USER_DIRS.
        /// This value represents the recommended maximum number of directories an application should include in its DLL search path.
        /// </summary>
        SearchDefaultDirectories = 0x1000
    }
}