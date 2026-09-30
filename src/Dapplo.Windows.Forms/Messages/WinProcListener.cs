// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Dapplo.Windows.Forms.Messages
{
    /// <summary>
    ///     This is a Listener for WinProc messages of a Control, it subclasses the window of the control.
    ///     It follows the handle of the control: when the handle is recreated (e.g. by changing ShowInTaskbar, RightToLeft or FormBorderStyle) the new handle is subclassed.
    /// </summary>
    public sealed class WinProcListener : NativeWindow, IDisposable
    {
        private readonly object _lock = new object();
        private readonly Control _control;
        private WinProcHook[] _hooks = Array.Empty<WinProcHook>();

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
            _control = control ?? throw new ArgumentNullException(nameof(control));
            // Always follow the handle, also when it already exists, so a recreated handle is subclassed again
            _control.HandleCreated += OnHandleCreated;
            _control.HandleDestroyed += OnHandleDestroyed;
            if (_control.IsHandleCreated)
            {
                AssignHandle(_control.Handle);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            _control.HandleCreated -= OnHandleCreated;
            _control.HandleDestroyed -= OnHandleDestroyed;
            lock (_lock)
            {
                _hooks = Array.Empty<WinProcHook>();
            }
            if (Handle != IntPtr.Zero)
            {
                ReleaseHandle();
            }
        }

        /// <summary>
        ///     Adds an event handler
        /// </summary>
        /// <param name="hook">WinProcHook</param>
        public void AddHook(WinProcHook hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }
            lock (_lock)
            {
                var newHooks = new List<WinProcHook>(_hooks) { hook };
                _hooks = newHooks.ToArray();
            }
        }

        /// <summary>
        ///     Removes the event handlers that were added by AddHook
        /// </summary>
        /// <param name="hook">WinProcHook, The event handler to remove.</param>
        public void RemoveHook(WinProcHook hook)
        {
            lock (_lock)
            {
                var newHooks = new List<WinProcHook>(_hooks);
                newHooks.Remove(hook);
                _hooks = newHooks.ToArray();
            }
        }

        /// <summary>
        ///     The control's window was (re)created, subclass it.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void OnHandleCreated(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }
            if (Handle != IntPtr.Zero)
            {
                ReleaseHandle();
            }
            AssignHandle(((Control)sender).Handle);
        }

        /// <summary>
        ///     The control's window is destroyed, release it. The hooks are kept, so they work again when the handle is recreated.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            if (Handle != IntPtr.Zero)
            {
                ReleaseHandle();
            }
        }

        /// <inheritdoc />
#if !NET6_0_OR_GREATER
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
#endif
        protected override void WndProc(ref Message m)
        {
            if (IsDisposed || !ProcessMessage(ref m))
            {
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Helper method to process the message, the result of the handling hook is stored in the message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>bool if the message was handled</returns>
        private bool ProcessMessage(ref Message message)
        {
            bool handled = false;
            foreach (var hWndSourceHook in _hooks)
            {
                var result = hWndSourceHook.Invoke(message.HWnd, message.Msg, message.WParam, message.LParam, ref handled);
                if (handled)
                {
                    message.Result = result;
                    break;
                }
            }
            return handled;
        }
    }
}
