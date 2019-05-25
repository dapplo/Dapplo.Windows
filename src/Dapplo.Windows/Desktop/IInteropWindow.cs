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

#region using

using System;
using System.Collections.Generic;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     This is the interface of all classes which represent a native window
    /// </summary>
    public interface IInteropWindow
    {
        /// <summary>
        ///     Specifies if a WindowScroller can work with this window
        /// </summary>
        bool? CanScroll { get; set; }

        /// <summary>
        ///     Return the title of the window, if any
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        ///     Returns the children of this window
        /// </summary>
        IEnumerable<IInteropWindow> Children { get; set; }

        /// <summary>
        ///     string with the name of the internal class for the window
        /// </summary>
        string Classname { get; set; }

        /// <summary>
        ///     Handle (ID) of the window
        /// </summary>
        IntPtr Handle { get; }

        /// <summary>
        /// Checks if the children are retrieved in a Z-Order
        /// </summary>
        bool HasZOrderedChildren { get; set; }

        /// <summary>
        ///     Test if there are any children
        /// </summary>
        bool HasChildren { get; }

        /// <summary>
        ///     Does the window have a classname?
        /// </summary>
        bool HasClassname { get; }

        /// <summary>
        ///     Does this window have parent?
        /// </summary>
        bool HasParent { get; }

        /// <summary>
        ///     WindowInfo for the Window
        /// </summary>
        WindowInfo? Info { get; set; }

        /// <summary>
        ///     Returns true if the window is maximized
        /// </summary>
        bool? IsMaximized { get; set; }

        /// <summary>
        ///     Returns true if the window is minimized
        /// </summary>
        bool? IsMinimized { get; set; }

        /// <summary>
        ///     Returns true if the window is visible
        /// </summary>
        bool? IsVisible { get; set; }

        /// <summary>
        ///     The handle for the parent to which this window belongs
        /// </summary>
        IntPtr? Parent { get; set; }

        /// <summary>
        ///     The actually IInteropWindow for the parent.
        ///     This is filled when this window was retrieved via parent.GetChildren or parent.GetZOrderChildren
        /// </summary>
        IInteropWindow ParentWindow { get; set; }

        /// <summary>
        ///     WindowPlacement for the Window
        /// </summary>
        WindowPlacement? Placement { get; set; }

        /// <summary>
        ///     Get the thread ID this window belongs to, read with GetProcessId()
        /// </summary>
        int? ThreadId { get; set; }

        /// <summary>
        ///     Get the process ID this window belongs to, read with GetProcessId()
        /// </summary>
        int? ProcessId { get; set; }

        /// <summary>
        ///     Return the text (not title) of the window, if any
        /// </summary>
        string Text { get; set; }

        /// <summary>
        ///     Dump the information in the InteropWindow for debugging
        /// </summary>
        /// <param name="retrieveSettings">InteropWindowRetrieveSettings to specify what to dump</param>
        /// <param name="dump">StringBuilder to dump to</param>
        /// <param name="indentation">int</param>
        /// <returns>StringBuilder</returns>
        StringBuilder Dump(InteropWindowRetrieveSettings retrieveSettings = InteropWindowRetrieveSettings.CacheAll, StringBuilder dump = null, string indentation = "");
    }
}