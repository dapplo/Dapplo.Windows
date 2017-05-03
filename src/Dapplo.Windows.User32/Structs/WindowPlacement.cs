//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     Contains information about the placement of a window on the screen.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct WindowPlacement
    {
        /// <summary>
        ///     The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set
        ///     this member to sizeof(WINDOWPLACEMENT).
        ///     <para>
        ///         GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
        ///     </para>
        /// </summary>
        public int Length;

        /// <summary>
        ///     Specifies flags that control the position of the minimized window and the method by which the window is restored.
        /// </summary>
        public WindowPlacementFlags Flags;

        /// <summary>
        ///     The current show state of the window.
        /// </summary>
        public ShowWindowCommands ShowCmd;

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is minimized.
        /// </summary>
        public POINT MinPosition;

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is maximized.
        /// </summary>
        public POINT MaxPosition;

        /// <summary>
        ///     The window's coordinates when the window is in the restored position.
        /// </summary>
        public RECT NormalPosition;

        /// <summary>
        ///     Gets the default (empty) value.
        /// </summary>
        public static WindowPlacement Create()
        {
            return new WindowPlacement
            {
                Length = Marshal.SizeOf(typeof(WindowPlacement))
            };
        }
    }
}