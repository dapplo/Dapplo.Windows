//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019 Dapplo
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

#if !NETSTANDARD2_0
#region using

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

#endregion

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
                IntPtr WindowMessageHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                {
                    var message = WindowMessageInfo.Create(hwnd, msg, wParam, lParam);
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