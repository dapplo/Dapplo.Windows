// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Pure logic tests for the keyboard handlers, these feed synthetic KeyboardHookEventArgs and don't need the keyboard hook or the desktop.
/// </summary>
public class KeyboardHandlerTests
{
    [Fact]
    public void TestKeyHandler_KeySequenceHandler_Wrong_Right()
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
    public void TestKeyHandler_KeySequenceHandler_Wrong_Right_ModifierReleasedLast()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA));

        // First stage
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print)));
        // Wrong second stage, released in reverse order so the modifier comes up last (like KeyboardInputGenerator.KeyCombinationPress)
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyB)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Shift)));
        // The sequence must have been reset, so the full sequence triggers
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift)));
        Assert.True(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA)));
    }

    [Fact]
    public void TestKeyHandler_KeySequenceHandler_Right()
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
    public void TestKeyHandler_KeyCombinationHandler_Repeat()
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
    public void TestKeyHandler_KeyCombinationHandler_KeyUp()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

        var result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print));
        Assert.True(result);
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Control));
        Assert.False(result);
        result = keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Control));
        Assert.False(result);
    }

    [Fact]
    public async Task TestKeyHandler_SequenceWithOptionalKeys_Timeout()
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
        await Task.Delay(400, TestContext.Current.CancellationToken);
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
    public void TestKeyHandler_SequenceWithOptionalKeys_OneTry()
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
    public void TestKeyHelper_VirtualKeyCodesFromString()
    {
        const string testKeys = "ctrl + shift + A";
        var virtualKeyCodes = KeyHelper.VirtualKeyCodesFromString(testKeys).ToList();
        Assert.NotEmpty(virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.Shift,virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.Control, virtualKeyCodes);
        Assert.Contains(VirtualKeyCode.KeyA, virtualKeyCodes);
    }

    [Fact]
    public void TestKeyHelper_VirtualCodeToLocaleDisplayText()
    {
        var keyCombination = string.Join(" + ", new[] {VirtualKeyCode.LeftShift, VirtualKeyCode.KeyA}.Select(vk => KeyHelper.VirtualCodeToLocaleDisplayText(vk, false)));

        Assert.NotEmpty(keyCombination);
        Assert.Contains("+ A", keyCombination);
    }

    /// <summary>
    /// Test that TriggerMode.FirstKeyUp triggers when a key is released, not when pressed
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_FirstKeyUp_SingleKey()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print)
        {
            TriggerMode = TriggerMode.FirstKeyUp
        };

        // Key down should not trigger
        var keyDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print);
        var result = keyCombinationHandler.Handle(keyDown);
        Assert.False(result);
        Assert.False(keyDown.Handled);

        // Key up should trigger, but must not be swallowed as the key down was passed through
        var keyUp = KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print);
        result = keyCombinationHandler.Handle(keyUp);
        Assert.True(result);
        Assert.False(keyUp.Handled);
    }

    /// <summary>
    /// Test that TriggerMode.FirstKeyUp still triggers when the key was auto-repeated before it was released (CanRepeat is false by default)
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_FirstKeyUp_AfterAutoRepeat()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyT)
        {
            TriggerMode = TriggerMode.FirstKeyUp
        };

        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftControl)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));
        // Auto repeat
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));

        Assert.True(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyT)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.LeftControl)));
    }

    /// <summary>
    /// Test that, with TriggerMode.KeyDown, a repeated key down is swallowed but doesn't trigger again
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_Repeat_IsSwallowedButDoesNotTrigger()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyT);

        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftControl)));
        var keyDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT);
        Assert.True(keyCombinationHandler.Handle(keyDown));
        Assert.True(keyDown.Handled);

        var repeat = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT);
        Assert.False(keyCombinationHandler.Handle(repeat));
        Assert.True(repeat.Handled);

        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyT)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.LeftControl)));
    }

    /// <summary>
    /// Test that TriggerMode.FirstKeyUp works correctly with key combinations
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_FirstKeyUp_Combination()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.FirstKeyUp
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
    /// Test that TriggerMode.FirstKeyUp does not trigger if an extra key was pressed
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_FirstKeyUp_WithExtraKey()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.FirstKeyUp
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
    /// Test that TriggerMode.FirstKeyUp does not trigger if not all combination keys were pressed
    /// </summary>
    [Fact]
    public void TestKeyHandler_KeyCombinationHandler_FirstKeyUp_PartialPress()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.FirstKeyUp
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

    [Fact]
    public void TestVirtualKeyCodeExtensions_IsModifier()
    {
        var pureModifiers = new[]
        {
            VirtualKeyCode.Shift, VirtualKeyCode.LeftShift, VirtualKeyCode.RightShift,
            VirtualKeyCode.Control, VirtualKeyCode.LeftControl, VirtualKeyCode.RightControl,
            VirtualKeyCode.Menu, VirtualKeyCode.LeftMenu, VirtualKeyCode.RightMenu,
            VirtualKeyCode.LeftWin, VirtualKeyCode.RightWin
        };

        foreach (var key in pureModifiers)
        {
            Assert.True(key.IsModifier(), $"{key} should be a modifier");
            Assert.False(key.IsToggleKey(), $"{key} should not be a toggle key");
        }

        var toggleKeys = new[]
        {
            VirtualKeyCode.Capital,
            VirtualKeyCode.NumLock,
            VirtualKeyCode.Scroll
        };

        foreach (var key in toggleKeys)
        {
            Assert.False(key.IsModifier(), $"{key} should NOT be a modifier");
            Assert.True(key.IsToggleKey(), $"{key} should be a toggle key");
        }

        var ordinaryKeys = new[]
        {
            VirtualKeyCode.KeyA,
            VirtualKeyCode.Space,
            VirtualKeyCode.Return,
            VirtualKeyCode.Print,
            VirtualKeyCode.Pause
        };

        foreach (var key in ordinaryKeys)
        {
            Assert.False(key.IsModifier(), $"{key} should NOT be a modifier");
            Assert.False(key.IsToggleKey(), $"{key} should NOT be a toggle key");
        }
    }

    [Fact]
    public void TestKeyboardHookEventArgs_ModifiersAndToggleKeys()
    {
        var scrollDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Scroll);
        Assert.False(scrollDown.IsModifier);
        Assert.True(scrollDown.IsToggleKey);

        var capsUp = KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Capital);
        Assert.False(capsUp.IsModifier);
        Assert.True(capsUp.IsToggleKey);

        var numDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.NumLock);
        Assert.False(numDown.IsModifier);
        Assert.True(numDown.IsToggleKey);

        var shiftDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Shift);
        Assert.True(shiftDown.IsModifier);
        Assert.False(shiftDown.IsToggleKey);

        var aDown = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA);
        Assert.False(aDown.IsModifier);
        Assert.False(aDown.IsToggleKey);
    }

    [Fact]
    public void TestKeySequenceHandler_ScrollLockSequence()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Scroll),
            new KeyCombinationHandler(VirtualKeyCode.KeyC))
        {
            Timeout = null
        };

        // Press and release ScrollLock
        var result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Scroll));
        Assert.False(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Scroll));
        Assert.False(result);

        // Press and release C
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyC));
        Assert.True(result);
        result = sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyC));
        Assert.False(result);
        Assert.False(sequenceHandler.HasKeysPressed);
    }

    [Fact]
    public void TestKeyCombinationHandler_GenericWin_MatchesBothSides()
    {
        foreach (var winKey in new[] { VirtualKeyCode.LeftWin, VirtualKeyCode.RightWin })
        {
            var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Win, VirtualKeyCode.Shift, VirtualKeyCode.KeyS);
            Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(winKey)));
            Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.RightShift)));
            Assert.True(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyS)), $"{winKey} should satisfy Win");
            Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyS)));
            Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.RightShift)));
            Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(winKey)));
            Assert.False(keyCombinationHandler.HasKeysPressed);
        }
    }

    [Fact]
    public void TestVirtualKeyCode_GenericWin()
    {
        Assert.Equal(VirtualKeyCode.Win, KeyHelper.VirtualKeyCodeFromString("win"));
        Assert.Contains(VirtualKeyCode.Win, KeyHelper.VirtualKeyCodesFromString("Win + Shift + S"));
        Assert.True(VirtualKeyCode.Win.IsModifier());
        Assert.True(VirtualKeyCode.Win.IsGeneric());
        Assert.True(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Win).IsModifier);
        Assert.True(VirtualKeyCode.LeftWin.Matches(VirtualKeyCode.Win));
        Assert.True(VirtualKeyCode.RightWin.Matches(VirtualKeyCode.Win));
        Assert.False(VirtualKeyCode.RightWin.Matches(VirtualKeyCode.LeftWin));
        Assert.False(VirtualKeyCode.Win.Matches(VirtualKeyCode.LeftWin));
    }

    [Fact]
    public void TestKeyCombinationHandler_MissedKeyUp_OtherKey_IsForgotten()
    {
        var pressed = new HashSet<VirtualKeyCode>();
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyT)
        {
            KeyStateVerifier = pressed.Contains
        };

        pressed.Add(VirtualKeyCode.KeyB);
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB)));
        // The key-up of B is missed (e.g. released on the secure desktop)
        pressed.Remove(VirtualKeyCode.KeyB);

        pressed.Add(VirtualKeyCode.LeftControl);
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftControl)));
        pressed.Add(VirtualKeyCode.KeyT);
        Assert.True(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));
    }

    [Fact]
    public void TestKeyCombinationHandler_MissedKeyUp_SyntheticEventsAreNotVerifiedByDefault()
    {
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyT);
        // Synthetic events: without a KeyStateVerifier nothing is forgotten, the physical key state is not used
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyB)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftControl)));
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));
        Assert.True(keyCombinationHandler.HasKeysPressed);
    }

    [Fact]
    public void TestKeyCombinationHandler_MissedKeyUp_CombinationKey_IsForgotten()
    {
        var pressed = new HashSet<VirtualKeyCode>();
        var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyT)
        {
            KeyStateVerifier = pressed.Contains
        };

        pressed.Add(VirtualKeyCode.LeftControl);
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftControl)));
        // The key-up of the control key is missed
        pressed.Remove(VirtualKeyCode.LeftControl);

        // T alone must not trigger the combination
        pressed.Add(VirtualKeyCode.KeyT);
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyT)));
        pressed.Remove(VirtualKeyCode.KeyT);
        Assert.False(keyCombinationHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyT)));
        Assert.False(keyCombinationHandler.HasKeysPressed);
    }

    [Fact]
    public async Task TestKeySequenceHandler_AfterTimeout_FirstKeyRestartsSequence()
    {
        var sequenceHandler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Print),
            new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA))
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };

        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print)));

        // Let the sequence expire
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // The first press of Print must start the sequence again, not be consumed by the stale second stage
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.Print)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.LeftShift)));
        Assert.True(sequenceHandler.Handle(KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.KeyA)));
        Assert.False(sequenceHandler.Handle(KeyboardHookEventArgs.KeyUp(VirtualKeyCode.LeftShift)));
    }

    [Fact]
    public void TestKeyHelper_VirtualCodeToLocaleDisplayText_ExtendedKeys()
    {
        // RightControl has an E0 scan code, it must not be named like LeftControl
        var leftControl = KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.LeftControl, false);
        var rightControl = KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.RightControl, false);
        Assert.NotEqual(leftControl, rightControl);
        Assert.Equal(KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.LeftControl), KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.RightControl));

        // PrintScreen (E0 37) must not be named like the numpad * (37)
        var printScreen = KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.PrintScreen, false);
        Assert.False(string.IsNullOrEmpty(printScreen));
        Assert.DoesNotContain("*", printScreen);

        Assert.Contains("*", KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.Multiply));
        Assert.Contains("/", KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.Divide));

        Assert.Equal("Win", KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.LeftWin));
        Assert.Equal("Win", KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.Win));
        Assert.NotEqual("Win", KeyHelper.VirtualCodeToLocaleDisplayText(VirtualKeyCode.RightWin, false));
    }

    [Fact]
    public void TestMouseHookEventArgs_MouseData()
    {
        var mouseHookEventArgs = new MouseHookEventArgs
        {
            MouseData = 0xFF880000 // -120 in the high word
        };
        Assert.Equal(-120, mouseHookEventArgs.WheelDelta);
        mouseHookEventArgs.MouseData = 0x00020000;
        Assert.Equal(2, mouseHookEventArgs.XButton);
        Assert.False(mouseHookEventArgs.IsInjectedByProcess);
    }

    /// <summary>
    /// The hook runs on its own thread with a message loop, so subscribing from a thread without a message loop must work and the hook must install without error.
    /// No input is generated.
    /// </summary>
    [Fact]
    public async Task TestKeyboardHook_Subscribe_FromThreadWithoutMessageLoop()
    {
        Exception error = null;
        await Task.Run(() =>
        {
            using var subscription = KeyboardHook.KeyboardEvents.Subscribe(_ => { }, ex => error = ex);
            using var nonBlockingSubscription = KeyboardHook.KeyboardEventsNonBlocking.Subscribe(_ => { }, ex => error = ex);
            Thread.Sleep(50);
        }, TestContext.Current.CancellationToken);
        Assert.Null(error);
    }
}
