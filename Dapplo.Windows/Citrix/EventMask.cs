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

using System;

namespace Dapplo.Windows.Citrix
{
	/// <summary>
	/// XenApp Server creates ICA connections dynamically as needed. The following table lists and describes
	/// the events (possible values for EventMask), and indicates the flags triggered by the event. 
	/// </summary>
	[Flags]
	public enum EventMask : ulong
	{
		/// <summary>
		/// New ICA session created Create, State Change, All
		/// </summary>
		WF_EVENT_CREATE,
		/// <summary>
		/// Existing ICA session deleted Delete, State Change, All
		/// </summary>
		WF_EVENT_DELETE,
		/// <summary>
		/// User logon to system (from console or WinStation) Logon, State Change, All
		/// </summary>
		WF_EVENT_LOGON,
		/// <summary>
		/// User logon to system (from console or WinStation) Logon, State Change, All
		/// </summary>
		WF_EVENT_LOGOFF,
		/// <summary>
		/// ICA sesion connect from client Connect, State Change, All
		/// </summary>
		xWF_EVENT_CONNECT,
		/// <summary>
		/// ICA session disconnect from client Disconnect, State Event Description Flags triggered Change, All
		/// </summary>
		WF_EVENT_DISCONNECT,
		/// <summary>
		/// Existing ICA session renamed Rename, All
		/// </summary>
		WF_EVENT_RENAME,
		/// <summary>
		/// ICA session state change (this event is triggered when WF_CONNECTSTATE_CLASS (defined in Wfapi.h) changes)
		/// </summary>
		WF_EVENT_STATECHANGE,
		/// <summary>
		/// License state change (this event is triggered when a license is added or deleted using License Manager) License, All
		/// </summary>
		WF_EVENT_LICENSE,
		/// <summary>
		/// Wait for any event type WF_EVENT_FLUSH Unblock all waiting events(this event is used only as an EventMask)
		/// </summary>
		WF_EVENT_ALL,
		/// <summary>
		/// No event (this event is used only as a return value in pEventFlags)		/// </summary>
		WF_EVENT_NONE
	}
}
