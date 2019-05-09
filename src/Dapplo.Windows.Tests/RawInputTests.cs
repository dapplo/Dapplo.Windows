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

#region using

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class RawInputTests
    {
        private static readonly LogSource Log = new LogSource();
        public RawInputTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test RawInput.GetAllDevices
        /// </summary>
        [Fact]
        public void Test_RawInput_AllDevices()
        {
            bool foundOneDevice = false;
            foreach (var rawInputDeviceInfo in RawInputApi.GetAllDevices().OrderBy(information => information.DeviceInfo.Type).ThenBy(information => information.DisplayName))
            {
                Log.Info().WriteLine("RawInput Device {0} with name {1}", rawInputDeviceInfo.DeviceInfo.Type, rawInputDeviceInfo.DisplayName);
                switch (rawInputDeviceInfo.DeviceInfo.Type)
                {
                    case RawInputDeviceTypes.Keyboard:
                        var keyboardInfo = rawInputDeviceInfo.DeviceInfo.Keyboard;
                        Log.Info().WriteLine("Keyboard is of type {0} and subtype {1} and in mode {2}.", keyboardInfo.Type, keyboardInfo.SubType, keyboardInfo.KeyboardMode);
                        Log.Info().WriteLine("Keyboard with {0} key, of which {1} function keys and it has {2} LEDs.", keyboardInfo.NumberOfKeysTotal, keyboardInfo.NumberOfFunctionKeys, keyboardInfo.NumberOfIndicators);
                        break;
                }

                foundOneDevice = true;
            }
            Assert.True(foundOneDevice);
        }

        //[WpfFact]
        public async Task Test_RawInput_DeviceChanges_KeyboardRemoved()
        {
            var device = await RawInputMonitor.MonitorRawInputDeviceChanges(RawInputDevices.Keyboard).Where(args => !args.Added).FirstAsync();
            Assert.False(device.Added);
            Assert.Equal(RawInputDeviceTypes.Keyboard, device.DeviceInformation.DeviceInfo.Type);
        }

        //[WpfFact]
        public async Task Test_RawInput_Left()
        {
            var rawInputObservable = RawInputMonitor.MonitorRawInput(RawInputDevices.Keyboard);

            using (rawInputObservable.Subscribe(ri =>
            {
                if (ri.RawInput.Device.Keyboard.Flags == RawKeyboardFlags.Break)
                {
                    Log.Debug().WriteLine("Key down {0}", ri.RawInput.Device.Keyboard.VirtualKey);
                }
                if (ri.RawInput.Device.Keyboard.Flags == RawKeyboardFlags.Break)
                {
                    Log.Debug().WriteLine("Key up {0}", ri.RawInput.Device.Keyboard.VirtualKey);
                }
            }))
            {
                var device = await rawInputObservable.FirstAsync(args => args.RawInput.Device.Keyboard.VirtualKey == VirtualKeyCode.Left);
                Assert.Equal(RawInputDeviceTypes.Keyboard, device.RawInput.Header.Type);
            }

        }
    }
}