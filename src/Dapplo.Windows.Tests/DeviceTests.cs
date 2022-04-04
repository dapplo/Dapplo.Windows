// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class DeviceTests
{
    private static readonly LogSource Log = new LogSource();

    public DeviceTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [Fact]
    public void TestParse()
    {
        const string iPhoneDeviceName = @"\?\USB#VID_05AC&PID_1294&MI_00#0#{6bdd1fc6-810f-11d0-bec7-08002be2092f}";
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(iPhoneDeviceName, DeviceInterfaceClass.StillImage);
        Assert.Equal("1294", devBroadcastDeviceInterface.ProductId);
        Assert.Equal("05AC", devBroadcastDeviceInterface.VendorId);
        Assert.Equal("6bdd1fc6-810f-11d0-bec7-08002be2092f", devBroadcastDeviceInterface.DeviceClassGuid.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.True(devBroadcastDeviceInterface.IsUsb);
        Log.Info().WriteLine("More information: {0}", devBroadcastDeviceInterface.UsbDeviceInfoUri);
    }

    [Fact]
    public void TestParse_2()
    {
        const string graphicsCard =
            @"\?\PCI#VEN_10DE&DEV_1FB8&SUBSYS_09061028&REV_A1#4&32af3f68&0&0008#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}";
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(graphicsCard, DeviceInterfaceClass.DisplayDeviceArrival);
        Assert.Equal("10DE", devBroadcastDeviceInterface.VendorId);
        Assert.Equal("1ca05180-a699-450a-9a0c-de4fbe3ddd89", devBroadcastDeviceInterface.DeviceClassGuid.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(@"PCI\VEN_10DE&DEV_1FB8&SUBSYS_09061028&REV_A1\4&32af3f68&0&0008", devBroadcastDeviceInterface.DisplayName);
        Assert.True(devBroadcastDeviceInterface.IsPci);
        Log.Info().WriteLine("More information: {0}", devBroadcastDeviceInterface.UsbDeviceInfoUri);
    }
}