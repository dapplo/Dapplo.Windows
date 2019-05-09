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
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Interop;

#endregion

namespace Dapplo.Windows.Messages
{
    /// <summary>
    ///     This is a Listener for WinProc messages
    /// </summary>
    public sealed class WinProcListener : NativeWindow, IDisposable
    {
        private List<HwndSourceHook> _hooks = new List<HwndSourceHook>();

        /// <summary>
        /// Is the WinProcListener already disposed?
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Constructor for a window listener
        /// </summary>
        /// <param name="control">Control to listen to</param>
        public WinProcListener(Control control)
        {
            if (control.IsHandleCreated && Handle == IntPtr.Zero)
            {
                AssignHandle(control.Handle);
            }
            else
            {
                control.HandleCreated += OnHandleCreated;
            }
            control.HandleDestroyed += OnHandleDestroyed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            IsDisposed = true;
            _hooks = null;
            ReleaseHandle();
        }

        /// <summary>
        ///     Adds an event handler
        /// </summary>
        /// <param name="hook">HwndSourceHook</param>
        public void AddHook(HwndSourceHook hook)
        {
            var newHooks = _hooks.ToList();
            newHooks.Add(hook);
            _hooks = newHooks;
        }

        /// <summary>
        ///     Listen for the control's window creation and then hook into it.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void OnHandleCreated(object sender, EventArgs e)
        {
            var handle = ((Control) sender).Handle;
            // control is now created, assign handle to NativeWindow.
            AssignHandle(handle);
        }

        /// <summary>
        ///     Remove the handle
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            // Window was destroyed, release hook.
            ReleaseHandle();
             _hooks = null;
        }

        /// <summary>
        ///     Removes the event handlers that were added by AddHook
        /// </summary>
        /// <param name="hook">HwndSourceHook, The event handler to remove.</param>
        public void RemoveHook(HwndSourceHook hook)
        {
            var newHooks = _hooks.ToList();
            newHooks.Remove(hook);
            _hooks = newHooks;
        }

        /// <inheritdoc />
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            if (IsDisposed)
            {
                return;
            }
            if (!ProcessMessage(m))
            {
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Helper class to process the message
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>bool if the message was handled</returns>
        private bool ProcessMessage(Message message)
        {
            bool handled = false;
            foreach (var hwndSourceHook in _hooks ?? Enumerable.Empty<HwndSourceHook>())
            {
                if (IsDisposed)
                {
                    break;
                }
                message.Result = hwndSourceHook.Invoke(message.HWnd, message.Msg, message.WParam, message.LParam, ref handled);
                if (handled)
                {
                    break;
                }
            }
            return handled;
        }
    }
}
#endif