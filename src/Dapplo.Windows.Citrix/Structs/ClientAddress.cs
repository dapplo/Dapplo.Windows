// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientAddress
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ClientAddress
{
    private readonly int _adressFamily;

    private fixed byte _address[20];

    /// <summary>
    ///     Address Family
    /// </summary>
    public AddressFamily AddressFamily => (AddressFamily)_adressFamily;

    /// <summary>
    ///     IP Address used
    /// </summary>
    public string IpAddress
    {
        get
        {
            fixed (byte* address = _address)
            {
                return $"{address[2]}.{address[3]}.{address[4]}.{address[5]}";
            }
        }
    }
}