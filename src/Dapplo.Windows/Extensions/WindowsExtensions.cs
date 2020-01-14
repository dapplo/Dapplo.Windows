// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NETSTANDARD2_0
using System;
using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Extensions
{
    /// <summary>
    /// Extensions for WPF Windows
    /// </summary>
    public static class WindowsExtensions
    {
        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow AsInteropWindow(this Window window)
        {
            return InteropWindowFactory.CreateFor(window.GetHandle());
        }

        /// <summary>
        /// Retrieve the handle of a Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>IntPtr</returns>
        public static IntPtr GetHandle(this Window window)
        {
            var windowInteropHelper = new WindowInteropHelper(window);
            return windowInteropHelper.Handle;
        }

        /// <summary>
        /// Place the window
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow ApplyPlacement(this Window window, WindowPlacement windowPlacement)
        {
            var interopWindow = window.AsInteropWindow();
            interopWindow.SetPlacement(windowPlacement);
            return interopWindow;
        }

        /// <summary>
        /// Returns the WindowPlacement 
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>WindowPlacement</returns>
        public static WindowPlacement RetrievePlacement(this Window window)
        {
            var interopWindow = window.AsInteropWindow();
            return interopWindow.GetPlacement();
        }
    }
}
#endif