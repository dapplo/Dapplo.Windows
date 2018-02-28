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
            var hwnd = new WindowInteropHelper(window).Handle;
            return new InteropWindow(hwnd);
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
