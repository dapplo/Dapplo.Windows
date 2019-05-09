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

#endregion

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632611(v=vs.85).aspx">WINDOWPLACEMENT structure</a>
    /// </summary>
    [Flags]
    public enum WindowPlacementFlags : uint
    {
        /// <summary>
        /// When no flags are used
        /// </summary>
        None = 0,

        /// <summary>
        ///     The coordinates of the minimized window may be specified.
        ///     This flag must be specified if the coordinates are set in the ptMinPosition member.
        /// </summary>
        SetMinPosition = 0x0001,

        /// <summary>
        ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
        ///     the request to the thread that owns the window.
        ///     This prevents the calling thread from blocking its execution while other threads process the request.
        /// </summary>
        AsyncWindowPlacement = 0x0004,

        /// <summary>
        ///     The restored window will be maximized, regardless of whether it was maximized before it was minimized.
        ///     This setting is only valid the next time the window is restored.
        ///     It does not change the default restoration behavior.
        ///     This flag is only valid when the SW_SHOWMINIMIZED value is specified for the showCmd member.
        /// </summary>
        RestoreToMaximized = 0x0002
    }
}