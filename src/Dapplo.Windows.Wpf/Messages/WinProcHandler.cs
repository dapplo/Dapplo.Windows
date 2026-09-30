// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Windows.Interop;

namespace Dapplo.Windows.Wpf.Messages
{
    /// <summary>
    ///     This can be used to handle WinProc messages, for instance when there is no running WinProc
    /// </summary>
    public class WinProcHandler
    {
        private static readonly object Lock = new object();
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
        ///     Special HwndSource which is only there for handling messages, is top-level (no parent) to be able to handle ALL windows messages.
        ///     It is (re)created when it doesn't exist yet or was disposed.
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S2696:Instance members should not write to static fields", Justification = "Instance member needs access to the _hooks, this is checked!")]
        public HwndSource MessageHandlerWindow
        {
            get
            {
                lock (Lock)
                {
                    // Special code to make sure the _hWndSource is (re)created when it's not yet there or disposed
                    // For example in xUnit tests when WpfFact is used, the _hWndSource is disposed.
                    if (_hWndSource != null && !_hWndSource.IsDisposed)
                    {
                        return _hWndSource;
                    }
                    // Create a new message window
                    var hWndSource = CreateMessageWindow();
                    hWndSource.Disposed += (sender, args) =>
                    {
                        // Don't use MessageHandlerWindow here, that would create a new window while the old one is being disposed
                        ReleaseAllHooks();
                    };
                    // When the window is destroyed the hooks are no longer valid, dispose them
                    hWndSource.AddHook((IntPtr hWnd, int msg, IntPtr param, IntPtr lParam, ref bool handled) =>
                    {
                        if ((WindowsMessages)msg == WindowsMessages.WM_NCDESTROY)
                        {
                            ReleaseAllHooks();
                        }
                        return IntPtr.Zero;
                    });
                    _hWndSource = hWndSource;
                    return _hWndSource;
                }
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
            if (winProcHandlerHook == null)
            {
                throw new ArgumentNullException(nameof(winProcHandlerHook));
            }
            lock (Lock)
            {
                if (_hooks.Contains(winProcHandlerHook))
                {
                    return Disposable.Empty;
                }

                MessageHandlerWindow.AddHook(winProcHandlerHook.Hook);

                // Clone and add
                _hooks = new List<WinProcHandlerHook>(_hooks) { winProcHandlerHook };
            }
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
            lock (Lock)
            {
                if (!_hooks.Contains(winProcHandlerHook))
                {
                    // Already released, e.g. because the window was destroyed
                    return;
                }
                // Use the field, never create a new window just to remove a hook
                var hWndSource = _hWndSource;
                if (hWndSource != null && !hWndSource.IsDisposed)
                {
                    hWndSource.RemoveHook(winProcHandlerHook.Hook);
                }

                // Clone and remove
                var newHooks = new List<WinProcHandlerHook>(_hooks);
                newHooks.Remove(winProcHandlerHook);
                _hooks = newHooks;
            }
            winProcHandlerHook.Disposable?.Dispose();
        }

        /// <summary>
        ///     Unsubscribe all current hooks
        /// </summary>
        public void UnsubscribeAllHooks()
        {
            ReleaseAllHooks();
        }

        /// <summary>
        ///     Remove all hooks from the current window (if it's still alive) and dispose their disposables, without creating a window.
        /// </summary>
        private void ReleaseAllHooks()
        {
            List<WinProcHandlerHook> hooks;
            lock (Lock)
            {
                hooks = _hooks;
                _hooks = new List<WinProcHandlerHook>();
                var hWndSource = _hWndSource;
                if (hWndSource != null && !hWndSource.IsDisposed)
                {
                    foreach (var winProcHandlerHook in hooks)
                    {
                        hWndSource.RemoveHook(winProcHandlerHook.Hook);
                    }
                }
            }
            foreach (var winProcHandlerHook in hooks)
            {
                winProcHandlerHook.Disposable?.Dispose();
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
