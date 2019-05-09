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
    ///     Type of information to be retrieved from the specified session via the WFQuerySessionInformation(W) call.
    /// </summary>
    public enum InfoClasses
    {
        /// <summary>
        ///     WFVersion to get OSVERSIONINFO
        /// </summary>
        Version = 0,

        /// <summary>
        ///     WFInitialProgram NULL-terminated string
        /// </summary>
        InitialProgram = 1,

        /// <summary>
        ///     WFWorkingDirectory NULL-terminated string
        /// </summary>
        WorkingDirectory = 2,

        /// <summary>
        ///     WFOEMId NULL-terminated string
        /// </summary>
        OemId = 3,

        /// <summary>
        ///     WFSessionId ULONG = unsigned long
        /// </summary>
        SessionId = 4,

        /// <summary>
        ///     WFUserName NULL-terminated string
        /// </summary>
        UserName = 5,

        /// <summary>
        ///     WFWinStationName NULL-terminated string
        /// </summary>
        WinStationName = 6,

        /// <summary>
        ///     WFDomainName NULL-terminated string
        /// </summary>
        DomainName = 7,

        /// <summary>
        ///     WFConnectState INT
        /// </summary>
        ConnectState = 8,

        /// <summary>
        ///     WFClientBuildNumber USHORT
        /// </summary>
        ClientBuildNumber = 9,

        /// <summary>
        ///     WFClientName NULL-terminated string
        /// </summary>
        ClientName = 10,

        /// <summary>
        ///     WFClientDirectory NULL-terminated string
        /// </summary>
        ClientDirectory = 11,

        /// <summary>
        ///     WFClientProductId to get USHORT
        /// </summary>
        ClientProductId = 12,

        /// <summary>
        ///     WFClientHardwareId
        /// </summary>
        ClientHardwareId = 13,

        /// <summary>
        ///     WFClientAddress to get WF_CLIENT_ADDRESS
        /// </summary>
        ClientAddress = 14,

        /// <summary>
        ///     WFClientDisplay to get WF_CLIENT_DISPLAY
        /// </summary>
        ClientDisplay = 15,

        /// <summary>
        ///     WFClientCache to get WF_CLIENT_CACHE
        /// </summary>
        ClientCache = 16,

        /// <summary>
        ///     WFClientDrives to get WF_CLIENT_DRIVES
        /// </summary>
        ClientDrives = 17,

        /// <summary>
        ///     WFICABufferLength to get ULONG
        /// </summary>
        IcaBufferLength = 18,

        /// <summary>
        ///     WFLicenseEnabler
        /// </summary>
        LicenseEnabler = 19,

        /// <summary>
        ///     Reserved
        /// </summary>
        Reserved = 20,

        /// <summary>
        ///     WFApplicationName NULL-terminated string
        /// </summary>
        ApplicationName = 21,

        /// <summary>
        ///     WFVersionEx
        /// </summary>
        VersionEx = 22,

        /// <summary>
        ///     WFClientInfo to get WF_CLIENT_INFO
        /// </summary>
        ClientInfo = 23,

        /// <summary>
        ///     WFUserInfo to get WF_USER_INFO
        /// </summary>
        UserInfo = 24,

        /// <summary>
        ///     WFAppInfo to get WF_APP_INFO
        /// </summary>
        AppInfo = 25,

        /// <summary>
        ///     WFClientLatency to get WF_CLIENT_LATENCY
        /// </summary>
        ClientLatency = 26,

        /// <summary>
        ///     WFSessionTime to get WF_SESSION_TIME
        /// </summary>
        SessionTime = 27,

        /// <summary>
        /// WFLicensingModel
        /// </summary>
        LicensingModel = 28
    }
}