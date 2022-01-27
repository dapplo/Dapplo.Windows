// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Citrix.Enums;

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