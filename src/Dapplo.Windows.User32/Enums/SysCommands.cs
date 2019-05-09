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
    ///     A window receives this message when the user chooses a command from the Window menu
    ///    (formerly known as the system or control menu)
    ///     or when the user chooses the maximize button, minimize button, restore button, or close button.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SysCommands
    {
        /// <summary>
        ///     Sizes the window.
        /// </summary>
        SC_SIZE = 0xF000,

        /// <summary>
        ///     Moves the window.
        /// </summary>
        SC_MOVE = 0xF010,

        /// <summary>
        ///     Minimizes the window.
        /// </summary>
        SC_MINIMIZE = 0xF020,

        /// <summary>
        ///     Maximizes the window.
        /// </summary>
        SC_MAXIMIZE = 0xF030,

        /// <summary>
        ///     Moves to the next window.
        /// </summary>
        SC_NEXTWINDOW = 0xF040,

        /// <summary>
        ///     Moves to the previous window.
        /// </summary>
        SC_PREVWINDOW = 0xF050,

        /// <summary>
        ///     Closes the window.
        /// </summary>
        SC_CLOSE = 0xF060,

        /// <summary>
        ///     Scrolls vertically.
        /// </summary>
        SC_VSCROLL = 0xF070,

        /// <summary>
        ///     Scrolls horizontally.
        /// </summary>
        SC_HSCROLL = 0xF080,

        /// <summary>
        ///     Retrieves the window menu as a result of a mouse click.
        /// </summary>
        SC_MOUSEMENU = 0xF090,

        /// <summary>
        ///     Retrieves the window menu as a result of a keystroke.
        ///     If the wParam is SC_KEYMENU, lParam contains the character code of the key that is used with the ALT key to display
        ///     the popup menu. For example, pressing ALT+F to display the File popup will cause a WM_SYSCOMMAND with wParam equal
        ///     to SC_KEYMENU and lParam equal to 'f'.
        /// </summary>
        SC_KEYMENU = 0xF100,

        /// <summary>
        ///     TODO
        /// </summary>
        SC_ARRANGE = 0xF110,

        /// <summary>
        ///     Restores the window to its normal position and size.
        /// </summary>
        SC_RESTORE = 0xF120,

        /// <summary>
        ///     Activates the Start menu.
        /// </summary>
        SC_TASKLIST = 0xF130,

        /// <summary>
        ///     Executes the screen saver application specified in the [boot] section of the System.ini file.
        /// </summary>
        SC_SCREENSAVE = 0xF140,

        /// <summary>
        ///     Activates the window associated with the application-specified hot key. The lParam parameter identifies the window
        ///     to activate.
        /// </summary>
        SC_HOTKEY = 0xF150,

        /// <summary>
        ///     Selects the default item; the user double-clicked the window menu.
        /// </summary>
        SC_DEFAULT = 0xF160,

        /// <summary>
        ///     Sets the state of the display. This command supports devices that have power-saving features, such as a
        ///     battery-powered personal computer.
        ///     The lParam parameter can have the following values:
        ///     -1 (the display is powering on)
        ///     1 (the display is going to low power)
        ///     2 (the display is being shut off)
        /// </summary>
        SC_MONITORPOWER = 0xF170,

        /// <summary>
        ///     Changes the cursor to a question mark with a pointer. If the user then clicks a control in the dialog box, the
        ///     control receives a WM_HELP message.
        /// </summary>
        SC_CONTEXTHELP = 0xF180,

        /// <summary>
        ///     TODO
        /// </summary>
        SC_SEPARATOR = 0xF00F,

        /// <summary>
        ///     Indicates whether the screen saver is secure.
        /// </summary>
        SCF_ISSECURE = 0x00000001,

        /// <summary>
        ///     Same as SC_MINIMIZE
        /// </summary>
        SC_ICON = SC_MINIMIZE,

        /// <summary>
        ///     Same as SC_MAXIMIZE
        /// </summary>
        SC_ZOOM = SC_MAXIMIZE
    }
}