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
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     The following are the window styles. After the window has been created, these styles cannot be modified, except as
    ///     noted.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632600(v=vs.85).aspx">Window Styles</a>
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowStyleFlags : uint
    {
        /// <summary>
        ///     The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
        /// </summary>
        WS_OVERLAPPED = 0x00000000,

        /// <summary>
        ///     The windows is a pop-up window. This style cannot be used with the WS_CHILD style.
        /// </summary>
        WS_POPUP = 0x80000000,

        /// <summary>
        ///     The window is a child window. A window with this style cannot have a menu bar.
        ///     This style cannot be used with the WS_POPUP style.
        ///     Alias WS_CHILDWINDOW: Same as the WS_CHILD style.
        /// </summary>
        WS_CHILD = 0x40000000,

        /// <summary>
        ///     The window is initially minimized. Same as the WS_ICONIC style.
        /// </summary>
        WS_MINIMIZE = 0x20000000,

        /// <summary>
        ///     The window is initially visible.
        ///     This style can be turned on and off by using the ShowWindow or SetWindowPos function.
        /// </summary>
        WS_VISIBLE = 0x10000000,

        /// <summary>
        ///     The window is initially disabled. A disabled window cannot receive input from the user. To change this after a
        ///     window has been created, use the EnableWindow function.
        /// </summary>
        WS_DISABLED = 0x08000000,

        /// <summary>
        ///     Clips child windows relative to each other;
        ///     that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other
        ///     overlapping child windows out of the region of the child window to be updated.
        ///     If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area
        ///     of a child window, to draw within the client area of a neighboring child window.
        /// </summary>
        WS_CLIPSIBLINGS = 0x04000000,

        /// <summary>
        ///     Excludes the area occupied by child windows when drawing occurs within the parent window.
        ///     This style is used when creating the parent window.
        /// </summary>
        WS_CLIPCHILDREN = 0x02000000,

        /// <summary>
        ///     The window is initially maximized.
        /// </summary>
        WS_MAXIMIZE = 0x01000000,

        /// <summary>
        ///     The window has a thin-line border.
        /// </summary>
        WS_BORDER = 0x00800000,

        /// <summary>
        ///     The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title
        ///     bar.
        /// </summary>
        WS_DLGFRAME = 0x00400000,

        /// <summary>
        ///     The window has a vertical scroll bar.
        /// </summary>
        WS_VSCROLL = 0x00200000,

        /// <summary>
        ///     The window has a horizontal scroll bar.
        /// </summary>
        WS_HSCROLL = 0x00100000,

        /// <summary>
        ///     The window has a window menu on its title bar.
        ///     The WS_CAPTION style must also be specified.
        /// </summary>
        WS_SYSMENU = 0x00080000,

        /// <summary>
        ///     The window has a sizing border. Same as the WS_SIZEBOX style.
        /// </summary>
        WS_THICKFRAME = 0x00040000,

        /// <summary>
        ///     The window has a minimize button
        ///     Cannot be combined with the WS_EX_CONTEXTHELP style.
        ///     The WS_SYSMENU style must also be specified.
        ///     Value is the same as WS_GROUP, due to a different context
        /// </summary>
        WS_MINIMIZEBOX = 0x00020000,

        /// <summary>
        ///     The window is the first control of a group of controls.
        ///     The group consists of this first control and all controls defined after it, up to the next control with the
        ///     WS_GROUP style.
        ///     The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group.
        ///     The user can subsequently change the keyboard focus from one control in the group to the next control in the group
        ///     by using the direction keys.
        ///     You can turn this style on and off to change dialog box navigation.
        ///     To change this style after a window has been created, use the SetWindowLong function.
        ///     Value is the same as WS_MINIMIZEBOX, due to a different context
        /// </summary>
        WS_GROUP = 0x00020000,

        /// <summary>
        ///     The window has a maximize button.
        ///     Cannot be combined with the WS_EX_CONTEXTHELP style.
        ///     The WS_SYSMENU style must also be specified.
        ///     Value is the same as WS_TABSTOP, due to a different context
        /// </summary>
        WS_MAXIMIZEBOX = 0x00010000,

        /// <summary>
        ///     The window is a control that can receive the keyboard focus when the user presses the TAB key.
        ///     Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style.
        ///     You can turn this style on and off to change dialog box navigation.
        ///     To change this style after a window has been created, use the SetWindowLong function.
        ///     For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the
        ///     IsDialogMessage function.
        ///     Value is the same as WS_MAXIMIZEBOX, due to a different context
        /// </summary>
        WS_TABSTOP = 0x00010000,

#pragma warning disable 1591
        WS_UNK8000 = 0x00008000,
        WS_UNK4000 = 0x00004000,
        WS_UNK2000 = 0x00002000,
        WS_UNK1000 = 0x00001000,
        WS_UNK800 = 0x00000800,
        WS_UNK400 = 0x00000400,
        WS_UNK200 = 0x00000200,
        WS_UNK100 = 0x00000100,
        WS_UNK80 = 0x00000080,
        WS_UNK40 = 0x00000040,
        WS_UNK20 = 0x00000020,
        WS_UNK10 = 0x00000010,
        WS_UNK8 = 0x00000008,
        WS_UNK4 = 0x00000004,
        WS_UNK2 = 0x00000002,
        WS_UNK1 = 0x00000001,
#pragma warning restore 1591


        /// <summary>
        ///     The window has a title bar (includes the WS_BORDER style).
        /// </summary>
        WS_CAPTION = WS_BORDER | WS_DLGFRAME,

        /// <summary>
        ///     The window is an overlapped window. An overlapped window has a title bar and a border.
        ///     Same as the WS_OVERLAPPED style.
        /// </summary>
        /// <summary>
        ///     The window has a sizing border. Same as the WS_THICKFRAME style.
        /// </summary>
        /// <summary>
        ///     The window is an overlapped window. Same as the WS_OVERLAPPEDWINDOW style.
        /// </summary>
        /// <summary>
        ///     The window is an overlapped window. Same as the WS_TILEDWINDOW style.
        /// </summary>
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

        /// <summary>
        ///     The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu
        ///     visible.
        /// </summary>
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU

        //WS_CHILDWINDOW      = WS_CHILD
    }
}