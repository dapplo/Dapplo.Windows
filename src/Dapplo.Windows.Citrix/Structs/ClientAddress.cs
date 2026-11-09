// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientAddress
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ClientAddress
{
    private readonly int _addressFamily;

    private fixed byte _address[20];

    /// <summary>
    ///     Address Family
    /// </summary>
    public AddressFamily AddressFamily => (AddressFamily)_addressFamily;

    /// <summary>
    ///     The IP address of the client, null when the address family is neither IPv4 (AF_INET) nor IPv6 (AF_INET6)
    /// </summary>
    /// <remarks>
    ///     Like WTS_CLIENT_ADDRESS the address bytes start at offset 2 of the address field: 4 bytes for IPv4, 16 bytes for IPv6.
    /// </remarks>
    public IPAddress Address
    {
        get
        {
            var length = AddressFamily switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => 0
            };
            if (length == 0)
            {
                return null;
            }
            var bytes = new byte[length];
            fixed (byte* address = _address)
            {
                for (var i = 0; i < length; i++)
                {
                    bytes[i] = address[2 + i];
                }
            }
            return new IPAddress(bytes);
        }
    }

    /// <summary>
    ///     The IP address of the client as string, null when the address family is neither IPv4 nor IPv6, see <see cref="Address"/>
    /// </summary>
    public string IpAddress => Address?.ToString();
}
