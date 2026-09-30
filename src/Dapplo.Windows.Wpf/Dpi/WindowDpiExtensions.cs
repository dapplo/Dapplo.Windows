// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enums;
using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Wpf.Messages;

namespace Dapplo.Windows.Wpf.Dpi
{
    /// <summary>
    ///     Extensions for the WPF Window class
    /// </summary>
    public static class WindowDpiExtensions
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Track the DPI of the specified Window, and compensate where WPF itself doesn't follow the monitor DPI.
        /// </summary>
        /// <remarks>
        ///     WPF already renders in device independent pixels, scaled to the DPI WPF renders the window with.
        ///     When WPF's own Per-Monitor DPI support is active (the process is Per-Monitor aware via the manifest, on .NET Framework also not disabled via the DoNotScaleForDpiChanges switch),
        ///     WPF scales and resizes the window itself, and this only publishes the DPI changes via the returned DpiHandler. This is the recommended setup.
        ///     Only when the window gets a monitor DPI which differs from the DPI WPF renders with, a LayoutTransform of monitor DPI / WPF DPI is applied to the content of the window,
        ///     and the window is moved / resized to the rectangle Windows suggests.
        ///     The returned DpiHandler is disposed when the window (HwndSource) is disposed, disposing it earlier detaches it.
        /// </remarks>
        /// <param name="window">Window</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Creating a dpi handler for {0}", window.GetType());
            }

            // WPF, or this extension, decides if the suggested rectangle needs to be applied
            var dpiHandler = new DpiHandler
            {
                ApplySuggestedWindowRect = false
            };
            var subscriptions = new CompositeDisposable();
            dpiHandler.MessageHandler = subscriptions;

            NativeRect? suggestedRect = null;
            var isUpdateScheduled = false;

            void UpdateScaling()
            {
                isUpdateScheduled = false;
                var hWnd = new WindowInteropHelper(window).Handle;
                if (subscriptions.IsDisposed || hWnd == IntPtr.Zero)
                {
                    return;
                }
                // The DPI WPF renders the window with, when WPF handles the DPI change itself this is equal to the DPI of the window
                var wpfDpi = VisualTreeHelper.GetDpi(window).PixelsPerInchX;
                var scaleFactor = wpfDpi > 0 ? dpiHandler.Dpi / wpfDpi : 1d;
                var wpfIgnoredTheDpiChange = Math.Abs(scaleFactor - 1) > 0.001;
                if (suggestedRect.HasValue && wpfIgnoredTheDpiChange)
                {
                    DpiHandler.ApplySuggestedRect(hWnd, suggestedRect.Value);
                }
                suggestedRect = null;
                window.UpdateLayoutTransform(scaleFactor);
            }

            void ScheduleUpdateScaling()
            {
                if (isUpdateScheduled)
                {
                    return;
                }
                isUpdateScheduled = true;
                // Run after the current message was processed by WPF, and the layout was done
                window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)UpdateScaling);
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Registering the UpdateLayoutTransform subscription for {0}", window.GetType());
            }
            subscriptions.Add(dpiHandler.OnDpiChanged.Subscribe(_ => ScheduleUpdateScaling()));
            subscriptions.Add(window.WinProcMessages().Subscribe(message =>
            {
                switch (message.Msg)
                {
                    case WindowsMessages.WM_NCCREATE:
                        // This is simulated after SourceInitialized, the window already exists
                        dpiHandler.RefreshDpi(message.Hwnd);
                        return;
                    case WindowsMessages.WM_DPICHANGED:
                        // The lParam is only valid while processing the message, copy it
                        if (message.LParam != IntPtr.Zero)
                        {
                            suggestedRect = Marshal.PtrToStructure<NativeRect>(message.LParam);
                        }
                        break;
                }
                dpiHandler.HandleWindowMessages(message);
            }, dpiHandler.Dispose));

            var existingHandle = new WindowInteropHelper(window).Handle;
            if (existingHandle != IntPtr.Zero)
            {
                dpiHandler.RefreshDpi(existingHandle);
            }

            return dpiHandler;
        }

        /// <summary>
        ///     This can be used to change the scaling of the FrameworkElement, by setting a LayoutTransform on its first visual child.
        ///     Nothing happens when the FrameworkElement has no visual child (yet).
        /// </summary>
        /// <param name="frameworkElement">FrameworkElement</param>
        /// <param name="scaleFactor">double with the factor, 1.0 removes the transform</param>
        public static void UpdateLayoutTransform(this FrameworkElement frameworkElement, double scaleFactor)
        {
            if (frameworkElement == null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Updating dpi for {0} to a scale factor {1}", frameworkElement.GetType(), scaleFactor);
            }

            // The template might not be applied yet
            if (VisualTreeHelper.GetChildrenCount(frameworkElement) == 0)
            {
                return;
            }
            // Adjust the rendering graphics and text size by applying the scale transform to the top level visual node of the Window
            var child = VisualTreeHelper.GetChild(frameworkElement, 0);
            if (scaleFactor > 0 && Math.Abs(scaleFactor - 1) > 0.001)
            {
                var scaleTransform = new ScaleTransform(scaleFactor, scaleFactor);
                child.SetValue(FrameworkElement.LayoutTransformProperty, scaleTransform);
            }
            else
            {
                child.ClearValue(FrameworkElement.LayoutTransformProperty);
            }
        }
    }
}
