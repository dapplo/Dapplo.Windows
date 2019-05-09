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

#if !NETSTANDARD2_0
#region using

using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     A monitor for environment changes
    /// </summary>
    public class EnvironmentMonitor
    {
        /// <summary>
        ///     The singleton of the KeyboardHook
        /// </summary>
        private static readonly Lazy<EnvironmentMonitor> Singleton = new Lazy<EnvironmentMonitor>(() => new EnvironmentMonitor());

        /// <summary>
        ///     Used to store the observable
        /// </summary>
        private readonly IObservable<EnvironmentChangedEventArgs> _environmentObservable;

        /// <summary>
        ///     Private constructor to create the observable
        /// </summary>
        private EnvironmentMonitor()
        {
            _environmentObservable = Observable.Create<EnvironmentChangedEventArgs>(observer =>
                {
                    // This handles the message
                    IntPtr WinProcSettingsChangeHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        var windowsMessage = (WindowsMessages) msg;
                        if (windowsMessage != WindowsMessages.WM_SETTINGCHANGE)
                        {
                            return IntPtr.Zero;
                        }

                        var action = (SystemParametersInfoActions) wParam.ToInt32();
                        var area = Marshal.PtrToStringAuto(lParam);
                        observer.OnNext(EnvironmentChangedEventArgs.Create(action, area));
                        return IntPtr.Zero;
                    }

                    return WinProcHandler.Instance.Subscribe(WinProcSettingsChangeHandler);
                })
                .Publish()
                .RefCount();
        }


        /// <summary>
        ///     The actual clipboard hook observable
        /// </summary>
        public static IObservable<EnvironmentChangedEventArgs> EnvironmentUpdateEvents => Singleton.Value._environmentObservable;
    }
}
#endif