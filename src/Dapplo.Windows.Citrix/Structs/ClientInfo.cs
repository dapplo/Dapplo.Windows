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
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientInfo
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ClientInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _name;
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string _directory;
        private readonly int _buildNumber;
        private readonly int _productId;
        private readonly int _hardwareId;
        private readonly ClientAddress _address;

        /// <summary>
        ///     Return the client's name
        /// </summary>
        public string Name => _name;

        /// <summary>
        ///     Return the client's directory
        /// </summary>
        public string Directory => _directory;

        /// <summary>
        ///     Return the client's BuildNumber
        /// </summary>
        public int BuildNumber => _buildNumber;

        /// <summary>
        ///     Return the client's product ID
        /// </summary>
        public int ProductId => _productId;

        /// <summary>
        ///     Return the client's hardware ID
        /// </summary>
        public int HardwareId => _hardwareId;

        /// <summary>
        ///     Return the client's address
        /// </summary>
        public ClientAddress Address => _address;
    }
}