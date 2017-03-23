//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Linq;
using System.Windows.Interop;
using Dapplo.Log;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     This can be used to handle WinProc messages, for instance when there is no running winproc
    /// </summary>
    public class WinProcHandler
    {
        /// <summary>
        ///     To create a message-only window, specify HWndMessage as the parent of the window
        /// </summary>
        public static readonly IntPtr HwndMessage = new IntPtr(-3);

        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Hold the singeton
        /// </summary>
        private static readonly Lazy<WinProcHandler> Singleton = new Lazy<WinProcHandler>(() => new WinProcHandler());

        /// <summary>
        ///     Store hooks, so they can be removed
        /// </summary>
        private readonly IList<HwndSourceHook> _hooks = new List<HwndSourceHook>();

        /// <summary>
        ///     Special HwndSource which is only there for handling messages
        /// </summary>
        private readonly HwndSource _hwndSource = new HwndSource(new HwndSourceParameters
        {
            ParentWindow = HwndMessage,
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            AcquireHwndFocusInMenuMode = false,
            ExtendedWindowStyle = (int) ExtendedWindowStyleFlags.WS_NONE,
            WindowStyle = (int) WindowStyleFlags.WS_OVERLAPPED,
            WindowClassStyle = 0,
            WindowName = "Dapplo.Windows"
        });

        /// <summary>
        ///     The actual handle for the HwndSource
        /// </summary>
        public IntPtr Handle => _hwndSource.Handle;

        /// <summary>
        ///     Singleton instance of the WinProcHandler
        /// </summary>
        public static WinProcHandler Instance => Singleton.Value;

        /// <summary>
        ///     Add a hook to handle messages
        /// </summary>
        /// <param name="hook">HwndSourceHook</param>
        public void AddHook(HwndSourceHook hook)
        {
            Log.Verbose().WriteLine("Adding a hook to handle messages.");
            _hwndSource.AddHook(hook);
            _hooks.Add(hook);
        }

        /// <summary>
        ///     Unregister a hook
        /// </summary>
        /// <param name="hook">HwndSourceHook</param>
        public void RemoveHook(HwndSourceHook hook)
        {
            Log.Verbose().WriteLine("Removing a hook to handle messages.");
            _hwndSource.RemoveHook(hook);
            _hooks.Remove(hook);
        }

        /// <summary>
        ///     Remove all current hooks
        /// </summary>
        public void RemoveHooks()
        {
            foreach (var hwndSourceHook in _hooks.ToList())
            {
                RemoveHook(hwndSourceHook);
            }
        }
    }
}