// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Forms.Messages
{
    /// <summary>
    ///     A monitor for window messages
    /// </summary>
    public static class WinProcFormsExtensions
    {
        /// <summary>
        ///     Create an observable for the messages of the specified Control (Form).
        ///     Every subscription subclasses the control's window, and follows it when the handle is recreated.
        ///     The sequence completes when the control is disposed.
        /// </summary>
        /// <remarks>
        ///     Setting <see cref="WindowMessageInfo.Handled"/> and <see cref="WindowMessageInfo.Result"/> synchronously in OnNext (on the UI thread)
        ///     returns the result to Windows instead of calling the original window procedure.
        /// </remarks>
        public static IObservable<WindowMessageInfo> WinProcFormsMessages(this Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return Observable.Create<WindowMessageInfo>(observer =>
            {
                var winProcListener = new WinProcListener(control);

                // This handles the message, and generates the observable OnNext
                IntPtr WindowMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var message = WindowMessageInfo.Create(hWnd, msg, wParam, lParam);
                    observer.OnNext(message);
                    handled = message.Handled;
                    return message.Result;
                }

                void ControlDisposed(object sender, EventArgs e)
                {
                    observer.OnCompleted();
                }

                winProcListener.AddHook(WindowMessageHandler);
                control.Disposed += ControlDisposed;

                return Disposable.Create(() =>
                {
                    control.Disposed -= ControlDisposed;
                    winProcListener.Dispose();
                });
            });
        }
    }
}
