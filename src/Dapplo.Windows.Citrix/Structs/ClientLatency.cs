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