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

namespace Dapplo.Windows.Citrix.Enums
{
    /// <summary>
    ///     XenApp Server creates ICA connections dynamically as needed. The following table lists and describes
    ///     the events (possible values for EventMask), and indicates the flags triggered by the event.
    /// </summary>
    [Flags]
    public enum EventMask : ulong
    {
        /// <summary>
        ///     WF_EVENT_NONE: No event (this event is used only as a return value in pEventFlags)
        /// </summary>
        None = 0x00000000,

        /// <summary>
        ///     WF_EVENT_CREATE: New ICA session created Create, State Change, All
        /// </summary>
        Create = 0x00000001,

        /// <summary>
        ///     WF_EVENT_DELETE: Existing ICA session deleted Delete, State Change, All
        /// </summary>
        Delete = 0x00000002,

        /// <summary>
        ///     WF_EVENT_LOGON: User logon to system (from console or WinStation) Logon, State Change, All
        /// </summary>
        Logon = 0x00000020,

        /// <summary>
        ///     WF_EVENT_LOGOFF: User logoff to system (from console or WinStation) Logoff, State Change, All
        /// </summary>
        Logoff = 0x00000040,

        /// <summary>
        ///     xWF_EVENT_CONNECT: ICA sesion connect from client Connect, State Change, All
        /// </summary>
        Connect = 0x00000008,

        /// <summary>
        ///     WF_EVENT_DISCONNECT: ICA session disconnect from client Disconnect, State Event Description Flags triggered Change,
        ///     All
        /// </summary>
        Disconnect = 0x00000010,

        /// <summary>
        ///     WF_EVENT_RENAME: Existing ICA session renamed Rename, All
        /// </summary>
        Rename = 0x00000004,

        /// <summary>
        ///     WF_EVENT_STATECHANGE: ICA session state change (this event is triggered when WF_CONNECTSTATE_CLASS (defined in
        ///     Wfapi.h) changes)
        /// </summary>
        StateChange = 0x00000080,

        /// <summary>
        ///     WF_EVENT_LICENSE: License state change (this event is triggered when a license is added or deleted using License
        ///     Manager) License, All
        /// </summary>
        License = 0x00000100,

        /// <summary>
        ///     WF_EVENT_ALL: Wait for any event type WF_EVENT_FLUSH Unblock all waiting events(this event is used only as an
        ///     EventMask)
        /// </summary>
        All = 0x7fffffff
    }
}