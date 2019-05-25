//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

#endregion

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    ///     A glocal keyboard hook, using System.Reactive
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
                    // Make sure the current state of the lock keys is retrieved
                    SyncLockState();

                    var hookId = IntPtr.Zero;
                    // Need to hold onto this callback, otherwise it will get GC'd as it is an unmanged callback
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
            var isKeyDown = wParam == (IntPtr) WmKeydown || wParam == (IntPtr) WmSyskeydown;

            var keyboardLowLevelHookStruct = (KeyboardLowLevelHookStruct) Marshal.PtrToStructure(lParam, typeof(KeyboardLowLevelHookStruct));
            bool isModifier = keyboardLowLevelHookStruct.VirtualKeyCode.IsModifier();

            // Check the key to find if there any modifiers, store these in the global values.
            switch (keyboardLowLevelHookStruct.VirtualKeyCode)
            {
                case VirtualKeyCode.Capital:
                    if (isKeyDown)
                    {
                        _capsLock = !_capsLock;
                    }
                    break;
                case VirtualKeyCode.NumLock:
                    if (isKeyDown)
                    {
                        _numLock = !_numLock;
                    }
                    break;
                case VirtualKeyCode.Scroll:
                    if (isKeyDown)
                    {
                        _scrollLock = !_scrollLock;
                    }
                    break;
                case VirtualKeyCode.LeftShift:
                    _leftShift = isKeyDown;
                    break;
                case VirtualKeyCode.RightShift:
                    _rightShift = isKeyDown;
                    break;
                case VirtualKeyCode.LeftControl:
                    _leftCtrl = isKeyDown;
                    break;
                case VirtualKeyCode.RightControl:
                    _rightCtrl = isKeyDown;
                    break;
                case VirtualKeyCode.LeftMenu:
                    _leftAlt = isKeyDown;
                    break;
                case VirtualKeyCode.RightMenu:
                    _rightAlt = isKeyDown;
                    break;
                case VirtualKeyCode.LeftWin:
                    _leftWin = isKeyDown;
                    break;
                case VirtualKeyCode.RightWin:
                    _rightWin = isKeyDown;
                    break;
            }

            var keyEventArgs = new KeyboardHookEventArgs
            {
                TimeStamp = keyboardLowLevelHookStruct.TimeStamp,
                Key = keyboardLowLevelHookStruct.VirtualKeyCode,
                Flags = keyboardLowLevelHookStruct.Flags,
                IsModifier = isModifier,
                IsKeyDown = isKeyDown,
                IsLeftShift = _leftShift,
                IsRightShift = _rightShift,
                IsLeftAlt = _leftAlt,
                IsRightAlt = _rightAlt,
                IsLeftControl = _leftCtrl,
                IsRightControl = _rightCtrl,
                IsLeftWindows = _leftWin,
                IsRightWindows = _rightWin,
                IsScrollLockActive = _scrollLock,
                IsNumLockActive = _numLock,
                IsCapsLockActive = _capsLock
            };

            // Do we need this??
            //http://msdn.microsoft.com/en-us/library/windows/desktop/ms646286(v=vs.85).aspx
            if (!keyEventArgs.IsAlt && (wParam == (IntPtr) WmSyskeydown || wParam == (IntPtr)WmSyskeyup))
            {
                keyEventArgs.IsLeftAlt = true;
                keyEventArgs.IsSystemKey = true;
            }
            return keyEventArgs;
        }

        #region Current key states

        /// <summary>
        ///     Flags for the current state
        /// </summary>
        private static bool _leftShift;

        private static bool _rightShift;
        private static bool _leftAlt;
        private static bool _rightAlt;
        private static bool _leftCtrl;
        private static bool _rightCtrl;
        private static bool _leftWin;
        private static bool _rightWin;

        // Flags for the lock keys, initialize the locking keys state one time, these will be updated later
        private static bool _capsLock;

        private static bool _numLock;
        private static bool _scrollLock;

        /// <summary>
        ///     Sync the lock key state
        /// </summary>
        private static void SyncLockState()
        {
            _capsLock = GetKeyState(VirtualKeyCode.Capital) > 0;
            _numLock = GetKeyState(VirtualKeyCode.NumLock) > 0;
            _scrollLock = GetKeyState(VirtualKeyCode.Scroll) > 0;
        }

        #endregion

        #region Native

        private const int WmKeydown = 256;
        private const int WmSyskeyup = 261;
        private const int WmSyskeydown = 260;

        /// <summary>
        ///     Retrieve the state of a key
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        private static extern ushort GetKeyState(VirtualKeyCode keyCode);

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
        /// <param name="lpfn">LowLevelKeyboardProc</param>
        /// <param name="hMod">IntPtr</param>
        /// <param name="dwThreadId">uint</param>
        /// <returns>ID to be able to unhook it again</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(HookTypes hookType, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

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

        #endregion
    }
}