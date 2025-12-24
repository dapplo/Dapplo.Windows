// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests;

public class KeyboardHookTests
{
    private static LogSource Log = new LogSource();
    public KeyboardHookTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    [StaFact]
    private async Task TestKeyHandler_SingleCombination()
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
    private async Task TestKeyHandler_Slow_Subscriber()
    {
        int pressCount = 0;
        const int pressHandlingTime = 500;
        // Wait 2x press plus overhead
        const int waitForPressHandling = (int)((pressHandlingTime * 2) * 1.1);

        var sequenceHandler = new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false };

        using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs =>
               {
                   Log.Info().WriteLine("Key combination was pressed, slow handling!", null);
                   Thread.Sleep(pressHandlingTime);
                   Log.Info().WriteLine("Key combination was pressed, finished!", null);
                   pressCount++;
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
    private async Task TestKeyHandler_Sequence_InputGenerator()
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

    [Fact]
    private void TestKeyHandler_KeySequenceHandler_Wrong_Right()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA))
        {
            // Prevent debug issues, we are not testing the timeout here!
            Timeout = null
        };

        var result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyB));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print));
        Assert.False(result);

        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA));
        Assert.True(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyA));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift));
        Assert.False(result);
        Assert.False(sequenceHandler.HasKeysPressed);
    }

    [Fact]
    private void TestKeyHandler_KeySequenceHandler_Right()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA))
        {
            Timeout = null
        };

        var result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA));
        Assert.True(result);
    }

    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_Repeat()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

        var keyPrintDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print);

        var keyPrintUp = KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print);

        var result = keyCombinationHandler.Handle(keyPrintDown);
        Assert.True(result);
        result = keyCombinationHandler.Handle(keyPrintDown);
        Assert.False(result);

        // Key up again
        result = keyCombinationHandler.Handle(keyPrintUp);
        Assert.False(result);
        result = keyCombinationHandler.Handle(keyPrintDown);
        Assert.True(result);
    }

    /// <summary>
    /// Test that after a key down, having a not matching key down & up we should not have a "hit"
    /// </summary>
    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_KeyUp()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.True(result);
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
    }

    [StaFact]
    private async Task TestKeyHandler_SequenceWithOptionalKeys_KeyboardInputGenerator()
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

    [Fact]
    private async Task TestKeyHandler_SequenceWithOptionalKeys_Timeout()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyOrCombinationHandler(
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB))
        )
        {
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        bool result;
        // Print key
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print));
        Assert.False(result);

        // Shift KeyB
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.True(sequenceHandler.HasKeysPressed);
        Assert.False(result);
        await Task.Delay(400);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB));
        Assert.True(sequenceHandler.HasKeysPressed);
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyB));
        Assert.True(sequenceHandler.HasKeysPressed);
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift));
        Assert.False(sequenceHandler.HasKeysPressed);
        Assert.False(result);
    }

    [Fact]
    private void TestKeyHandler_SequenceWithOptionalKeys_OneTry()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyOrCombinationHandler(
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB))
        )
        {
            Timeout = null
        };

        bool result;

        // Print key
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print));
        Assert.False(result);

        // Shift KeyB
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB));
        Assert.True(result);
        // Shift KeyB
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyB));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift));
        Assert.False(result);
    }

    [Fact]
    private void TestKeyHelper_VirtualKeyCodesFromString()
    {
        const string testKeys = "ctrl + shift + A";
        var virtualKeyCodes = KeyHelper.VirtualKeyCodesFromString(testKeys).ToList();
        Assert.NotEmpty(virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.Shift,virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.Control, virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.KeyA, virtualKeyCodes);
    }

    [Fact]
    private void TestKeyHelper_VirtualCodeToLocaleDisplayText()
    {
        var keyCombination = string.Join(" + ", new[] {VirtualKeyCode.LeftShift, VirtualKeyCode.KeyA}.Select(vk => KeyHelper.VirtualCodeToLocaleDisplayText(vk, false)));

        Assert.NotEmpty(keyCombination);
        Assert.Contains("+ A", keyCombination);
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

    /// <summary>
    /// Test that TriggerOnKeyUp triggers when all keys are released, not when pressed
    /// </summary>
    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_TriggerOnKeyUp_SingleKey()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print)
        {
            TriggerOnKeyUp = true
        };

        // Key down should not trigger
        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.False(result);
        
        // Key up should trigger
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print));
        Assert.True(result);
    }

    /// <summary>
    /// Test that TriggerOnKeyUp works correctly with key combinations
    /// </summary>
    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_TriggerOnKeyUp_Combination()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerOnKeyUp = true
        };

        // Press all keys in the combination
        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA));
        Assert.False(result); // Should not trigger on key down
        
        // Start releasing keys - should trigger on first key up
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyA));
        Assert.True(result);
        
        // Further releases should not trigger
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
    }

    /// <summary>
    /// Test that TriggerOnKeyUp does not trigger if an extra key was pressed
    /// </summary>
    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_TriggerOnKeyUp_WithExtraKey()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerOnKeyUp = true
        };

        // Press the combination keys
        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA));
        Assert.False(result);
        
        // Press an extra key
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB));
        Assert.False(result);
        
        // Release combination key - should not trigger because extra key is pressed
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyA));
        Assert.False(result);
        
        // Release extra key
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyB));
        Assert.False(result);
        
        // Release last combination key
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
    }

    /// <summary>
    /// Test that TriggerOnKeyUp does not trigger if not all combination keys were pressed
    /// </summary>
    [Fact]
    private void TestKeyHandler_KeyCombinationHandler_TriggerOnKeyUp_PartialPress()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerOnKeyUp = true
        };

        // Press only some keys
        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA));
        Assert.False(result);
        
        // Release a key without having pressed Shift - should not trigger
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyA));
        Assert.False(result);
        
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
    }
}