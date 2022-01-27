// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.UserInfo
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct UserInfo
{
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _userName;
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _domainName;
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _connectionName;

    /// <summary>
    ///     Return the username
    /// </summary>
    public string Username => _userName;

    /// <summary>
    ///     Return the domain name
    /// </summary>
    public string Domainname => _domainName;

    /// <summary>
    ///     Return the connection name
    /// </summary>
    public string ConnectionName => _connectionName;
}