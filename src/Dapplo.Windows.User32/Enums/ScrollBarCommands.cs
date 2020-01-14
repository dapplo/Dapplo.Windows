// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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