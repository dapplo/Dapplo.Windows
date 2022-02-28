// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
                Log.Info().WriteLine("RawInput Device {0} with handle {1} and name {2} on {3}", rawInputDeviceInfo.DeviceInfo.Type, rawInputDeviceInfo.DeviceHandle, rawInputDeviceInfo.DisplayName, rawInputDeviceInfo.DeviceName);
                switch (rawInputDeviceInfo.DeviceInfo.Type)
                {
                    case RawInputDeviceTypes.Keyboard:
                        var keyboardInfo = rawInputDeviceInfo.DeviceInfo.Keyboard;
                        Log.Info().WriteLine("Keyboard is of type {0} and subtype {1} and in mode {2}.", keyboardInfo.Type, keyboardInfo.SubType, keyboardInfo.KeyboardMode);
                        Log.Info().WriteLine("Keyboard with {0} keys, of which {1} function keys and it has {2} LEDs.", keyboardInfo.NumberOfKeysTotal, keyboardInfo.NumberOfFunctionKeys, keyboardInfo.NumberOfIndicators);
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
            Log.Debug().WriteLine($"Display name: {device.DeviceInformation.DisplayName} Device name: {device.DeviceInformation.DeviceName}");
            Assert.Equal(RawInputDeviceTypes.Keyboard, device.DeviceInformation.DeviceInfo.Type);
        }

        //[WpfFact]
        public async Task Test_RawInput_Left()
        {
            var rawInputObservable = RawInputMonitor.MonitorRawInput(RawInputDevices.Keyboard);

            using (rawInputObservable.Subscribe(ri =>
                   {
                       switch (ri.RawInput.Device.Keyboard.Flags)
                       {
                           case RawKeyboardFlags.Make:
                               Log.Debug().WriteLine("Key down {0} from handle {1}", ri.RawInput.Device.Keyboard.VirtualKey, ri.RawInput.Header.DeviceHandle);
                               break;
                           case RawKeyboardFlags.Break:
                               Log.Debug().WriteLine("Key up {0} from handle {1}", ri.RawInput.Device.Keyboard.VirtualKey, ri.RawInput.Header.DeviceHandle);
                               break;
                       }
                   }))
            {
                var rawInputEvent = await rawInputObservable.FirstAsync(args => args.RawInput.Device.Keyboard.VirtualKey == VirtualKeyCode.Left);
                Log.Debug().WriteLine("From handle {0}", rawInputEvent.RawInput.Header.DeviceHandle);
                var device = RawInputMonitor.Devices[rawInputEvent.RawInput.Header.DeviceHandle];

                Assert.Equal(RawInputDeviceTypes.Keyboard, device.DeviceInfo.Type);
            }

        }
    }
}