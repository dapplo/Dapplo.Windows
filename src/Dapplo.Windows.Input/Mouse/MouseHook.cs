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
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages;

#endregion

namespace Dapplo.Windows.Input.Mouse
{
    /// <summary>
    ///     A glocal mouse hook, using System.Reactive
    /// </summary>
    public sealed class MouseHook
    {
        /// <summary>
        ///     The singleton of the MouseHook
        /// </summary>
        private static readonly Lazy<MouseHook> Singleton = new Lazy<MouseHook>(() => new MouseHook());

        /// <summary>
        ///     Used to store the observable
        /// </summary>
        private readonly IObservable<MouseHookEventArgs> _mouseObservable;

        /// <summary>
        ///     Store the handler, otherwise it might be GCed
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private LowLevelMouseProc _callback;

        /// <summary>
        ///     Private constructor to create the observable
        /// </summary>
        private MouseHook()
        {
            _mouseObservable = Observable.Create<MouseHookEventArgs>(observer =>
                {
                    var hookId = IntPtr.Zero;
                    // Need to hold onto this callback, otherwise it will get GC'd as it is an unmanged callback
                    _callback = (nCode, wParam, lParam) =>
                    {
                        if (nCode >= 0)
                        {
                            var eventArgs = CreateMouseEventArgs(wParam, lParam);
                            observer.OnNext(eventArgs);
                            if (eventArgs.Handled)
                            {
                                return (IntPtr) 1;
                            }
                        }

                        // ReSharper disable once AccessToModifiedClosure
                        return CallNextHookEx(hookId, nCode, wParam, lParam);
                    };

                    hookId = SetWindowsHookEx(HookTypes.WH_MOUSE_LL, _callback, IntPtr.Zero, 0);

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
        public static IObservable<MouseHookEventArgs> MouseEvents => Singleton.Value._mouseObservable;

        /// <summary>
        ///     Create the MouseEventArgs from the parameters which where in the event
        /// </summary>
        /// <param name="wParam">IntPtr</param>
        /// <param name="lParam">IntPtr</param>
        /// <returns>MouseEventArgs</returns>
        private static MouseHookEventArgs CreateMouseEventArgs(IntPtr wParam, IntPtr lParam)
        {
            var mouseLowLevelHookStruct = (MouseLowLevelHookStruct) Marshal.PtrToStructure(lParam, typeof(MouseLowLevelHookStruct));


            var mouseEventArgs = new MouseHookEventArgs
            {
                WindowsMessage = (WindowsMessages) wParam.ToInt32(),
                Point = mouseLowLevelHookStruct.pt
            };

            return mouseEventArgs;
        }

        #region Native code

        /// <summary>
        ///     The actual delegate for the p
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///     Register a windows hook
        /// </summary>
        /// <param name="hookType">HookTypes</param>
        /// <param name="lpfn">LowLevelMouseProc</param>
        /// <param name="hMod">IntPtr</param>
        /// <param name="dwThreadId">uint</param>
        /// <returns>ID to be able to unhook it again</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(HookTypes hookType, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

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