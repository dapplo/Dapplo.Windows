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
    ///     Used by the user32.GetWindow function
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515(v=vs.85).aspx">GetWindow function</a>
    ///     The relationship between the specified window and the window whose handle is to be retrieved. This parameter can be
    ///     one of the following values.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum GetWindowCommands : uint
    {
        /// <summary>
        ///     The retrieved handle identifies the window of the same type that is highest in the Z order.
        ///     If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a
        ///     top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle
        ///     identifies a sibling window.
        /// </summary>
        GW_HWNDFIRST = 0,

        /// <summary>
        ///     The retrieved handle identifies the window of the same type that is lowest in the Z order.
        ///     If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a
        ///     top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle
        ///     identifies a sibling window.
        /// </summary>
        GW_HWNDLAST = 1,

        /// <summary>
        ///     The retrieved handle identifies the window below the specified window in the Z order.
        ///     If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a
        ///     top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle
        ///     identifies a sibling window.
        /// </summary>
        GW_HWNDNEXT = 2,

        /// <summary>
        ///     The retrieved handle identifies the window above the specified window in the Z order.
        ///     If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a
        ///     top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle
        ///     identifies a sibling window.
        /// </summary>
        GW_HWNDPREV = 3,

        /// <summary>
        ///     The retrieved handle identifies the specified window's owner window, if any. For more information, see Owned
        ///     Windows:
        ///     An overlapped or pop-up window can be owned by another overlapped or pop-up window. Being owned places several
        ///     constraints on a window.
        ///     An owned window is always above its owner in the z-order.
        ///     The system automatically destroys an owned window when its owner is destroyed.
        ///     An owned window is hidden when its owner is minimized.
        ///     Only an overlapped or pop-up window can be an owner window; a child window cannot be an owner window. An
        ///     application creates an owned window by specifying the owner's window handle as the hwndParent parameter of
        ///     CreateWindowEx when it creates a window with the WS_OVERLAPPED or WS_POPUP style. The hwndParent parameter must
        ///     identify an overlapped or pop-up window. If hwndParent identifies a child window, the system assigns ownership to
        ///     the top-level parent window of the child window. After creating an owned window, an application cannot transfer
        ///     ownership of the window to another window.
        ///     Dialog boxes and message boxes are owned windows by default. An application specifies the owner window when calling
        ///     a function that creates a dialog box or message box.
        ///     An application can use the GetWindow function with the GW_OWNER flag to retrieve a handle to a window's owner.
        /// </summary>
        GW_OWNER = 4,

        /// <summary>
        ///     The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent
        ///     window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window.
        ///     It does not examine descendant windows.
        /// </summary>
        GW_CHILD = 5,

        /// <summary>
        ///     The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first
        ///     such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is
        ///     that of the specified window.
        /// </summary>
        GW_ENABLEDPOPUP = 6
    }
}