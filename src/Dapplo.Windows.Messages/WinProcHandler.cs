// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Interop;
using Dapplo.Log;

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
        private List<WinProcHandlerHook> _hooks = new List<WinProcHandlerHook>();

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
        /// <param name="winProcHandlerHook">WinProcHandlerHook</param>
        /// <returns>IDisposable which unsubscribes the hWndSourceHook when Dispose is called</returns>
        public IDisposable Subscribe(WinProcHandlerHook winProcHandlerHook)
        {
            if (_hooks != null && _hooks.Contains(winProcHandlerHook))
            {
                return Disposable.Empty;
            }

            MessageHandlerWindow.AddHook(winProcHandlerHook.Hook);

            // Clone and add
            var newHooks = _hooks?.ToList() ?? new List<WinProcHandlerHook>();
            newHooks.Add(winProcHandlerHook);
            _hooks = newHooks;
            return Disposable.Create(() =>
            {
                Unsubscribe(winProcHandlerHook);
            });
        }

        /// <summary>
        ///     Unsubscribe a hook
        /// </summary>
        /// <param name="winProcHandlerHook">WinProcHandlerHook</param>
        private void Unsubscribe(WinProcHandlerHook winProcHandlerHook)
        {
            MessageHandlerWindow.RemoveHook(winProcHandlerHook.Hook);

            if (_hooks == null)
            {
                return;
            }

            // Clone and remove
            var newHooks = _hooks.ToList();
            newHooks.Remove(winProcHandlerHook);
            _hooks = newHooks;
            winProcHandlerHook.Disposable?.Dispose();
        }

        /// <summary>
        ///     Unsubscribe all current hooks
        /// </summary>
        public void UnsubscribeAllHooks()
        {
            foreach (var winProcHandlerHook in _hooks ?? Enumerable.Empty<WinProcHandlerHook>())
            {
                MessageHandlerWindow.RemoveHook(winProcHandlerHook.Hook);
                winProcHandlerHook.Disposable?.Dispose();
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