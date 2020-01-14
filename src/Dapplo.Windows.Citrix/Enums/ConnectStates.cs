// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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