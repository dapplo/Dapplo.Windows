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
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.SessionTime
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SessionTime
    {
        private readonly double _connectTime;
        private readonly double _disconnectTime;
        private readonly double _lastInputTime;
        private readonly double _logonTime;
        private readonly double _currentTime;

        /// <summary>
        ///     Return the username
        /// </summary>
        public double ConnectTime => _connectTime;

        /// <summary>
        ///     Return the last disconnect time
        /// </summary>
        public double DisconnectTime => _disconnectTime;

        /// <summary>
        ///     Return the last input time
        /// </summary>
        public double LastInputTime => _lastInputTime;

        /// <summary>
        ///     Return the logon time
        /// </summary>
        public double LogonTime => _logonTime;

        /// <summary>
        ///     Return the current time
        /// </summary>
        public double CurrentTime => _currentTime;
    }
}