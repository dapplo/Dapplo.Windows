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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Interop;
using Dapplo.Log;

#endregion

namespace Dapplo.Windows.Messages
{
    /// <summary>
    ///     This can be used to handle WinProc messages, for instance when there is no running WinProc
    /// </summary>
    public class WinProcHandler
    {
        private static readonly LogSource Log = new LogSource();
        private static HwndSource _hWndSource;

        /// <summary>
        ///     Hold the singleton
        /// </summary>
        private static readonly Lazy<WinProcHandler> Singleton = new Lazy<WinProcHandler>(() => new WinProcHandler());

        /// <summary>
        ///     Store hooks, so they can be removed
        /// </summary>
        private List<HwndSourceHook> _hooks = new List<HwndSourceHook>();

        /// <summary>
        ///     Special HwndSource which is only there for handling messages, is top-level (no parent) to be able to handle ALL windows messages
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S2696:Instance members should not write to static fields", Justification = "Instance member needs access to the _hooks, this is checked!")]
        private HwndSource MessageHandlerWindow
        {
            get
            {
                // Special code to make sure the _hWndSource is (re)created when it's not yet there or disposed
                // For example in xUnit tests when WpfFact is used, the _hWndSource is disposed.
                if (_hWndSource != null && !_hWndSource.IsDisposed)
                {
                    return _hWndSource;
                }
                // Create a new message window
                _hWndSource = CreateMessageWindow();
                Log.Verbose().WriteLine("MessageHandlerWindow with handle {0} was created.", _hWndSource.Handle);
                _hWndSource.Disposed += (sender, args) =>
                {
                    Log.Verbose().WriteLine("MessageHandlerWindow with handle {0} is disposed.", _hWndSource.Handle);
                    UnsubscribeAllHooks();
                };
                // Hook automatic removing of all the hooks
                _hWndSource.AddHook((IntPtr hWnd, int msg, IntPtr param, IntPtr lParam, ref bool handled) =>
                {
                    var windowsMessage = (WindowsMessages)msg;
                    if (windowsMessage != WindowsMessages.WM_NCDESTROY)
                    {
                        return IntPtr.Zero;
                    }

                    // The hooks are no longer valid, either there is no _hWndSource or it was disposed.
                    _hooks = null;
                    return IntPtr.Zero;
                });
                return _hWndSource;
            }
        }

        /// <summary>
        ///     The actual handle for the HwndSource
        /// </summary>
        public IntPtr Handle => MessageHandlerWindow.Handle;

        /// <summary>
        ///     Singleton instance of the WinProcHandler
        /// </summary>
        public static WinProcHandler Instance => Singleton.Value;

        /// <summary>
        ///     Subscribe a hook to handle messages
        /// </summary>
        /// <param name="hWndSourceHook">HwndSourceHook</param>
        /// <returns>IDisposable which unsubscribes the hWndSourceHook when Dispose is called</returns>
        public IDisposable Subscribe(HwndSourceHook hWndSourceHook)
        {
            if (_hooks != null && _hooks.Contains(hWndSourceHook))
            {
                return Disposable.Empty;
            }

            MessageHandlerWindow.AddHook(hWndSourceHook);

            // Clone and add
            var newHooks = _hooks?.ToList() ?? new List<HwndSourceHook>();
            newHooks.Add(hWndSourceHook);
            _hooks = newHooks;
            return Disposable.Create(() => { Unsubscribe(hWndSourceHook); });
        }

        /// <summary>
        ///     Unsubscribe a hook
        /// </summary>
        /// <param name="hWndSourceHook">HwndSourceHook</param>
        private void Unsubscribe(HwndSourceHook hWndSourceHook)
        {
            MessageHandlerWindow.RemoveHook(hWndSourceHook);

            if (_hooks == null)
            {
                return;
            }

            // Clone and remove
            var newHooks = _hooks.ToList();
            newHooks.Remove(hWndSourceHook);
            _hooks = newHooks;
        }

        /// <summary>
        ///     Unsubscribe all current hooks
        /// </summary>
        public void UnsubscribeAllHooks()
        {
            foreach (var hWndSourceHook in _hooks ?? Enumerable.Empty<HwndSourceHook>())
            {
                MessageHandlerWindow.RemoveHook(hWndSourceHook);
            }
            _hooks = null;
        }

        /// <summary>
        /// Creates a HwndSource to catch windows message
        /// </summary>
        /// <param name="parent">IntPtr for the parent, this should usually not be set</param>
        /// <param name="title">Title of the window, a default is already set</param>
        /// <returns>HwndSource</returns>
        public static HwndSource CreateMessageWindow(IntPtr parent = default, string title = "Dapplo.MessageHandlerWindow")
        {
            return new HwndSource(new HwndSourceParameters
            {
                ParentWindow = parent,
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                AcquireHwndFocusInMenuMode = false,
                ExtendedWindowStyle = 0, // ExtendedWindowStyleFlags.WS_NONE
                WindowStyle = 0, // WindowStyleFlags.WS_OVERLAPPED
                WindowClassStyle = 0,
                WindowName = title
            });
        }
    }
}
#endif