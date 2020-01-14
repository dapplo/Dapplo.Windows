// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

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