// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Citrix.Enums;

/// <summary>
///     The protocol of a session, as returned by WFGetActiveProtocol (and WTSClientProtocolType)
/// </summary>
public enum SessionProtocols
{
    /// <summary>
    ///     The console of the machine, not remoted
    /// </summary>
    Console = 0,

    /// <summary>
    ///     A Citrix ICA (HDX) session
    /// </summary>
    Ica = 1,

    /// <summary>
    ///     A Microsoft Remote Desktop (RDP) session
    /// </summary>
    Rdp = 2
}
