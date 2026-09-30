// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
///     This is a utility class to help to generate input for mouse and keyboard
/// </summary>
public static class KeyboardInputGenerator
{
    /// <summary>
    ///     The maximum number of input events which <see cref="TypeText"/> passes to one SendInput call.
    ///     The events of one call are inserted without other input in between, but a very large call blocks the input of the system while it is processed.
    /// </summary>
    private const int MaxInputsPerSendInput = 256;

    /// <summary>
    ///     Type the text into the application with the keyboard focus, independent of the keyboard layout: every character is sent as a Unicode character
    ///     (SendInput with KEYEVENTF_UNICODE, a key down and a key up per UTF-16 code unit, surrogate pairs as two code units in order).
    ///     Line breaks ("\r\n", "\n" or "\r") are sent as the Enter key, "\t" as the Tab key, all other control characters are skipped,
    ///     see <see cref="KeyboardInput.ForText"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keys which the user still holds are combined with the input: Shift turns the Tab into Shift+Tab, and Alt or Ctrl make many applications treat
    /// the characters as shortcuts. When the text is typed in reaction to a hotkey, use <see cref="TriggerMode.AllKeysUp"/>, or wait until the keys are released
    /// (<see cref="KeyboardState.IsAnyDown"/>).
    /// </para>
    /// <para>
    /// The text is sent in batches of SendInput calls, a surrogate pair is never split over two calls.
    /// If Windows inserts fewer events than a batch has, the rest of the text is not sent: this happens when the input is blocked (BlockInput,
    /// the secure desktop is active) or the foreground application runs with a higher integrity level (UIPI).
    /// Compare the result with the length of <see cref="KeyboardInput.ForText"/> to detect this.
    /// </para>
    /// <para>
    /// Low-level keyboard hooks see the characters as <see cref="VirtualKeyCode.Packet"/> events (<see cref="KeyboardHookEventArgs.IsPacket"/>),
    /// the Enter and Tab as normal keys. All events are marked as injected.
    /// Some applications (games, remote desktop / virtual machine windows, applications which read the keys with GetAsyncKeyState or raw input) ignore VK_PACKET input,
    /// for these use <see cref="KeyPresses"/> or the clipboard.
    /// </para>
    /// </remarks>
    /// <param name="text">string with the text to type</param>
    /// <returns>uint with the number of input events inserted, two for every typed character</returns>
    public static uint TypeText(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));

        var inputs = Structs.Input.CreateKeyboardInputs(KeyboardInput.ForText(text));
        uint inserted = 0;
        var offset = 0;
        while (offset < inputs.Length)
        {
            var count = Math.Min(MaxInputsPerSendInput, inputs.Length - offset);
            if (offset + count < inputs.Length && IsHighSurrogate(inputs[offset + count - 1]))
            {
                // Don't split a surrogate pair over two SendInput calls, the batch size is even so the key up of the high surrogate ends the batch
                count -= 2;
            }

            var batch = new Structs.Input[count];
            Array.Copy(inputs, offset, batch, 0, count);
            var batchInserted = NativeInput.SendInput(batch);
            inserted += batchInserted;
            if (batchInserted < count)
            {
                // Input is blocked, don't send the rest of the text
                break;
            }
            offset += count;
        }
        return inserted;
    }

    /// <summary>
    ///     Check if the input is a Unicode event for a high surrogate
    /// </summary>
    /// <param name="input">Input</param>
    /// <returns>bool</returns>
    private static bool IsHighSurrogate(Structs.Input input)
    {
        var keyboardInput = input.InputUnion.KeyboardInput;
        return (keyboardInput.KeyEventFlags & KeyEventFlags.Unicode) != 0 && char.IsHighSurrogate(unchecked((char)(ushort)keyboardInput.ScanCode));
    }

    /// <summary>
    ///     Generate key down
    /// </summary>
    /// <param name="keycodes">VirtualKeyCodes for the key downs</param>
    /// <returns>number of input events generated</returns>
    public static uint KeyDown(params VirtualKeyCode[] keycodes)
    {
        var keyboardInputs = new KeyboardInput[keycodes.Length];
        var index = 0;
        foreach (var virtualKeyCode in keycodes)
        {
            keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
        }
        return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
    }

    /// <summary>
    ///     Generate a key combination press(es)
    /// </summary>
    /// <param name="keycodes">params VirtualKeyCodes</param>
    public static uint KeyCombinationPress(params VirtualKeyCode[] keycodes)
    {
        var keyboardInputs = new KeyboardInput[keycodes.Length * 2];
        var index = 0;
        // all down
        foreach (var virtualKeyCode in keycodes)
        {
            keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
        }
        // all up, in reverse order
        for (var i = keycodes.Length - 1; i >= 0; i--)
        {
            keyboardInputs[index++] = KeyboardInput.ForKeyUp(keycodes[i]);
        }

        return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
    }

    /// <summary>
    ///     Generate key press(es)
    /// </summary>
    /// <param name="keycodes">params VirtualKeyCodes</param>
    public static uint KeyPresses(params VirtualKeyCode[] keycodes)
    {
        var keyboardInputs = new KeyboardInput[keycodes.Length * 2];
        var index = 0;
        foreach (var virtualKeyCode in keycodes)
        {
            keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
            keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
        }

        return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
    }

    /// <summary>
    ///     Generate key(s) up
    /// </summary>
    /// <param name="keycodes">VirtualKeyCodes for the keys to release</param>
    /// <returns>number of input events generated</returns>
    public static uint KeyUp(params VirtualKeyCode[] keycodes)
    {
        var keyboardInputs = new KeyboardInput[keycodes.Length];
        var index = 0;
        foreach (var virtualKeyCode in keycodes)
        {
            keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
        }
        return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
    }
}