// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Pure logic tests for TypeText (the INPUT structures), VK_PACKET handling, KeyboardState and the TriggerMode of the KeyCombinationHandler.
/// These don't send input and don't need the desktop.
/// </summary>
public class FcFeatureTests
{
    private static KeyboardHookEventArgs Down(VirtualKeyCode key) => KeyboardHookEventArgs.KeyDown(key);
    private static KeyboardHookEventArgs Up(VirtualKeyCode key) => KeyboardHookEventArgs.KeyUp(key);

    private static void AssertUnicode(KeyboardInput keyboardInput, char expected, bool isKeyUp)
    {
        Assert.Equal(VirtualKeyCode.None, keyboardInput.VirtualKeyCode);
        Assert.Equal(expected, unchecked((char)(ushort)keyboardInput.ScanCode));
        var expectedFlags = isKeyUp ? KeyEventFlags.Unicode | KeyEventFlags.KeyUp : KeyEventFlags.Unicode;
        Assert.Equal(expectedFlags, keyboardInput.KeyEventFlags);
        Assert.Equal(0u, keyboardInput.Timestamp);
    }

    private static void AssertKey(KeyboardInput keyboardInput, VirtualKeyCode expected, bool isKeyUp)
    {
        Assert.Equal(expected, keyboardInput.VirtualKeyCode);
        Assert.Equal(0, (int)(keyboardInput.KeyEventFlags & KeyEventFlags.Unicode));
        Assert.Equal(isKeyUp, (keyboardInput.KeyEventFlags & KeyEventFlags.KeyUp) != 0);
    }

    [Fact]
    public void ForText_Characters_DownAndUpPerCodeUnit()
    {
        var inputs = KeyboardInput.ForText("aé€");
        Assert.Equal(6, inputs.Length);
        AssertUnicode(inputs[0], 'a', false);
        AssertUnicode(inputs[1], 'a', true);
        AssertUnicode(inputs[2], 'é', false);
        AssertUnicode(inputs[3], 'é', true);
        AssertUnicode(inputs[4], '€', false);
        AssertUnicode(inputs[5], '€', true);
    }

    [Fact]
    public void ForText_SurrogatePair_HighThenLow()
    {
        // U+1F600, grinning face: D83D DE00
        var inputs = KeyboardInput.ForText("\U0001F600");
        Assert.Equal(4, inputs.Length);
        AssertUnicode(inputs[0], '\uD83D', false);
        AssertUnicode(inputs[1], '\uD83D', true);
        AssertUnicode(inputs[2], '\uDE00', false);
        AssertUnicode(inputs[3], '\uDE00', true);
    }

    [Fact]
    public void ForText_LineBreaksAndTab_AreKeys()
    {
        var inputs = KeyboardInput.ForText("a\r\nb\nc\rd\te");
        // 5 characters, 3 line breaks and a tab, each 2 events
        Assert.Equal(18, inputs.Length);
        AssertUnicode(inputs[0], 'a', false);
        AssertKey(inputs[2], VirtualKeyCode.Return, false);
        AssertKey(inputs[3], VirtualKeyCode.Return, true);
        AssertUnicode(inputs[4], 'b', false);
        AssertKey(inputs[6], VirtualKeyCode.Return, false);
        AssertUnicode(inputs[8], 'c', false);
        AssertKey(inputs[10], VirtualKeyCode.Return, false);
        AssertKey(inputs[11], VirtualKeyCode.Return, true);
        AssertUnicode(inputs[12], 'd', false);
        AssertKey(inputs[14], VirtualKeyCode.Tab, false);
        AssertKey(inputs[15], VirtualKeyCode.Tab, true);
        AssertUnicode(inputs[16], 'e', false);
        AssertUnicode(inputs[17], 'e', true);
    }

    [Fact]
    public void ForText_OtherControlCharacters_AreSkipped()
    {
        var inputs = KeyboardInput.ForText("a\0\b\u001B\u007F\u0085b");
        Assert.Equal(4, inputs.Length);
        AssertUnicode(inputs[0], 'a', false);
        AssertUnicode(inputs[2], 'b', false);
    }

    [Fact]
    public void ForText_EmptyAndNull()
    {
        Assert.Empty(KeyboardInput.ForText(string.Empty));
        Assert.Throws<ArgumentNullException>(() => KeyboardInput.ForText(null));
        Assert.Throws<ArgumentNullException>(() => KeyboardInputGenerator.TypeText(null));
        // Nothing to send, SendInput isn't called
        Assert.Equal(0u, KeyboardInputGenerator.TypeText(string.Empty));
        Assert.Equal(0u, KeyboardInputGenerator.TypeText("\0\b"));
    }

    [Fact]
    public void ForText_CreateKeyboardInputs_AreKeyboardInputs()
    {
        var inputs = Input.Structs.Input.CreateKeyboardInputs(KeyboardInput.ForText("x\U0001F44D"));
        Assert.Equal(6, inputs.Length);
        Assert.All(inputs, input => Assert.Equal(InputTypes.Keyboard, input.InputType));
        AssertUnicode(inputs[2].InputUnion.KeyboardInput, '\uD83D', false);
        AssertUnicode(inputs[5].InputUnion.KeyboardInput, '\uDC4D', true);
    }

    [Fact]
    public void KeyboardHookEventArgs_Packet()
    {
        var packet = KeyboardHookEventArgs.Packet('\uD83D', true);
        Assert.True(packet.IsPacket);
        Assert.True(packet.IsKeyDown);
        Assert.Equal(VirtualKeyCode.Packet, packet.Key);
        Assert.Equal('\uD83D', packet.PacketCharacter);
        Assert.Contains("U+D83D", packet.ToString());
        Assert.False(packet.IsModifier);

        var normal = KeyboardHookEventArgs.KeyDown(VirtualKeyCode.KeyA);
        Assert.False(normal.IsPacket);
        Assert.Equal('\0', normal.PacketCharacter);
    }

    [Theory]
    [InlineData(TriggerMode.KeyDown)]
    [InlineData(TriggerMode.FirstKeyUp)]
    [InlineData(TriggerMode.AllKeysUp)]
    public void KeyCombinationHandler_IgnoresPackets(TriggerMode triggerMode)
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = triggerMode,
            IgnoreInjected = false
        };
        var triggered = 0;
        void Feed(KeyboardHookEventArgs args)
        {
            if (handler.Handle(args))
            {
                triggered++;
            }
        }

        // Packets alone don't count as pressed keys
        var packetDown = KeyboardHookEventArgs.Packet('x', true);
        Assert.False(handler.Handle(packetDown));
        Assert.False(packetDown.Handled);
        Assert.False(handler.HasKeysPressed);
        Feed(KeyboardHookEventArgs.Packet('x', false));

        // Packets while the combination is pressed don't interrupt it
        Feed(Down(VirtualKeyCode.LeftControl));
        Feed(KeyboardHookEventArgs.Packet('y', true));
        Feed(Down(VirtualKeyCode.KeyA));
        Feed(KeyboardHookEventArgs.Packet('y', false));
        Feed(KeyboardHookEventArgs.Packet('z', true));
        Feed(Up(VirtualKeyCode.KeyA));
        Feed(KeyboardHookEventArgs.Packet('z', false));
        Feed(Up(VirtualKeyCode.LeftControl));

        Assert.Equal(1, triggered);
        Assert.False(handler.HasKeysPressed);
    }

    [Fact]
    public void KeySequenceHandler_IgnoresPackets()
    {
        var handler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyK),
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyC))
        {
            Timeout = null
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
        // A packet in the second stage would be a wrong, non-modifier, key if it weren't ignored
        Assert.False(handler.Handle(KeyboardHookEventArgs.Packet('q', true)));
        Assert.False(handler.Handle(KeyboardHookEventArgs.Packet('q', false)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.True(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_TriggersOnLastRelease_NotHandled()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        var events = new[]
        {
            Down(VirtualKeyCode.LeftControl), Down(VirtualKeyCode.RightShift), Down(VirtualKeyCode.KeyA),
            Up(VirtualKeyCode.KeyA), Up(VirtualKeyCode.LeftControl)
        };
        foreach (var args in events)
        {
            Assert.False(handler.Handle(args));
            Assert.False(args.Handled);
        }
        var last = Up(VirtualKeyCode.RightShift);
        Assert.True(handler.Handle(last));
        Assert.False(last.Handled);
        Assert.False(handler.HasKeysPressed);
    }

    [Fact]
    public void AllKeysUp_ReleaseOrderDoesNotMatter_AndFiresOncePerPress()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyD)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        // Modifier released last
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyD)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        // A stray key-up afterwards doesn't fire again
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        // Modifier released first
        Assert.False(handler.Handle(Down(VirtualKeyCode.RightControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.RightControl)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.KeyD)));
    }

    [Fact]
    public void AllKeysUp_AutoRepeat_FiresOnce()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyD)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyD)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_PartialPress_DoesNotFire()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.Shift, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_InterruptedByOtherKey_DoesNotFire()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        // Ctrl+A, then Ctrl+A+B: the user did something else
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyB)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyB)));
        // Auto-repeat of A doesn't arm the combination again
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        // The next clean press works
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_InterruptedWhilePartiallyReleased_DoesNotFire()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        // Ctrl is still down: Ctrl+C
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_CompletedAgainAfterInterruption_Fires()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyC)));
        // Still holding Ctrl, now a clean Ctrl+A
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_OtherKeyHeldBefore_DoesNotFire()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyB)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyB)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_MissedKeyUp_DoesNotFireLater()
    {
        var downKeys = new HashSet<VirtualKeyCode>();
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp,
            KeyStateVerifier = key => downKeys.Contains(key)
        };

        downKeys.Add(VirtualKeyCode.LeftControl);
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        downKeys.Add(VirtualKeyCode.KeyA);
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        downKeys.Remove(VirtualKeyCode.KeyA);
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        // The key-up of Ctrl is missed (e.g. released on the secure desktop)
        downKeys.Remove(VirtualKeyCode.LeftControl);

        // A alone: Ctrl is forgotten, so this is not the combination
        downKeys.Add(VirtualKeyCode.KeyA);
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyA)));
        downKeys.Remove(VirtualKeyCode.KeyA);
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyA)));
        Assert.False(handler.HasKeysPressed);
    }

    [Fact]
    public void AllKeysUp_SingleKey()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.PrintScreen)
        {
            TriggerMode = TriggerMode.AllKeysUp
        };
        var down = Down(VirtualKeyCode.PrintScreen);
        Assert.False(handler.Handle(down));
        Assert.False(down.Handled);
        var up = Up(VirtualKeyCode.PrintScreen);
        Assert.True(handler.Handle(up));
        Assert.False(up.Handled);
    }

    [Fact]
    public void AllKeysUp_IgnoresPassThroughFalse_NeverHandled()
    {
        var handler = new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyA)
        {
            TriggerMode = TriggerMode.AllKeysUp,
            IsPassThrough = false,
            CanRepeat = true
        };
        var events = new[] { Down(VirtualKeyCode.LeftControl), Down(VirtualKeyCode.KeyA), Down(VirtualKeyCode.KeyA), Up(VirtualKeyCode.KeyA), Up(VirtualKeyCode.LeftControl) };
        var results = events.Select(args => handler.Handle(args)).ToArray();
        Assert.Equal(new[] { false, false, false, false, true }, results);
        Assert.All(events, args => Assert.False(args.Handled));
    }

    [Fact]
    public void AllKeysUp_InKeySequence()
    {
        var handler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyK) { TriggerMode = TriggerMode.AllKeysUp },
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyC) { TriggerMode = TriggerMode.AllKeysUp })
        {
            Timeout = null
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.KeyC)));

        // The sequence starts over
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_InKeySequence_WrongSecondStageResets()
    {
        var handler = new KeySequenceHandler(
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyK) { TriggerMode = TriggerMode.AllKeysUp },
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyC) { TriggerMode = TriggerMode.AllKeysUp })
        {
            Timeout = null
        };

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyK)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        // Ctrl+X instead of Ctrl+C
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyX)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyX)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));

        // Ctrl+C alone is not the sequence
        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyC)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.LeftControl)));
    }

    [Fact]
    public void AllKeysUp_InKeyOrCombination()
    {
        var handler = new KeyOrCombinationHandler(
            new KeyCombinationHandler(VirtualKeyCode.Control, VirtualKeyCode.KeyD) { TriggerMode = TriggerMode.AllKeysUp },
            new KeyCombinationHandler(VirtualKeyCode.PrintScreen));

        Assert.False(handler.Handle(Down(VirtualKeyCode.LeftControl)));
        Assert.False(handler.Handle(Down(VirtualKeyCode.KeyD)));
        Assert.False(handler.Handle(Up(VirtualKeyCode.KeyD)));
        Assert.True(handler.Handle(Up(VirtualKeyCode.LeftControl)));
        Assert.False(handler.HasKeysPressed);

        var printDown = Down(VirtualKeyCode.PrintScreen);
        Assert.True(handler.Handle(printDown));
        Assert.True(printDown.Handled);
        Assert.False(handler.Handle(Up(VirtualKeyCode.PrintScreen)));
    }

    [Fact]
    public void TriggerMode_DefaultIsKeyDown()
    {
        Assert.Equal(TriggerMode.KeyDown, new KeyCombinationHandler(VirtualKeyCode.KeyA).TriggerMode);
    }

    [Fact]
    public void KeyboardState_Arguments()
    {
        Assert.Throws<ArgumentNullException>(() => KeyboardState.IsAnyDown(null));
        Assert.False(KeyboardState.IsAnyDown());
        // No key has the code 0
        Assert.False(KeyboardState.IsDown(VirtualKeyCode.None));
        Assert.False(KeyboardState.IsDownForCurrentThread(VirtualKeyCode.None));
        Assert.False(KeyboardState.IsToggled(VirtualKeyCode.None));
    }
}

/// <remarks>Interactive: these tests change the real desktop (input, clipboard or registry). They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class FcFeatureInteractiveTests
{
    /// <summary>
    /// Type text into a TextBox of a form owned by the test, the form needs the keyboard focus
    /// </summary>
    [StaFact]
    public void TypeText_IntoTextBox()
    {
        const string text = "Hé €\U0001F44D\r\nx\ty\b!";
        // The backspace is skipped, the TextBox turns the Enter into \r\n and inserts the tab
        const string expected = "Hé €\U0001F44D\r\nx\ty!";

        var packets = new ConcurrentQueue<char>();
        using var form = new System.Windows.Forms.Form
        {
            Text = nameof(TypeText_IntoTextBox),
            TopMost = true,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        };
        var textBox = new System.Windows.Forms.TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            Dock = System.Windows.Forms.DockStyle.Fill
        };
        form.Controls.Add(textBox);
        form.Show();
        // A background process may not take the foreground itself (foreground lock), ToForegroundAsync works around that.
        // It continues on the thread pool, so keep pumping this thread's messages until it's done instead of blocking.
        var toForeground = InteropWindowFactory.CreateFor(form.Handle).ToForegroundAsync().AsTask();
        PumpUntil(() => toForeground.IsCompleted, "ToForegroundAsync didn't finish");
        toForeground.GetAwaiter().GetResult();
        form.Activate();
        textBox.Focus();

        PumpUntil(() => textBox.Focused && System.Windows.Forms.Form.ActiveForm == form, "The test form didn't get the keyboard focus");

        using (KeyboardHook.KeyboardEventsNonBlocking.Where(args => args.IsPacket && args.IsKeyDown).Subscribe(args => packets.Enqueue(args.PacketCharacter)))
        {
            // Wait until no key is held anymore, keys of the user would be combined with the input
            PumpUntil(() => !KeyboardState.IsAnyDown(VirtualKeyCode.Shift, VirtualKeyCode.Control, VirtualKeyCode.Menu, VirtualKeyCode.Win), "Modifier keys are held down");

            var expectedEvents = (uint)KeyboardInput.ForText(text).Length;
            Assert.Equal(expectedEvents, KeyboardInputGenerator.TypeText(text));

            PumpUntil(() => textBox.Text == expected, $"The TextBox didn't get the text, it has: \"{textBox.Text}\"");
            // The hook sees every UTF-16 code unit as a packet, the thumbs up as two surrogates
            PumpUntil(() => packets.Contains('é') && packets.Contains('\uD83D') && packets.Contains('\uDC4D'), "The keyboard hook didn't see the VK_PACKET events");
        }
    }

    /// <summary>
    /// Wait for the condition and keep the message loop of the STA thread running meanwhile.
    /// This is synchronous on purpose: creating the form installs the WindowsFormsSynchronizationContext on this thread,
    /// an await would post its continuation to that context, which is never pumped by the test runner (the test hangs).
    /// </summary>
    private static void PumpUntil(Func<bool> condition, string message)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > TestWait.DefaultTimeout)
            {
                Assert.Fail(message);
            }
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(10);
        }
    }
}
