// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
///     A global (low-level) keyboard hook, using System.Reactive
/// </summary>
/// <remarks>
/// The hook is installed when the first subscriber subscribes and removed when the last one unsubscribes.
/// It runs on a dedicated background thread with its own message loop, it doesn't matter which thread subscribes.
/// <para>
/// Subscribers of <see cref="KeyboardEvents"/> are called synchronously inside the hook callback, on the hook thread, and every keyboard event of the whole system waits for them.
/// Windows silently removes a low-level hook which takes longer than the LowLevelHooksTimeout (at most 1 second, often less), after that no events arrive anymore.
/// So only decide <see cref="KeyboardHookEventArgs.Handled"/> in there (e.g. with an <see cref="IKeyboardHookEventHandler"/>) and keep it quick,
/// move any other work away from the hook thread with ObserveOn, or use <see cref="KeyboardEventsNonBlocking"/>.
/// Anything which touches the UI needs to be marshalled to the UI thread.
/// </para>
/// <para>
/// Unicode characters which are sent with <see cref="KeyboardInputGenerator.TypeText"/> (KEYEVENTF_UNICODE), by an IME or a remote desktop client, arrive as
/// <see cref="VirtualKeyCode.Packet"/> (VK_PACKET) events, see <see cref="KeyboardHookEventArgs.IsPacket"/> and <see cref="KeyboardHookEventArgs.PacketCharacter"/>.
/// The modifier state of such an event reflects the keys the user holds, not the character.
/// </para>
/// <para>
/// Exceptions thrown by subscribers never leave the hook callback, they are published on <see cref="SubscriberErrors"/>.
/// When the hook cannot be installed, the subscriber gets OnError with a <see cref="System.ComponentModel.Win32Exception"/>.
/// </para>
/// </remarks>
public static class KeyboardHook
{
    private const long WmKeyDown = 0x0100;
    private const long WmSysKeyDown = 0x0104;
    private const long WmSysKeyUp = 0x0105;

    private static readonly LowLevelHook<KeyboardHookEventArgs> Hook = new(HookTypes.WH_KEYBOARD_LL, "Dapplo.Windows.Input.KeyboardHook", CreateKeyboardEventArgs, eventArgs => eventArgs.Handled);

    // Only used on the hook thread: the previous down state of the toggle keys, to ignore auto-repeat
    private static bool _isCapsLockDown;
    private static bool _isNumLockDown;
    private static bool _isScrollLockDown;

    /// <summary>
    ///     The keyboard events, OnNext is called synchronously on the hook thread.
    ///     Setting <see cref="KeyboardHookEventArgs.Handled"/> to true in OnNext swallows the key event, keep the processing short.
    /// </summary>
    public static IObservable<KeyboardHookEventArgs> KeyboardEvents => Hook.Events;

    /// <summary>
    ///     The keyboard events, delivered in order on a separate background thread so slow subscribers never delay the keyboard input of the system.
    ///     Setting <see cref="KeyboardHookEventArgs.Handled"/> has no effect here, the event was already passed on.
    /// </summary>
    public static IObservable<KeyboardHookEventArgs> KeyboardEventsNonBlocking => Hook.NonBlockingEvents;

    /// <summary>
    ///     Exceptions thrown by subscribers of <see cref="KeyboardEvents"/> or <see cref="KeyboardEventsNonBlocking"/>, these are also written to System.Diagnostics.Trace.
    /// </summary>
    public static IObservable<Exception> SubscriberErrors => Hook.SubscriberErrors;

    /// <summary>
    ///     Create the KeyboardHookEventArgs from the parameters which where in the event
    /// </summary>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <returns>KeyboardHookEventArgs</returns>
    private static KeyboardHookEventArgs CreateKeyboardEventArgs(IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt64();
        var isKeyDown = message is WmKeyDown or WmSysKeyDown;
        var isSystemKey = message is WmSysKeyDown or WmSysKeyUp;
        var keyboardLowLevelHookStruct = Marshal.PtrToStructure<KeyboardLowLevelHookStruct>(lParam);
        var key = keyboardLowLevelHookStruct.VirtualKeyCode;

        // Query the current state of modifiers
        var leftShift = KeyboardState.IsDown(VirtualKeyCode.LeftShift);
        var rightShift = KeyboardState.IsDown(VirtualKeyCode.RightShift);
        var leftCtrl = KeyboardState.IsDown(VirtualKeyCode.LeftControl);
        var rightCtrl = KeyboardState.IsDown(VirtualKeyCode.RightControl);
        var leftAlt = KeyboardState.IsDown(VirtualKeyCode.LeftMenu);
        var rightAlt = KeyboardState.IsDown(VirtualKeyCode.RightMenu);
        var leftWin = KeyboardState.IsDown(VirtualKeyCode.LeftWin);
        var rightWin = KeyboardState.IsDown(VirtualKeyCode.RightWin);

        // Query the current state of lock keys
        var capsLock = KeyboardState.IsToggled(VirtualKeyCode.Capital);
        var numLock = KeyboardState.IsToggled(VirtualKeyCode.NumLock);
        var scrollLock = KeyboardState.IsToggled(VirtualKeyCode.Scroll);

        // Override the state for the current key to ensure accuracy, the OS state isn't updated yet when the hook is called
        switch (key)
        {
            case VirtualKeyCode.LeftShift:
                leftShift = isKeyDown;
                break;
            case VirtualKeyCode.RightShift:
                rightShift = isKeyDown;
                break;
            case VirtualKeyCode.LeftControl:
                leftCtrl = isKeyDown;
                break;
            case VirtualKeyCode.RightControl:
                rightCtrl = isKeyDown;
                break;
            case VirtualKeyCode.LeftMenu:
                leftAlt = isKeyDown;
                break;
            case VirtualKeyCode.RightMenu:
                rightAlt = isKeyDown;
                break;
            case VirtualKeyCode.LeftWin:
                leftWin = isKeyDown;
                break;
            case VirtualKeyCode.RightWin:
                rightWin = isKeyDown;
                break;
            case VirtualKeyCode.Capital:
                // The toggle only flips on the transition from up to down, not on auto-repeat
                if (isKeyDown && !_isCapsLockDown) capsLock = !capsLock;
                _isCapsLockDown = isKeyDown;
                break;
            case VirtualKeyCode.NumLock:
                if (isKeyDown && !_isNumLockDown) numLock = !numLock;
                _isNumLockDown = isKeyDown;
                break;
            case VirtualKeyCode.Scroll:
                if (isKeyDown && !_isScrollLockDown) scrollLock = !scrollLock;
                _isScrollLockDown = isKeyDown;
                break;
        }

        return new KeyboardHookEventArgs
        {
            TimeStamp = keyboardLowLevelHookStruct.TimeStamp,
            Key = key,
            ScanCode = keyboardLowLevelHookStruct.ScanCode,
            Flags = keyboardLowLevelHookStruct.Flags,
            IsFromKeyboardHook = true,
            IsKeyDown = isKeyDown,
            // WM_SYSKEYDOWN / WM_SYSKEYUP: F10, or a key while Alt is down. Use Flags (AltDown) for the Alt context, this doesn't change the Alt state.
            IsSystemKey = isSystemKey,
            IsLeftShift = leftShift,
            IsRightShift = rightShift,
            IsLeftAlt = leftAlt,
            IsRightAlt = rightAlt,
            IsLeftControl = leftCtrl,
            IsRightControl = rightCtrl,
            IsLeftWindows = leftWin,
            IsRightWindows = rightWin,
            IsScrollLockActive = scrollLock,
            IsNumLockActive = numLock,
            IsCapsLockActive = capsLock
        };
    }
}
