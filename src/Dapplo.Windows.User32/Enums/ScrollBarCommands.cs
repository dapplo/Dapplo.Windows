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

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/de-de/library/windows/desktop/bb787577(v=vs.85).aspx">WM_VSCROLL message</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ScrollBarCommands : uint
    {
        /// <summary>
        ///     Scrolls one line up.
        /// </summary>
        SB_LINEUP = 0,

        /// <summary>
        ///     Same as SB_LINEUP, can be used when thinking horizontally
        /// </summary>
        SB_LINELEFT = SB_LINEUP,

        /// <summary>
        ///     Scrolls one line down.
        /// </summary>
        SB_LINEDOWN = 1,

        /// <summary>
        ///     Same as SB_LINEDOWN, can be used when thinking horizontally
        /// </summary>
        SB_LINERIGHT = SB_LINEDOWN,

        /// <summary>
        ///     Scrolls one page up.
        /// </summary>
        SB_PAGEUP = 2,

        /// <summary>
        ///     Same as SB_PAGEUP, can be used when thinking horizontally
        /// </summary>
        SB_PAGELEFT = SB_PAGEUP,

        /// <summary>
        ///     Scrolls one page down.
        /// </summary>
        SB_PAGEDOWN = 3,

        /// <summary>
        ///     Same as SB_PAGEDOWN, can be used when thinking horizontally
        /// </summary>
        SB_PAGERIGHT = SB_PAGEDOWN,

        /// <summary>
        ///     The user has dragged the scroll box (thumb) and released the mouse button.
        ///     The HIWORD indicates the position of the scroll box at the end of the drag operation.
        /// </summary>
        SB_THUMBPOSITION = 4,

        /// <summary>
        ///     The user is dragging the scroll box.
        ///     This message is sent repeatedly until the user releases the mouse button.
        ///     The HIWORD indicates the position that the scroll box has been dragged to.
        /// </summary>
        SB_THUMBTRACK = 5,

        /// <summary>
        ///     Scrolls to the upper left.
        /// </summary>
        SB_TOP = 6,

        /// <summary>
        ///     Same as SB_TOP, can be used when thinking horizontally
        /// </summary>
        SB_LEFT = SB_TOP,

        /// <summary>
        ///     Scrolls to the lower right.
        /// </summary>
        SB_BOTTOM = 7,

        /// <summary>
        ///     Same as SB_BOTTOM, can be used when thinking horizontally
        /// </summary>
        SB_RIGHT = SB_BOTTOM,

        /// <summary>
        ///     Ends scroll.
        /// </summary>
        SB_ENDSCROLL = 8
    }
}