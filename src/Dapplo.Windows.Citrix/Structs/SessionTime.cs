// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

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