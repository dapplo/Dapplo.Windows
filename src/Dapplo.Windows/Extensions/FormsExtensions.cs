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
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Structs;
using System.Windows.Forms;

namespace Dapplo.Windows.Extensions
{
    /// <summary>
    /// Extensions for Forms
    /// </summary>
    public static class FormsExtensions
    {
        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied WindowForm
        /// </summary>
        /// <param name="form">Form</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow AsInteropWindow(this Form form)
        {
            return InteropWindowFactory.CreateFor(form.Handle);
        }

        /// <summary>
        /// Place the Form
        /// </summary>
        /// <param name="form">Form</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow ApplyPlacement(this Form form, WindowPlacement windowPlacement)
        {
            var interopWindow = form.AsInteropWindow();
            interopWindow.SetPlacement(windowPlacement);
            return interopWindow;
        }

        /// <summary>
        /// Returns the WindowPlacement 
        /// </summary>
        /// <param name="form">WindowForm</param>
        /// <returns>WindowPlacement</returns>
        public static WindowPlacement RetrievePlacement(this Form form)
        {
            var interopWindow = form.AsInteropWindow();
            return interopWindow.GetPlacement();
        }
    }
}
#endif