//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
    ///     Type of information to be retrieved from the specified session via the WFQuerySessionInformation call.
    /// </summary>
    public enum InfoClasses
    {
        /// <summary>
        ///     WFVersion returns OSVERSIONINFO
        /// </summary>
        Version,

        /// <summary>
        ///     WFInitialProgram NULL-terminated string
        /// </summary>
        InitialProgram,

        /// <summary>
        ///     WFWorkingDirectory NULL-terminated string
        /// </summary>
        WorkingDirectory,

        /// <summary>
        ///     WFOEMId NULL-terminated string
        /// </summary>
        OemId,

        /// <summary>
        ///     WFSessionId ULONG = unsigned long
        /// </summary>
        SessionId,

        /// <summary>
        ///     WFUserName NULL-terminated string
        /// </summary>
        UserName,

        /// <summary>
        ///     WFWinStationName NULL-terminated string
        /// </summary>
        WinStationName,

        /// <summary>
        ///     WFDomainName NULL-terminated string
        /// </summary>
        DomainName,

        /// <summary>
        ///     WFConnectState INT
        /// </summary>
        ConnectState,

        /// <summary>
        ///     WFClientBuildNumber USHORT
        /// </summary>
        ClientBuildNumber,

        /// <summary>
        ///     WFClientName NULL-terminated string
        /// </summary>
        ClientName,

        /// <summary>
        ///     WFClientDirectory NULL-terminated string
        /// </summary>
        ClientDirectory,

        /// <summary>
        ///     WFClientProductId USHORT
        /// </summary>
        ClientProductId,

        /// <summary>
        ///     WFClientHardwareId
        /// </summary>
        ClientHardwareId,

        /// <summary>
        ///     WFClientAddress WF_CLIENT_ADDRESS
        /// </summary>
        ClientAddress,

        /// <summary>
        ///     WFClientDisplay WF_CLIENT_DISPLAY
        /// </summary>
        ClientDisplay,

        /// <summary>
        ///     WFClientCache WF_CLIENT_CACHE
        /// </summary>
        ClientCache,

        /// <summary>
        ///     WFClientDrives WF_CLIENT_DRIVES
        /// </summary>
        ClientDrives,

        /// <summary>
        ///     WFICABufferLength ULONG
        /// </summary>
        IcaBufferLength,

        /// <summary>
        ///     WFLicenseEnabler
        /// </summary>
        LicenseEnabler,

        /// <summary>
        ///     Reserved
        /// </summary>
        Reserved,

        /// <summary>
        ///     WFApplicationName NULL-terminated string
        /// </summary>
        ApplicationName,

        /// <summary>
        ///     WFVersionEx
        /// </summary>
        VersionEx,

        /// <summary>
        ///     WFClientInfo WF_CLIENT_INFO
        /// </summary>
        ClientInfo,

        /// <summary>
        ///     WFUserInfo WF_USER_INFO
        /// </summary>
        UserInfo,

        /// <summary>
        ///     WFAppInfo WF_APP_INFO
        /// </summary>
        AppInfo,

        /// <summary>
        ///     WFClientLatency WF_CLIENT_LATENCY
        /// </summary>
        ClientLatency,

        /// <summary>
        ///     WFSessionTime WF_SESSION_TIME
        /// </summary>
        SessionTime
    }
}