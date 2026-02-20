// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages;

#if !NETSTANDARD2_0
using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
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
            _environmentObservable = SharedMessageWindow.Messages
                .Where(m => m.Msg == (uint)WindowsMessages.WM_SETTINGCHANGE)
                .Select(m =>
                {
                    var action = (SystemParametersInfoActions)(int)m.WParam;
                    var area = Marshal.PtrToStringAuto((IntPtr)m.LParam);
                    return EnvironmentChangedEventArgs.Create(action, area);
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