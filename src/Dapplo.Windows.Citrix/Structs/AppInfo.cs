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

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Citrix.Structs
{
    /// <summary>
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.AppInfo
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct AppInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _initialProgram;
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _workingDirectory;
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _applicationName;

        /// <summary>
        ///     Return the InitialProgram
        /// </summary>
        public string InitialProgram => _initialProgram;

        /// <summary>
        ///     Return the WorkingDirectory
        /// </summary>
        public string WorkingDirectory => _workingDirectory;

        /// <summary>
        ///     Return the application name
        /// </summary>
        public string ApplicationName => _applicationName;
    }
}