// Copyright (c) Dapplo and contributors. All rights reserved.
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

namespace Dapplo.Windows.Tests;

/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class InputTests
{

    public InputTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test LastInputTimeSpan, an injected (harmless) shift press is input
    /// </summary>
    [Fact]
    public async Task TestInput_LastInputTimeSpan()
    {
        Assert.Equal(4u, KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Shift, VirtualKeyCode.Shift));
        await TestWait.UntilAsync(() => NativeInput.LastInputTimeSpan < TimeSpan.FromSeconds(1), "The injected input didn't update the last input time");
    }

    /// <summary>
    ///     Test LastInputDateTime, an injected (harmless) shift press is input
    /// </summary>
    [Fact]
    public async Task TestInput_LastInputDateTime()
    {
        var beforeInput = DateTimeOffset.Now;
        Assert.Equal(2u, KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Shift));
        // The tick count has a resolution of 10-16 ms, allow some deviation
        var tolerance = TimeSpan.FromMilliseconds(100);
        await TestWait.UntilAsync(() => NativeInput.LastInputDateTime >= beforeInput - tolerance, "The injected input didn't update the last input time");
        Assert.True(NativeInput.LastInputDateTime <= DateTimeOffset.Now + tolerance);
    }

    /// <summary>
    ///     Test generating keyboard input, only harmless modifier keys are pressed
    /// </summary>
    [Fact]
    public void TestInput()
    {
        var sentInputs = KeyboardInputGenerator.KeyPresses(VirtualKeyCode.Shift, VirtualKeyCode.Control);
        // 2 x down & up
        Assert.Equal(4u, sentInputs);
    }

    /// <summary>
    ///     Test moving the mouse, the cursor is restored afterwards
    /// </summary>
    [Fact]
    public async Task TestMouseInput()
    {
        var originalLocation = User32Api.GetCursorLocation();
        try
        {
            var target = new NativePoint(10, 10);
            Assert.Equal(1u, MouseInputGenerator.MoveMouse(target));
            // The absolute coordinates are normalized to 0-65535, allow a rounding difference
            await TestWait.UntilAsync(() =>
            {
                var location = User32Api.GetCursorLocation();
                return Math.Abs(location.X - target.X) <= 1 && Math.Abs(location.Y - target.Y) <= 1;
            }, "The mouse didn't move to the target location");
        }
        finally
        {
            MouseInputGenerator.MoveMouse(originalLocation);
        }
    }
}