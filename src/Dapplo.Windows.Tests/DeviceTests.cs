// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Devices;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;
using Xunit;

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
        const string iPhoneDeviceName = @"\\?\USB#VID_05AC&PID_1294&MI_00#0#{6bdd1fc6-810f-11d0-bec7-08002be2092f}";
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(iPhoneDeviceName, DeviceInterfaceClass.StillImage);
        Assert.Equal("1294", devBroadcastDeviceInterface.ProductId);
        Assert.Equal("05AC", devBroadcastDeviceInterface.VendorId);
        Assert.Equal("6bdd1fc6-810f-11d0-bec7-08002be2092f", devBroadcastDeviceInterface.DeviceClassGuid.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.True(devBroadcastDeviceInterface.IsUsb);
        Assert.Equal("USB", devBroadcastDeviceInterface.DeviceType);
        Assert.Equal(@"USB\VID_05AC&PID_1294&MI_00\0", devBroadcastDeviceInterface.DisplayName);
        Log.Info().WriteLine("More information: {0}", devBroadcastDeviceInterface.UsbDeviceInfoUri);
    }

    [Fact]
    public void TestDisplayName_WithoutInterfaceGuid()
    {
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(@"\\?\USB#VID_05AC&PID_1294&MI_00#0");
        Assert.Equal(@"USB\VID_05AC&PID_1294&MI_00\0", devBroadcastDeviceInterface.DisplayName);
        Assert.Equal("1294", devBroadcastDeviceInterface.ProductId);
    }

    [Fact]
    public void TestDisplayName_Empty()
    {
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(null);
        Assert.Null(devBroadcastDeviceInterface.DisplayName);
        Assert.Null(devBroadcastDeviceInterface.VendorId);
        Assert.Null(devBroadcastDeviceInterface.DeviceType);
    }

    [Fact]
    public void TestDeviceInterfaceClass_GuidsAreUnique()
    {
        var duplicates = Enum.GetValues(typeof(DeviceInterfaceClass)).Cast<DeviceInterfaceClass>()
            .Select(deviceClass => deviceClass.GetAttributeOfType<DescriptionAttribute>()?.Description)
            .Where(description => !string.IsNullOrEmpty(description))
            .GroupBy(description => Guid.Parse(description))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        Assert.Empty(duplicates);
        Assert.Equal(DeviceInterfaceClass.Keyboard, DevBroadcastDeviceInterface.Test("x", DeviceInterfaceClass.Keyboard).DeviceClass);
        Assert.Equal(DeviceInterfaceClass.BluetoothLeDevice, DevBroadcastDeviceInterface.Test("x", DeviceInterfaceClass.BluetoothLeDevice).DeviceClass);
    }

    [Fact]
    public void TestDeviceNotificationEvent_CopiesLongNameFromNativeMemory()
    {
        // A real device path, longer than the 255 characters of the managed struct
        var deviceName = @"\\?\USB#VID_05AC&PID_1294&MI_00#" + new string('A', 300) + "#{6bdd1fc6-810f-11d0-bec7-08002be2092f}";
        var classGuid = new Guid("6bdd1fc6-810f-11d0-bec7-08002be2092f");
        // DEV_BROADCAST_DEVICEINTERFACE_W: DWORD size, DWORD type, DWORD reserved, GUID class, WCHAR name[] (zero terminated)
        const int nameOffset = 28;
        var size = nameOffset + (deviceName.Length + 1) * 2;
        var ptr = Marshal.AllocHGlobal(size);
        DeviceNotificationEvent deviceNotificationEvent;
        try
        {
            Marshal.WriteInt32(ptr, 0, size);
            Marshal.WriteInt32(ptr, 4, (int)DeviceBroadcastDeviceType.DeviceInterface);
            Marshal.WriteInt32(ptr, 8, 0);
            Marshal.Copy(classGuid.ToByteArray(), 0, IntPtr.Add(ptr, 12), 16);
            var nameBytes = System.Text.Encoding.Unicode.GetBytes(deviceName + "\0");
            Marshal.Copy(nameBytes, 0, IntPtr.Add(ptr, nameOffset), nameBytes.Length);

            deviceNotificationEvent = new DeviceNotificationEvent((IntPtr)(int)DeviceChangeEvent.DeviceArrival, ptr);
        }
        finally
        {
            // The event must not depend on the native memory after the message was processed
            Marshal.FreeHGlobal(ptr);
        }

        Assert.Equal(DeviceChangeEvent.DeviceArrival, deviceNotificationEvent.EventType);
        Assert.True(deviceNotificationEvent.Is(DeviceBroadcastDeviceType.DeviceInterface));
        Assert.False(deviceNotificationEvent.TryGetDevBroadcastVolume(out _));
        Assert.True(deviceNotificationEvent.TryGetDevBroadcastDeviceInterface(out var device));
        Assert.Equal(deviceName, device.Name);
        Assert.Equal(classGuid, device.DeviceClassGuid);
        Assert.Equal(DeviceInterfaceClass.StillImage, device.DeviceClass);
        Assert.Equal("05AC", device.VendorId);
    }

    [Fact]
    public void TestParse_2()
    {
        const string graphicsCard =
            @"\\?\PCI#VEN_10DE&DEV_1FB8&SUBSYS_09061028&REV_A1#4&32af3f68&0&0008#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}";
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(graphicsCard, DeviceInterfaceClass.DisplayDeviceArrival);
        Assert.Equal("10DE", devBroadcastDeviceInterface.VendorId);
        Assert.Equal("1ca05180-a699-450a-9a0c-de4fbe3ddd89", devBroadcastDeviceInterface.DeviceClassGuid.ToString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(@"PCI\VEN_10DE&DEV_1FB8&SUBSYS_09061028&REV_A1\4&32af3f68&0&0008", devBroadcastDeviceInterface.DisplayName);
        Assert.True(devBroadcastDeviceInterface.IsPci);
        Log.Info().WriteLine("More information: {0}", devBroadcastDeviceInterface.UsbDeviceInfoUri);
    }

    [Fact]
    public void TestDeviceClassGuids()
    {
        // This test documents the difference between DeviceClassGuid and DeviceSetupClassGuid
        // Using an NVIDIA Quadro T1000 graphics card as the example device
        
        // DeviceClassGuid (Device Interface Class GUID): comes from the device notification message
        // For display device arrival events, this is the GUID_DISPLAY_DEVICE_ARRIVAL constant
        const string displayDeviceArrivalGuid = "1ca05180-a699-450a-9a0c-de4fbe3ddd89";
        
        const string graphicsCard =
            @"\\?\PCI#VEN_10DE&DEV_1FB8&SUBSYS_09061028&REV_A1#4&32af3f68&0&0008#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}";
        var devBroadcastDeviceInterface = DevBroadcastDeviceInterface.Test(graphicsCard, DeviceInterfaceClass.DisplayDeviceArrival);
        
        // DeviceClassGuid should be the interface class from the notification
        Assert.Equal(new Guid(displayDeviceArrivalGuid), devBroadcastDeviceInterface.DeviceClassGuid);
        
        // DeviceSetupClassGuid would be retrieved from registry (e.g., {4d36e968-e325-11ce-bfc1-08002be10318} for Display adapters)
        // In a test environment without the actual registry key, this will be null
        // Note: DeviceSetupClassGuid is what's shown in Windows Device Manager under "Class GUID"
        var setupClassGuid = devBroadcastDeviceInterface.DeviceSetupClassGuid;
        Log.Info().WriteLine("DeviceSetupClassGuid (from registry): {0}", setupClassGuid?.ToString() ?? "null (registry key not found)");
    }
}