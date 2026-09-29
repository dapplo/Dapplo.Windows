// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
/// This is an IKeyboardHookEventHandler which can handle a combination of VirtualKeyCode presses.
/// </summary>
public class KeyCombinationHandler : IKeyboardHookEventHandler
{
    /// <summary>
    /// The keys that do not makeup the combination, where there are any Handle cannot return true
    /// </summary>
    protected ISet<VirtualKeyCode> OtherPressedKeys { get; } = new HashSet<VirtualKeyCode>();

    /// <summary>
    /// An array with all the current available keys, the locations represent the TriggerCombination array.
    /// </summary>
    protected bool[] AvailableKeys;

    /// <summary>
    /// The actual keys which are pressed for the entries in AvailableKeys, e.g. LeftShift for a Shift in the TriggerCombination.
    /// </summary>
    private VirtualKeyCode[] _pressedKeys;

    /// <summary>
    /// Get the VirtualKeyCodes which trigger the combination
    /// </summary>
    public VirtualKeyCode[] TriggerCombination {get; protected set; }

    /// <summary>
    /// Defines if repeats are allowed, default is false
    /// </summary>
    public bool CanRepeat { get; set; } = false;

    /// <summary>
    /// Defines if generated (injected) key presses need to be ignored,
    /// By default (true) only "real" key presses are handled
    /// </summary>
    public bool IgnoreInjected { get; set; } = true;

    /// <summary>
    /// Defines if the key press needs to be passed through to other applications.
    /// By default (false) a keypress which is specified is marked as handled and will not be seen by others
    /// This has no effect when <see cref="TriggerOnKeyUp"/> is true, as the key-downs were already passed to other applications.
    /// </summary>
    public bool IsPassThrough { get; set; }

    /// <summary>
    /// Defines if the handler should trigger on the release of the combination instead of when all keys are pressed.
    /// By default (false) the handler triggers when all keys in the combination are down.
    /// When true, the handler triggers when the first key of the combination is released (key up),
    /// but only if all keys were previously pressed together and no other keys are pressed.
    /// Note that the other keys of the combination may still be down at that moment.
    /// In this mode the key events are never marked as handled, the key-downs were already seen by other applications
    /// and swallowing the key-up would leave the key stuck.
    /// </summary>
    public bool TriggerOnKeyUp { get; set; }

    /// <summary>
    /// Used to verify, on every key-down, that the keys which this handler considers pressed are really still down, keys which are reported as up are forgotten.
    /// This recovers from missed key-ups, which would otherwise block the combination until the key is pressed and released again:
    /// a key released on the secure desktop (Win+L, UAC, Ctrl+Alt+Del), a key-up swallowed by another hook, or the hook being removed temporarily by Windows.
    /// When null (the default) the physical key state (GetAsyncKeyState) is used, but only for events which come from the <see cref="KeyboardHook"/>
    /// (<see cref="KeyboardHookEventArgs.IsFromKeyboardHook"/>), synthetic events are not verified.
    /// When set, the function is used for all events, return true for keys which are pressed. Use <c>_ => true</c> to disable the verification.
    /// </summary>
    public Func<VirtualKeyCode, bool> KeyStateVerifier { get; set; }

    /// <summary>
    /// Create a KeyCombinationHandler for the specified VirtualKeyCodes
    /// </summary>
    /// <param name="keyCombination">IEnumerable with VirtualKeyCodes</param>
    public KeyCombinationHandler(IEnumerable<VirtualKeyCode> keyCombination)
    {
        Configure(keyCombination);
    }

    /// <summary>
    /// Configure the key combinations
    /// </summary>
    /// <param name="keyCombination">IEnumerable of VirtualKeyCode</param>
    public void Configure(IEnumerable<VirtualKeyCode> keyCombination)
    {
        TriggerCombination = keyCombination.Distinct().ToArray();
        AvailableKeys = new bool[TriggerCombination.Length];
        _pressedKeys = new VirtualKeyCode[TriggerCombination.Length];
        OtherPressedKeys.Clear();
    }

    /// <summary>
    /// Create a KeyCombinationHandler for the specified VirtualKeyCodes
    /// </summary>
    /// <param name="keyCombination">params with VirtualKeyCodes</param>
    public KeyCombinationHandler(params VirtualKeyCode[] keyCombination)
    {
        Configure(keyCombination);
    }

    /// <summary>
    /// Handle key presses to test if the combination is available
    /// </summary>
    /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
    public virtual bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
    {
        if (IgnoreInjected && keyboardHookEventArgs.IsInjectedByProcess)
        {
            return false;
        }

        if (keyboardHookEventArgs.IsKeyDown)
        {
            ForgetReleasedKeys(keyboardHookEventArgs);
        }

        bool keyMatched = false;
        bool isRepeat = false;
        bool wasAllKeysDown = AvailableKeys.All(b => b);
        
        for (int i = 0; i < TriggerCombination.Length; i++)
        {
            if (!CompareVirtualKeyCode(keyboardHookEventArgs.Key, TriggerCombination[i]))
            {
                continue;
            }

            // Only a key-down for a key which is already down is a repeat, a key-up never is
            isRepeat = keyboardHookEventArgs.IsKeyDown && AvailableKeys[i];
            AvailableKeys[i] = keyboardHookEventArgs.IsKeyDown;
            _pressedKeys[i] = keyboardHookEventArgs.IsKeyDown ? keyboardHookEventArgs.Key : VirtualKeyCode.None;
            keyMatched = true;
            break;
        }

        if (!keyMatched)
        {
            if (keyboardHookEventArgs.IsKeyDown)
            {
                OtherPressedKeys.Add(keyboardHookEventArgs.Key);
            }
            else
            {
                OtherPressedKeys.Remove(keyboardHookEventArgs.Key);
            }
        }

        bool isHandled;
        if (TriggerOnKeyUp)
        {
            // Trigger when a key is released, but only if all keys were previously down
            // and no other keys are pressed
            isHandled = !keyboardHookEventArgs.IsKeyDown && keyMatched && wasAllKeysDown && OtherPressedKeys.Count == 0;
        }
        else
        {
            // Original behavior: trigger when all keys are down
            isHandled = keyboardHookEventArgs.IsKeyDown && OtherPressedKeys.Count == 0 && AvailableKeys.All(b => b);
        }

        // Mark as handled if the key combination is handled and we don't have pass-through.
        // A repeated key-down is also marked as handled (but doesn't trigger), as the original key-down was swallowed too.
        // In TriggerOnKeyUp mode the key-downs were passed through, swallowing only the key-up would leave a stuck key.
        if (isHandled && !IsPassThrough && !TriggerOnKeyUp)
        {
            keyboardHookEventArgs.Handled = true;
        }

        // Do not return a true, if this is a repeat and CanRepeat is disabled
        if (!CanRepeat && isRepeat)
        {
            return false;
        }
        return isHandled;
    }

    /// <inheritdoc />
    public bool HasKeysPressed => OtherPressedKeys.Count > 0 || AvailableKeys.Any(b => b);

    /// <summary>
    /// Forget the keys which are considered pressed, but are not down anymore according to the <see cref="KeyStateVerifier"/> (or the physical key state).
    /// The key of the current event is never checked, its state is not yet updated when a low-level hook is called.
    /// </summary>
    /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs of the current key-down</param>
    private void ForgetReleasedKeys(KeyboardHookEventArgs keyboardHookEventArgs)
    {
        var isKeyPressed = KeyStateVerifier;
        if (isKeyPressed == null)
        {
            if (!keyboardHookEventArgs.IsFromKeyboardHook)
            {
                return;
            }
            isKeyPressed = IsPhysicallyPressed;
        }

        var currentKey = keyboardHookEventArgs.Key;
        if (OtherPressedKeys.Count > 0)
        {
            foreach (var otherKey in OtherPressedKeys.ToList())
            {
                if (otherKey != currentKey && !isKeyPressed(otherKey))
                {
                    OtherPressedKeys.Remove(otherKey);
                }
            }
        }

        for (int i = 0; i < AvailableKeys.Length; i++)
        {
            if (!AvailableKeys[i])
            {
                continue;
            }
            var pressedKey = _pressedKeys[i] == VirtualKeyCode.None ? TriggerCombination[i] : _pressedKeys[i];
            if (pressedKey != currentKey && !isKeyPressed(pressedKey))
            {
                AvailableKeys[i] = false;
                _pressedKeys[i] = VirtualKeyCode.None;
            }
        }
    }

    /// <summary>
    /// Check the physical state of the key
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool true if the key is down</returns>
    private static bool IsPhysicallyPressed(VirtualKeyCode virtualKeyCode)
    {
        if (virtualKeyCode == VirtualKeyCode.Win)
        {
            return IsPhysicallyPressed(VirtualKeyCode.LeftWin) || IsPhysicallyPressed(VirtualKeyCode.RightWin);
        }
        return (KeyboardHook.GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;
    }

    /// <summary>
    /// Helper method to compare VirtualKeyCode
    /// </summary>
    /// <param name="current">VirtualKeyCode</param>
    /// <param name="expected">VirtualKeyCode, a generic Shift, Control, Menu or Win matches the left and right variant</param>
    /// <returns>bool true if match</returns>
    protected virtual bool CompareVirtualKeyCode(VirtualKeyCode current, VirtualKeyCode expected)
    {
        return current.Matches(expected);
    }
}