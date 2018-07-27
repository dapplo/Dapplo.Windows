//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Interop;

#endregion

namespace Dapplo.Windows.Messages
{
    /// <summary>
    ///     This can be used to handle WinProc messages, for instance when there is no running winproc
    /// </summary>
    public class WinProcHandler
    {
        private static readonly object Lock = new object();
        private static HwndSource _hwndSource;

        /// <summary>
        ///     Hold the singeton
        /// </summary>
        private static readonly Lazy<WinProcHandler> Singleton = new Lazy<WinProcHandler>(() => new WinProcHandler());

        /// <summary>
        ///     Store hooks, so they can be removed
        /// </summary>
        private readonly IList<HwndSourceHook> _hooks = new List<HwndSourceHook>();

        /// <summary>
        ///     Special HwndSource which is only there for handling messages, is top-level (no parent) to be able to handle ALL windows messages
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S2696:Instance members should not write to static fields", Justification = "Instance member needs access to the _hooks, this is checked!")]
        private HwndSource MessageHandlerWindow
        {
            get
            {
                // Special code to make sure the _hwndSource is (re)created when it's not yet there or disposed
                // For example in xunit tests when WpfFact is used, the _hwndSource is disposed.
                if (_hwndSource != null && !_hwndSource.IsDisposed)
                {
                    return _hwndSource;
                }
                lock (Lock)
                {
                    if (_hwndSource != null && !_hwndSource.IsDisposed)
                    {
                        return _hwndSource;
                    }
                    // Create a new message window
                    _hwndSource = CreateMessageWindow();
                    _hwndSource.Disposed += (sender, args) =>
                    {
                        UnsubscribeAllHooks();
                    };
                    // Hook automatic removing of all the hooks
                    _hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr param, IntPtr lParam, ref bool handled) =>
                    {
                        var windowsMessage = (WindowsMessages)msg;
                        if (windowsMessage != WindowsMessages.WM_NCDESTROY)
                        {
                            return IntPtr.Zero;
                        }

                        // The hooks are no longer valid, either there is no _hwndSource or it was disposed.
                        lock (Lock)
                        {
                            _hooks.Clear();
                        }
                        return IntPtr.Zero;
                    });
                }
                return _hwndSource;
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
        /// <param name="hook">HwndSourceHook</param>
        /// <returns>IDisposable which unsubscribes the hook when Dispose is called</returns>
        public IDisposable Subscribe(HwndSourceHook hook)
        {
            lock (Lock)
            {
                if (_hooks.Contains(hook))
                {
                    return Disposable.Empty;
                }

                MessageHandlerWindow.AddHook(hook);
                _hooks.Add(hook);
            }
            return Disposable.Create(() => { Unsubscribe(hook); });
        }

        /// <summary>
        ///     Unsubscribe a hook
        /// </summary>
        /// <param name="hook">HwndSourceHook</param>
        private void Unsubscribe(HwndSourceHook hook)
        {
            lock (Lock)
            {
                MessageHandlerWindow.RemoveHook(hook);
                _hooks.Remove(hook);
            }
        }

        /// <summary>
        ///     Unsubscribe all current hooks
        /// </summary>
        public void UnsubscribeAllHooks()
        {
            foreach (var hwndSourceHook in _hooks.ToList())
            {
                Unsubscribe(hwndSourceHook);
            }
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