// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     A monitor for environment changes
    /// </summary>
    public class EnvironmentMonitor
    {
        /// <summary>
        ///     The singleton of the EnvironmentMonitor
        /// </summary>
        private static readonly Lazy<EnvironmentMonitor> Singleton = new(() => new EnvironmentMonitor());

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
                .Where(m => m.Msg == WindowsMessages.WM_SETTINGCHANGE)
                .Select(m =>
                {
                    // lParam is only valid while the message is processed: the string is copied here, synchronously on the window thread
                    var action = unchecked((SystemParametersInfoActions)(int)m.WParam);
                    var area = m.LParam == 0 ? null : Marshal.PtrToStringUni((IntPtr)m.LParam);
                    return EnvironmentChangedEventArgs.Create(action, area);
                })
                .Publish()
                .RefCount();
        }

        /// <summary>
        ///     The WM_SETTINGCHANGE messages of the SharedMessageWindow, produced on the thread of that window
        /// </summary>
        public static IObservable<EnvironmentChangedEventArgs> EnvironmentUpdateEvents => Singleton.Value._environmentObservable;
    }
}