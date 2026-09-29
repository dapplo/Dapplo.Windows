// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class KeyboardHookTests
{
    private static LogSource Log = new LogSource();
    public KeyboardHookTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [StaFact]
    public async Task TestKeyHandler_SingleCombination()
    {
        int pressCount = 0;
        var keyHandler = new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift)
        {
            IgnoreInjected = false,
            IsPassThrough = false
        };
        using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        {
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await Task.Delay(20);
        }
        Assert.True(pressCount == 2);
        KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
        await Task.Delay(20);
        Assert.True(pressCount == 2);
    }

    [StaFact]
    public async Task TestKeyHandler_Slow_Subscriber()
    {
        int pressCount = 0;
        const int pressHandlingTime = 500;
        // Wait 2x press plus overhead
        const int waitForPressHandling = (int)((pressHandlingTime * 2) * 1.1);

        var sequenceHandler = new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false };

        // The handler decides synchronously on the hook thread, the slow work is moved away from the hook thread with ObserveOn
        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).ObserveOn(ThreadPoolScheduler.Instance).Subscribe(keyboardHookEventArgs =>
               {
                   Log.Info().WriteLine("Key combination was pressed, slow handling!", null);
                   Thread.Sleep(pressHandlingTime);
                   Log.Info().WriteLine("Key combination was pressed, finished!", null);
                   Interlocked.Increment(ref pressCount);
               }))
        {
            Log.Info().WriteLine("Pressing key combination", null);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            Log.Info().WriteLine("Pressed key combination", null);
            Log.Info().WriteLine("Pressing key combination", null);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            Log.Info().WriteLine("Pressed key combination", null);
            await Task.Delay(waitForPressHandling);
            Assert.Equal(2, pressCount);
        }
    }

    [StaFact]
    public async Task TestKeyHandler_Sequence_InputGenerator()
    {
        int pressCount = 0;
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print) { IgnoreInjected = false },
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false });


        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        {
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await Task.Delay(20);
            Assert.True(pressCount == 0);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
            await Task.Delay(20);
            Assert.True(pressCount == 0);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await Task.Delay(20);
            Assert.True(pressCount == 1);
        }
    }

    [StaFact]
    public async Task TestKeyHandler_SequenceWithOptionalKeys_KeyboardInputGenerator()
    {
        int pressCount = 0;
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print) {IgnoreInjected = false},
            new KeyOrCombinationHandler(
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false },
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB) { IgnoreInjected = false })
        )
        {
            // Timeout for test
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
        {
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await Task.Delay(20);
            Assert.Equal(0, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
            await Task.Delay(20);
            Assert.Equal(1, pressCount);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await Task.Delay(20);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await Task.Delay(20);
            Assert.Equal(2, pressCount);

            // Test with timeout, waiting to long
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
            await Task.Delay(400);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
            await Task.Delay(20);
            Assert.Equal(2, pressCount);
        }
    }

    //[StaFact]
    private async Task TestHandlingKeyAsync()
    {
        await KeyboardHook.KeyboardEvents.Where(args => args.IsWindows && args.IsShift && args.IsControl && args.IsAlt)
            .Select(args =>
            {
                args.Handled = true;
                return args;
            })
            .FirstAsync();
    }

    //[StaFact]
    private async Task TestMappingAsync()
    {
        await KeyboardHook.KeyboardEvents.FirstAsync(info => info.IsLeftShift && info.IsKeyDown);
    }

    //[StaFact]
    private async Task TestSuppressVolumeAsync()
    {
        await KeyboardHook.KeyboardEvents.Where(args =>
            {
                if (args.Key != VirtualKeyCode.VolumeUp)
                {
                    return true;
                }
                args.Handled = true;
                return false;
            })
            .FirstAsync();
    }
}