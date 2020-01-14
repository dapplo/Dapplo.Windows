// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace Dapplo.Windows.Messages
{
    /// <summary>
    ///     A monitor for window messages
    /// </summary>
    public static class WinProcFormsExtensions
    {
        /// <summary>
        ///     Create an observable for the specified Control (Form)
        /// </summary>
        public static IObservable<WindowMessageInfo> WinProcFormsMessages(this Control control)
        {
            var winProcListener = new WinProcListener(control);

            return Observable.Create<WindowMessageInfo>(observer =>
            {
                winProcListener.AddHook(WindowMessageHandler);
                // This handles the message, and generates the observable OnNext
                IntPtr WindowMessageHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var message = WindowMessageInfo.Create(hWnd, msg, wParam, lParam);
                    observer.OnNext(message);
                    // ReSharper disable once AccessToDisposedClosure
                    if (winProcListener.IsDisposed || message.Message == WindowsMessages.WM_DESTROY)
                    {
                        observer.OnCompleted();
                    }
                    return IntPtr.Zero;
                }

                return Disposable.Create(() =>
                {
                    winProcListener.Dispose();
                });
            })
            // Make sure there is always a value produced when connecting
            .Publish()
            .RefCount();
        }
    }
}
#endif