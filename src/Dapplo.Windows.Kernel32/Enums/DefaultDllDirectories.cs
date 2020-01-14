// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

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