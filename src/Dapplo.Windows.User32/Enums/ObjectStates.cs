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
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd373609(v=vs.85).aspx">here</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ObjectStates : uint
    {
        /// <summary>
        ///     Indicates that the object does not have another state assigned to it.
        /// </summary>
        STATE_SYSTEM_NORMAL = 0x00000000,

        /// <summary>
        ///     The object is unavailable.
        /// </summary>
        STATE_SYSTEM_UNAVAILABLE = 0x00000001,

        /// <summary>
        ///     The object is selected.
        /// </summary>
        STATE_SYSTEM_SELECTED = 0x00000002,

        /// <summary>
        ///     The object has the keyboard focus. Do not confuse object focus with object selection. For more information, see
        ///     Selection and Focus Properties and Methods. For objects with this object state, send the EVENT_OBJECT_SHOW or
        ///     EVENT_OBJECT_HIDE WinEvents to notify client applications about state changes. Do not use EVENT_OBJECT_STATECHANGE.
        /// </summary>
        STATE_SYSTEM_FOCUSED = 0x00000004,

        /// <summary>
        ///     The object is pressed.
        /// </summary>
        STATE_SYSTEM_PRESSED = 0x00000008,

        /// <summary>
        ///     The object's check box is selected.
        /// </summary>
        STATE_SYSTEM_CHECKED = 0x00000010,

        /// <summary>
        ///     Indicates that the state of a three-state check box or toolbar button is not determined. The check box is neither
        ///     selected nor cleared and is therefore in the third or mixed state.
        /// </summary>
        STATE_SYSTEM_MIXED = 0x00000020,

        /// <summary>
        ///     Same as STATE_SYSTEM_MIXED
        /// </summary>
        STATE_SYSTEM_INDETERMINATE = STATE_SYSTEM_MIXED,

        /// <summary>
        ///     The object is designated read-only.
        /// </summary>
        STATE_SYSTEM_READONLY = 0x00000040,

        /// <summary>
        ///     The object is hot-tracked by the mouse, which means that the object's appearance has changed to indicate that the
        ///     mouse pointer is located over it.
        /// </summary>
        STATE_SYSTEM_HOTTRACKED = 0x00000080,

        /// <summary>
        ///     This state represents the default button in a window.
        /// </summary>
        STATE_SYSTEM_DEFAULT = 0x00000100,

        /// <summary>
        ///     The object's children that have the ROLE_SYSTEM_OUTLINEITEM role are displayed.
        /// </summary>
        STATE_SYSTEM_EXPANDED = 0x00000200,

        /// <summary>
        ///     The object's children that have the ROLE_SYSTEM_OUTLINEITEM role are hidden.
        /// </summary>
        STATE_SYSTEM_COLLAPSED = 0x00000400,

        /// <summary>
        ///     The control cannot accept input at this time.
        /// </summary>
        STATE_SYSTEM_BUSY = 0x00000800,

        /// <summary>
        ///     The object is not clipped to the boundary of its parent object, and it does not move automatically when the parent
        ///     moves.
        /// </summary>
        STATE_SYSTEM_FLOATING = 0x00001000,

        /// <summary>
        ///     Indicates scrolling or moving text or graphics.
        /// </summary>
        STATE_SYSTEM_MARQUEED = 0x00002000,

        /// <summary>
        ///     The object's appearance changes rapidly or constantly. Graphics that are animated occasionally are described as
        ///     ROLE_SYSTEM_GRAPHIC with the State property set to STATE_SYSTEM_ANIMATED. This state is used to indicate that the
        ///     object's location is changing.
        /// </summary>
        STATE_SYSTEM_ANIMATED = 0x00004000,

        /// <summary>
        ///     The object is programmatically hidden. For example, menu itmes are programmatically hidden until a user activates
        ///     the menu. Because objects with this state are not available to users, client applications must not communicate
        ///     information about the object to users. However, if client applications find an object with this state, they should
        ///     check whether STATE_SYSTEM_OFFSCREEN is also set. If this second state is defined, clients can communicate the
        ///     information about the object to users. For example, a list box can have both STATE_SYSTEM_INVISIBLE and
        ///     STATE_SYSTEM_OFFSCREEN set. In this case, the client application can communicate all items in the list to users.
        ///     If a client application is navigating through an IAccessible tree and encounters a parent object that is invisible,
        ///     Microsoft Active Accessibility will not expose information about any possible children of the parent as long as the
        ///     parent is invisible.
        /// </summary>
        STATE_SYSTEM_INVISIBLE = 0x00008000,

        /// <summary>
        ///     The object is clipped or has scrolled out of view, but it is not programmatically hidden. If the user makes the
        ///     viewport larger, more of the object will be visible on the computer screen.
        /// </summary>
        STATE_SYSTEM_OFFSCREEN = 0x00010000,

        /// <summary>
        ///     The object can be resized. For example, a user could change the size of a window by dragging it by the border.
        /// </summary>
        STATE_SYSTEM_SIZEABLE = 0x00020000,

        /// <summary>
        ///     Indicates that the object can be moved. For example, a user can click the object's title bar and drag the object to
        ///     a new location.
        /// </summary>
        STATE_SYSTEM_MOVEABLE = 0x00040000,

        /// <summary>
        ///     The object or child uses text-to-speech (TTS) technology for description purposes. When an object with this state
        ///     has the focus, a speech-based accessibility aid does not announce information because the object automatically
        ///     announces it.
        /// </summary>
        STATE_SYSTEM_SELFVOICING = 0x00080000,

        /// <summary>
        ///     The object is on the active window and is ready to receive keyboard focus.
        /// </summary>
        STATE_SYSTEM_FOCUSABLE = 0x00100000,

        /// <summary>
        ///     The object accepts selection.
        /// </summary>
        STATE_SYSTEM_SELECTABLE = 0x00200000,

        /// <summary>
        ///     Indicates that the object is formatted as a hyperlink. The object's role will usually be ROLE_SYSTEM_TEXT.
        /// </summary>
        STATE_SYSTEM_LINKED = 0x00400000,

        /// <summary>
        ///     The object is a hyperlink that has been visited (previously clicked) by a user.
        /// </summary>
        STATE_SYSTEM_TRAVERSED = 0x00800000,

        /// <summary>
        ///     Indicates that the object accepts multiple selected items; that is, SELFLAG_ADDSELECTION for the
        ///     IAccessible::accSelect method is valid.
        /// </summary>
        STATE_SYSTEM_MULTISELECTABLE = 0x01000000,

        /// <summary>
        ///     Indicates that an object extends its selection by using SELFLAG_EXTENDSELECTION in the IAccessible::accSelect
        ///     method.
        /// </summary>
        STATE_SYSTEM_EXTSELECTABLE = 0x02000000,

        /// <summary>
        ///     Indicates low-priority information that is not important to the user. This state is used, for example, when Word
        ///     changes the appearance of the TipWizard button on its toolbar to indicate that it has a hint for the user.
        /// </summary>
        STATE_SYSTEM_ALERT_LOW = 0x04000000,

        /// <summary>
        ///     Indicates important information that is not conveyed immediately to the user. For example, when a battery is
        ///     starting to reach a low level, a level indicator generates a medium-level alert. A blind access tool then generates
        ///     a sound to let the user know that important information is available, without actually interrupting the user's
        ///     work. The user could then query the alert information when convenient.
        /// </summary>
        STATE_SYSTEM_ALERT_MEDIUM = 0x08000000,

        /// <summary>
        ///     Indicates important information to be immediately conveyed to the user. For example, when a battery reaches a
        ///     critically low level, a level indicator generates a high-level alert. As a result, a blind access tool immediately
        ///     announces this information to the user, and a screen magnification program scrolls the screen so that the battery
        ///     indicator is in view. This state is also appropriate for any prompt or operation that must be completed before the
        ///     user can continue.
        /// </summary>
        STATE_SYSTEM_ALERT_HIGH = 0x10000000,

        /// <summary>
        ///     The object is a password-protected edit control.
        /// </summary>
        STATE_SYSTEM_PROTECTED = 0x20000000,

        /// <summary>
        ///     When invoked, the object displays a pop-up menu or a window.
        /// </summary>
        STATE_SYSTEM_HASPOPUP = 0x40000000,

        /// <summary>
        ///     A bitmask representing all valid state flags
        /// </summary>
        STATE_SYSTEM_VALID = 0x3FFFFFFF
    }
}