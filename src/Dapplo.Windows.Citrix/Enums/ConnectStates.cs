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

namespace Dapplo.Windows.Citrix.Enums
{
    /// <summary>
    ///     The connect states
    /// </summary>
    public enum ConnectStates
    {
        /// <summary>
        ///     WFActive: User logged on to WinStation
        /// </summary>
        Active,

        /// <summary>
        ///     WFConnected: WinStation connected to client
        /// </summary>
        Connected,

        /// <summary>
        ///     WFConnectQuery: In the process of connecting to client
        /// </summary>
        ConnectQuery,

        /// <summary>
        ///     WFShadow: Shadowing another WinStation
        /// </summary>
        Shadow,

        /// <summary>
        ///     WFDisconnected: WinStation logged on without client
        /// </summary>
        Disconnected,

        /// <summary>
        ///     WFIdle: Waiting for client to connect
        /// </summary>
        Idle,

        /// <summary>
        ///     WFListen: WinStation is listening for connection
        /// </summary>
        Listen,

        /// <summary>
        ///     WFReset: WinStation is being reset
        /// </summary>
        Reset,

        /// <summary>
        ///     WFDown: WinStation is down due to error
        /// </summary>
        Down,

        /// <summary>
        ///     WFInit: WinStation in initialization
        /// </summary>
        Init
    }
}