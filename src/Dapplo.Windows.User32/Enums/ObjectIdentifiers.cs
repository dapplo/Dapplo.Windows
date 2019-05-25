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

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     Used for User32.SetWinEventHook
    ///     See WinUser.h or <a href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd373606.aspx">here</a>
    /// </summary>
    public enum ObjectIdentifiers
    {
        /// <summary>
        ///     The window itself rather than a child object.
        /// </summary>
        Window = 0,

        /// <summary>
        ///     The window's system menu.
        /// </summary>
        SystemMenu = -1,

        /// <summary>
        ///     The window's title bar.
        /// </summary>
        TitleBar = -2,

        /// <summary>
        ///     The window's menu bar.
        /// </summary>
        Menu = -3,

        /// <summary>
        ///     The window's client area.
        ///     In most cases, the operating system controls the frame elements and the client object contains all elements that
        ///     are controlled by the application.
        ///     Servers only process the WM_GETOBJECT messages in which the lParam is OBJID_CLIENT, OBJID_WINDOW, or a custom
        ///     object identifier.
        /// </summary>
        Client = -4,

        /// <summary>
        ///     The window's vertical scroll bar.
        /// </summary>
        VerticalScrollbar = -5,

        /// <summary>
        ///     The window's horizontal scroll bar.
        /// </summary>
        HorizontalScrollbar = -6,

        /// <summary>
        ///     The window's size grip: an optional frame component located at the lower-right corner of the window frame.
        /// </summary>
        SizeGrip = -7,

        /// <summary>
        ///     The text insertion bar (caret) in the window.
        /// </summary>
        Caret = -8,

        /// <summary>
        ///     The mouse pointer. There is only one mouse pointer in the system, and it is not a child of any window.
        /// </summary>
        Cursor = -9,

        /// <summary>
        ///     An alert that is associated with a window or an application.
        ///     System provided message boxes are the only UI elements that send events with this object identifier.
        ///     Server applications cannot use the AccessibleObjectFromX functions with this object identifier.
        ///     This is a known issue with Microsoft Active Accessibility.
        /// </summary>
        Alert = -10,

        /// <summary>
        ///     A sound object. Sound objects do not have screen locations or children, but they do have name and state attributes.
        ///     They are children of the application that is playing the sound.
        /// </summary>
        Sound = -11,

        /// <summary>
        ///     An object identifier that Oleacc.dll uses internally.
        ///     For more information, see Appendix F: Object Identifier Values for OBJID_QUERYCLASSNAMEIDX.
        /// </summary>
        QueryClassNameIndex = -12,

        /// <summary>
        ///     In response to this object identifier, third-party applications can expose their own object model.
        ///     Third-party applications can return any COM interface in response to this object identifier.
        /// </summary>
        NativeObjectModel = -16
    }
}