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

using System;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Devices.Enums;
using Dapplo.Windows.Devices.Structs;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
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
    }
}