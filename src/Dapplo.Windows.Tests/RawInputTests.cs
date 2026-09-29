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
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages;
using System.Runtime.InteropServices;
using Xunit;

namespace Dapplo.Windows.Tests;

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
        var device = await RawInputDeviceMonitor.Listen(RawInputDevices.Keyboard).Where(args => !args.Added).FirstAsync();
        Assert.False(device.Added);
        Assert.Equal(RawInputDeviceTypes.Keyboard, device.DeviceInformation.DeviceInfo.Type);
    }

    //[WpfFact]
    public async Task Test_RawInput_Left()
    {
        var rawInputObservable = RawInputMonitor.Listen(RawInputDevices.Keyboard);

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

    /// <summary>
    /// Build a RAWINPUT buffer, as GetRawInputData returns it for the current process
    /// </summary>
    private static byte[] CreateRawInputBuffer(RawInputDeviceTypes type, byte[] payload)
    {
        var headerSize = Marshal.SizeOf<RawInputHeader>();
        var buffer = new byte[headerSize + payload.Length];
        BitConverter.GetBytes((uint)type).CopyTo(buffer, 0);
        BitConverter.GetBytes((uint)buffer.Length).CopyTo(buffer, 4);
        Array.Copy(payload, 0, buffer, headerSize, payload.Length);
        return buffer;
    }

    [Fact]
    public void Test_RawInput_Parse_Hid()
    {
        // RAWHID: dwSizeHid = 3, dwCount = 2, followed inline by 6 bytes
        var payload = new byte[] { 3, 0, 0, 0, 2, 0, 0, 0, 1, 2, 3, 4, 5, 6 };
        var buffer = CreateRawInputBuffer(RawInputDeviceTypes.HID, payload);

        Assert.True(RawInputApi.TryParseRawInput(buffer, out var rawInput, out var hidData));
        Assert.Equal(RawInputDeviceTypes.HID, rawInput.Header.Type);
        Assert.Equal(3u, rawInput.Device.HID.SizeHid);
        Assert.Equal(2u, rawInput.Device.HID.Count);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, hidData);

        // Truncated data must not be read
        Assert.False(RawInputApi.TryParseRawInput(buffer.Take(buffer.Length - 1).ToArray(), out _, out _));
    }

    [Fact]
    public void Test_RawInput_Parse_Keyboard()
    {
        // RAWKEYBOARD: MakeCode 0x1E, Flags 0 (make), Reserved 0, VKey 0x41 (A), Message WM_KEYDOWN, ExtraInformation 0
        var payload = new byte[16];
        BitConverter.GetBytes((ushort)0x1E).CopyTo(payload, 0);
        BitConverter.GetBytes((ushort)0x41).CopyTo(payload, 6);
        BitConverter.GetBytes(0x0100u).CopyTo(payload, 8);
        var buffer = CreateRawInputBuffer(RawInputDeviceTypes.Keyboard, payload);

        Assert.True(RawInputApi.TryParseRawInput(buffer, out var rawInput, out var hidData));
        Assert.Null(hidData);
        Assert.Equal(RawInputDeviceTypes.Keyboard, rawInput.Header.Type);
        Assert.Equal(VirtualKeyCode.KeyA, rawInput.Device.Keyboard.VirtualKey);
        Assert.Equal((ushort)0x1E, rawInput.Device.Keyboard.ScanCode);
    }

    /// <summary>
    /// Two listeners for the keyboard must combine their registrations, and remove them again when they are disposed.
    /// This only registers for raw input in the test process, no input is generated.
    /// </summary>
    [Fact]
    public async Task Test_RawInput_Registrations_AreCombined()
    {
        const ushort keyboardUsage = 0x06;
        RawInputDevice? FindKeyboard() => RawInputApi.GetRegisteredDevices()
            .Where(device => device.UsagePage == HidUsagePages.Generic && device.Usage == keyboardUsage)
            .Select(device => (RawInputDevice?)device)
            .FirstOrDefault();

        Assert.Null(FindKeyboard());
        var inputSubscription = RawInputMonitor.Listen(RawInputDevices.Keyboard).Subscribe(_ => { });
        try
        {
            var registration = FindKeyboard();
            Assert.NotNull(registration);
            Assert.Equal(RawInputDeviceFlags.InputSink, registration.Value.Flags);
            Assert.Equal(SharedMessageWindow.Handle, registration.Value.TargetHwnd);

            var arrivedKeyboard = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (RawInputDeviceMonitor.Listen(RawInputDevices.Keyboard).Where(args => args.Added).Subscribe(_ => arrivedKeyboard.TrySetResult(true)))
            {
                registration = FindKeyboard();
                Assert.NotNull(registration);
                Log.Info().WriteLine("Combined keyboard registration: {0}", registration.Value);
                // The device monitor must not strip the InputSink of the other listener
                Assert.True((registration.Value.Flags & RawInputDeviceFlags.InputSink) != 0, $"InputSink was removed: {registration.Value}");
                // Windows reports the present devices as arrivals when RIDEV_DEVNOTIFY is registered
                var completed = await Task.WhenAny(arrivedKeyboard.Task, Task.Delay(2000, TestContext.Current.CancellationToken));
                Assert.True(completed == arrivedKeyboard.Task, "No WM_INPUT_DEVICE_CHANGE arrival was received for a keyboard");
            }

            registration = FindKeyboard();
            Assert.NotNull(registration);
            Assert.Equal(RawInputDeviceFlags.InputSink, registration.Value.Flags);
        }
        finally
        {
            inputSubscription.Dispose();
        }
        Assert.Null(FindKeyboard());
    }
}
