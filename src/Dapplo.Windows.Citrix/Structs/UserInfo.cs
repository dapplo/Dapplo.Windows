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
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.UserInfo
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UserInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _userName;
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _domainName;
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _connectionName;

        /// <summary>
        ///     Return the username
        /// </summary>
        public string Username => _userName;

        /// <summary>
        ///     Return the domain name
        /// </summary>
        public string Domainname => _domainName;

        /// <summary>
        ///     Return the connection name
        /// </summary>
        public string ConnectionName => _connectionName;
    }
}