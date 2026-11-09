// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Xunit;
using Dapplo.Windows.Citrix;
using Dapplo.Windows.Citrix.Enums;
using Dapplo.Windows.Citrix.Structs;

namespace Dapplo.Windows.Tests;

public class CitrixTests
{
    public CitrixTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Assume that we are not running on Citrix
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void TestCitrix_NotAvailable()
    {
        Assert.False(WinFrame.IsAvailable, "We are running on Citrix????");
            
    }

    [Fact]
    public void TestCitrix_DllNameMatchesTheBitness()
    {
        Assert.Equal(Environment.Is64BitProcess ? "WFAPI64" : "WFAPI", WinFrame.DllName);
    }

    /// <summary>
    ///     Not on Citrix: the protocol is unknown, no ICA session, nothing can be queried
    /// </summary>
    [Fact]
    public void TestCitrix_NotOnCitrix_NothingIsAvailable()
    {
        Assert.Null(WinFrame.GetActiveProtocol());
        Assert.False(WinFrame.IsIcaSession);
        Assert.Null(WinFrame.QuerySessionConnectState());
    }

    [Fact]
    public void TestCitrix_ClientAddress_IPv4AndIPv6()
    {
        var ipv4 = new byte[24];
        BitConverter.GetBytes((int)AddressFamily.InterNetwork).CopyTo(ipv4, 0);
        new byte[] { 192, 168, 1, 42 }.CopyTo(ipv4, 4 + 2);
        var clientAddress = FromBytes<ClientAddress>(ipv4);
        Assert.Equal(AddressFamily.InterNetwork, clientAddress.AddressFamily);
        Assert.Equal("192.168.1.42", clientAddress.IpAddress);

        var ipv6 = new byte[24];
        BitConverter.GetBytes((int)AddressFamily.InterNetworkV6).CopyTo(ipv6, 0);
        new byte[] { 0xfe, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }.CopyTo(ipv6, 4 + 2);
        Assert.Equal("fe80::1", FromBytes<ClientAddress>(ipv6).IpAddress);

        var unknown = new byte[24];
        BitConverter.GetBytes((int)AddressFamily.Unspecified).CopyTo(unknown, 0);
        Assert.Null(FromBytes<ClientAddress>(unknown).IpAddress);
        Assert.Null(FromBytes<ClientAddress>(unknown).Address);
    }

    private static T FromBytes<T>(byte[] bytes) where T : struct
    {
        Assert.Equal(Marshal.SizeOf<T>(), bytes.Length);
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }
}
