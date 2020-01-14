// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs
{
    /// <summary>
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientLatency
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClientLatency
    {
        private readonly uint _avarage;
        private readonly uint _last;
        private readonly uint _derivation;

        /// <summary>
        ///     Return the client's avarage latency
        /// </summary>
        public uint Avarage => _avarage;
        /// <summary>
        ///     Return the client's last latency
        /// </summary>
        public uint Last => _last;

        /// <summary>
        ///     Return the client's latency derivation
        /// </summary>
        public uint Derivation => _derivation;
    }
}