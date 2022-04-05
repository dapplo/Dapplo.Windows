// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Messages.Enumerations;
#if !NETSTANDARD2_0
using System;
using System.Windows;
using System.Windows.Media;
using Dapplo.Log;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Dpi.Wpf
{
    /// <summary>
    ///     Extensions for the WPF Window class
    /// </summary>
    public static class WindowDpiExtensions
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Attach a DpiHandler to the specified window
        /// </summary>
        /// <param name="window">Windows</param>
        /// <param name="dpiHandler">DpiHandler</param>
        private static void AttachDpiHandler(Window window, DpiHandler dpiHandler)
        {
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Registering the UpdateLayoutTransform subscription for {0}", window.GetType());
            }
            // Add the layout transform action
            var transformSubscription = dpiHandler.OnDpiChanged.Subscribe(dpiChangeInfo => window.UpdateLayoutTransform((double)dpiChangeInfo.NewDpi / DpiCalculator.DefaultScreenDpi));
            window.WinProcMessages().Subscribe(message =>
            {
                dpiHandler.HandleWindowMessages(message);
                switch (message.Message)
                {
                    case WindowsMessages.WM_NCCREATE:
                        // Apply scaling 1x time
                        window.UpdateLayoutTransform((double)NativeDpiMethods.GetDpi(message.Handle) / DpiCalculator.DefaultScreenDpi);
                        break;
                    case WindowsMessages.WM_DESTROY:
                        // Remove layout transform 
                        if (Log.IsVerboseEnabled())
                        {
                            Log.Verbose().WriteLine("Removing the UpdateLayoutTransform subscription for {0}", window.GetType());
                        }

                        transformSubscription.Dispose();
                        break;
                }
            });
        }

        /// <summary>
        ///     Handle DPI changes for the specified Window, this is actually not really needed for WPF.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>DpiHandler</returns>
        public static DpiHandler AttachDpiHandler(this Window window)
        {
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Creating a dpi handler for {0}", window.GetType());
            }

            var dpiHandler = new DpiHandler();
            AttachDpiHandler(window, dpiHandler);

            return dpiHandler;
        }

        /// <summary>
        ///     This can be used to change the scaling of the FrameworkElement
        /// </summary>
        /// <param name="frameworkElement">FrameworkElement</param>
        /// <param name="scaleFactor">double with the factor (1.0 = 100% = 96 dpi)</param>
        public static void UpdateLayoutTransform(this FrameworkElement frameworkElement, double scaleFactor)
        {
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Updating dpi for {0} to a scale factor {1}", frameworkElement.GetType(), scaleFactor);
            }

            // Adjust the rendering graphics and text size by applying the scale transform to the top level visual node of the Window
            var child = VisualTreeHelper.GetChild(frameworkElement, 0);
            if (Math.Abs(scaleFactor) > 1)
            {
                var scaleTransform = new ScaleTransform(scaleFactor, scaleFactor);
                child.SetValue(FrameworkElement.LayoutTransformProperty, scaleTransform);
            }
            else
            {
                child.SetValue(FrameworkElement.LayoutTransformProperty, null);
            }
        }
    }
}
#endif