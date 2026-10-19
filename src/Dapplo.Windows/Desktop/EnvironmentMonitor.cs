// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Messages.Enums;
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

        /// <summary>
        ///     Whether a screen reader is running (SPI_GETSCREENREADER), starting with the current value, then every change.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     A screen reader sets the flag with SPI_SETSCREENREADER; Windows only broadcasts WM_SETTINGCHANGE (wParam SPI_SETSCREENREADER) when it
        ///     passes SPIF_UPDATEINIFILE and SPIF_SENDCHANGE. NVDA does so when it starts and when it quits (verified on Windows Server with NVDA's
        ///     -q); for other screen readers, Narrator included, this is not verified. A screen reader which only sets the flag is not seen here,
        ///     so read <see cref="User32Api.IsScreenReaderRunning"/> when the value matters at a certain moment.
        ///     </para>
        ///     <para>
        ///     The current value is produced on the subscribing thread, the changes on the thread of the SharedMessageWindow; equal values in a
        ///     row are suppressed. The subscription to the messages starts before the current value is read, so no change is missed.
        ///     </para>
        /// </remarks>
        public static IObservable<bool> ScreenReaderChanges => Observable.Create<bool>(observer =>
            {
                var gate = new object();
                var subscription = EnvironmentUpdateEvents
                    .Where(e => e.SystemParametersInfoAction == SystemParametersInfoActions.SPI_SETSCREENREADER)
                    .Subscribe(_ =>
                    {
                        lock (gate)
                        {
                            observer.OnNext(User32Api.IsScreenReaderRunning());
                        }
                    }, observer.OnError, observer.OnCompleted);
                lock (gate)
                {
                    observer.OnNext(User32Api.IsScreenReaderRunning());
                }
                return subscription;
            })
            .DistinctUntilChanged();
    }
}