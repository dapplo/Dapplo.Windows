// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
///     A global keyboard hook, using System.Reactive
/// </summary>
public sealed class KeyboardHook
{
    /// <summary>
    ///     The singleton of the KeyboardHook
    /// </summary>
    private static readonly Lazy<KeyboardHook> Singleton = new Lazy<KeyboardHook>(() => new KeyboardHook());

    /// <summary>
    ///     Used to store the observable
    /// </summary>
    private readonly IObservable<KeyboardHookEventArgs> _keyObservable;

    /// <summary>
    ///     Store the handler, otherwise it might be GCed
    /// </summary>
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private LowLevelKeyboardProc _callback;

    /// <summary>
    ///     Private constructor to create the observable
    /// </summary>
    private KeyboardHook()
    {
        _keyObservable = Observable.Create<KeyboardHookEventArgs>(observer =>
            {
                var hookId = IntPtr.Zero;
                // Need to hold onto this callback, otherwise it will get garbage collected as it is an un-manged callback
                _callback = (nCode, wParam, lParam) =>
                {
                    if (nCode >= 0)
                    {
                        var eventArgs = CreateKeyboardEventArgs(wParam, lParam);
                        observer.OnNext(eventArgs);
                        if (eventArgs.Handled)
                        {
                            return (IntPtr) 1;
                        }
                    }
                    // ReSharper disable once AccessToModifiedClosure
                    return CallNextHookEx(hookId, nCode, wParam, lParam);
                };

                hookId = SetWindowsHookEx(HookTypes.WH_KEYBOARD_LL, _callback, IntPtr.Zero, 0);

                return Disposable.Create(() =>
                {
                    UnhookWindowsHookEx(hookId);
                    _callback = null;
                });
            })
            .Publish()
            .RefCount();
    }

    /// <summary>
    ///     The actual keyboard hook observable
    /// </summary>
    public static IObservable<KeyboardHookEventArgs> KeyboardEvents => Singleton.Value._keyObservable;

    /// <summary>
    ///     Create the KeyboardHookEventArgs from the parameters which where in the event
    /// </summary>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <returns>KeyboardHookEventArgs</returns>
    private static KeyboardHookEventArgs CreateKeyboardEventArgs(IntPtr wParam, IntPtr lParam)
    {
        var isKeyDown = wParam == (IntPtr) WmKeyDown || wParam == (IntPtr) WmSysKeyDown;
        var keyboardLowLevelHookStruct = (KeyboardLowLevelHookStruct) Marshal.PtrToStructure(lParam, typeof(KeyboardLowLevelHookStruct));
        var key = keyboardLowLevelHookStruct.VirtualKeyCode;

        // Query the current state of modifiers
        var leftShift = (GetAsyncKeyState(VirtualKeyCode.LeftShift) & 0x8000) != 0;
        var rightShift = (GetAsyncKeyState(VirtualKeyCode.RightShift) & 0x8000) != 0;
        var leftCtrl = (GetAsyncKeyState(VirtualKeyCode.LeftControl) & 0x8000) != 0;
        var rightCtrl = (GetAsyncKeyState(VirtualKeyCode.RightControl) & 0x8000) != 0;
        var leftAlt = (GetAsyncKeyState(VirtualKeyCode.LeftMenu) & 0x8000) != 0;
        var rightAlt = (GetAsyncKeyState(VirtualKeyCode.RightMenu) & 0x8000) != 0;
        var leftWin = (GetAsyncKeyState(VirtualKeyCode.LeftWin) & 0x8000) != 0;
        var rightWin = (GetAsyncKeyState(VirtualKeyCode.RightWin) & 0x8000) != 0;

        // Query the current state of lock keys
        var capsLock = (GetKeyState(VirtualKeyCode.Capital) & 1) != 0;
        var numLock = (GetKeyState(VirtualKeyCode.NumLock) & 1) != 0;
        var scrollLock = (GetKeyState(VirtualKeyCode.Scroll) & 1) != 0;

        // Override the state for the current key to ensure accuracy even if the OS state hasn't updated yet
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
                if (isKeyDown) capsLock = !capsLock;
                break;
            case VirtualKeyCode.NumLock:
                if (isKeyDown) numLock = !numLock;
                break;
            case VirtualKeyCode.Scroll:
                if (isKeyDown) scrollLock = !scrollLock;
                break;
        }

        var keyEventArgs = new KeyboardHookEventArgs
        {
            TimeStamp = keyboardLowLevelHookStruct.TimeStamp,
            Key = key,
            Flags = keyboardLowLevelHookStruct.Flags,
            IsModifier = key.IsModifier(),
            IsKeyDown = isKeyDown,
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

        // Handle system keys (Alt combinations)
        if (!keyEventArgs.IsAlt && (wParam == (IntPtr) WmSysKeyDown || wParam == (IntPtr) WmSysKeyUp))
        {
            keyEventArgs.IsLeftAlt = true;
            keyEventArgs.IsSystemKey = true;
        }
        return keyEventArgs;
    }

    private const int WmKeyDown = 256;
    private const int WmSysKeyUp = 261;
    private const int WmSysKeyDown = 260;

    /// <summary>
    ///     Retrieve the state of a key (async)
    /// </summary>
    /// <param name="keyCode"></param>
    /// <returns></returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetAsyncKeyState(VirtualKeyCode keyCode);

    /// <summary>
    ///     Retrieve the state of a key
    /// </summary>
    /// <param name="keyCode"></param>
    /// <returns></returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    [ResourceExposure(ResourceScope.None)]
    private static extern short GetKeyState(VirtualKeyCode keyCode);

    /// <summary>
    ///     The actual delegate for the p
    /// </summary>
    /// <param name="nCode"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     Register a windows hook
    /// </summary>
    /// <param name="hookType">HookTypes</param>
    /// <param name="lowLevelKeyboardProc">LowLevelKeyboardProc</param>
    /// <param name="hMod">IntPtr</param>
    /// <param name="dwThreadId">uint</param>
    /// <returns>ID to be able to unhook it again</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(HookTypes hookType, LowLevelKeyboardProc lowLevelKeyboardProc, IntPtr hMod, uint dwThreadId);

    /// <summary>
    ///     Used to remove a hook which was set with SetWindowsHookEx
    /// </summary>
    /// <param name="hhk"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    ///     Used to call the next hook in the list, if there was any
    /// </summary>
    /// <param name="hhk"></param>
    /// <param name="nCode"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns>IntPtr</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
}
