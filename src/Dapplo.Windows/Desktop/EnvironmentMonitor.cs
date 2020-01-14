// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32.Enums;

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
                    IntPtr WinProcSettingsChangeHandler(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
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

                    return WinProcHandler.Instance.Subscribe(new WinProcHandlerHook(WinProcSettingsChangeHandler));
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