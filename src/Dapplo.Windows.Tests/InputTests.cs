﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.User32;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class InputTests
{

    public InputTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test LastInputTimeSpan
    /// </summary>
    [Fact]
    private async Task TestInput_LastInputTimeSpan()
    {
        var initialLastInputTimeSpan = NativeInput.LastInputTimeSpan;
        await Task.Delay(100);
        var laterLastInputTimeSpan = NativeInput.LastInputTimeSpan;
        Assert.True(laterLastInputTimeSpan > initialLastInputTimeSpan);
    }

    /// <summary>
    ///     Test LastInputDateTime
    /// </summary>
    [Fact]
    private async Task TestInput_LastInputDateTime()
    {
        var initialLastInput = NativeInput.LastInputDateTime;
        await Task.Delay(1300);
        var laterLastInput = NativeInput.LastInputDateTime;
        var deviation = laterLastInput.Subtract(initialLastInput);
        Assert.True(deviation < TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    ///     Test typing in a notepad
    /// </summary>
    [Fact]
    private async Task TestInput()
    {
        // Start a process to test against
        using (var process = Process.Start("notepad.exe"))
        {
            // Make sure it's started
            Assert.NotNull(process);
            // Wait until the process started it's message pump (listening for input)
            process.WaitForInputIdle();

            User32Api.SetWindowText(process.MainWindowHandle, "TestInput");

            // Find the belonging window
            var notepadWindow = await WindowsEnumerator.EnumerateWindowsAsync()
                .Where(interopWindow =>
                {
                    User32Api.GetWindowThreadProcessId(interopWindow.Handle, out var processId);
                    return processId == process.Id;
                })
                .FirstOrDefaultAsync();
            Assert.NotNull(notepadWindow);

            // Send input
            var sentInputs = KeyboardInputGenerator.KeyPresses(VirtualKeyCode.KeyR, VirtualKeyCode.KeyO, VirtualKeyCode.KeyB, VirtualKeyCode.KeyI, VirtualKeyCode.KeyN);
            // Test if we indead sent 10 inputs (5 x down & up)
            Assert.Equal((uint) 10, sentInputs);

            // Kill the process
            process.Kill();
        }
    }

    /// <summary>
    ///     Test typing in a notepad
    /// </summary>
    /// <returns></returns>
    [Fact]
    private void TestMouseInput()
    {
        MouseInputGenerator.MoveMouse(new NativePoint(10, 10));
        Thread.Sleep(100);
        MouseInputGenerator.MoveMouse(new NativePoint(100, 100));
        Thread.Sleep(100);
    }
}