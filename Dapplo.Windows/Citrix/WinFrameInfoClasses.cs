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

namespace Dapplo.Windows.Citrix
{
	/// <summary>
	/// Type of information to be retrieved from the specified session via the WFQuerySessionInformation call.
	/// </summary>
	public enum WinFrameInfoClasses
	{
		/// <summary>
		/// Returns OSVERSIONINFO
		/// </summary>
		Version =1,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		InitialProgram = 2,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		WorkingDirectory = 3,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		OemId = 4,
		/// <summary>
		/// Returns ulong
		/// </summary>
		SessionId = 5,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		UserName = 6,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		WinStationName = 7,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		DomainName = 8,
		/// <summary>
		/// INT 2
		/// </summary>
		ConnectState = 9,
		/// <summary>
		///  USHORT 3
		/// </summary>
		ClientBuildNumber = 10,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		ClientName = 11,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		ClientDirectory = 12,
		/// <summary>
		/// USHORT 3
		/// </summary>
		ClientProductId = 13,
		/// <summary>
		/// WF_CLIENT_ADDRESS
		/// </summary>
		ClientAddress = 14,
		/// <summary>
		/// WF_CLIENT_DISPLAY
		/// </summary>
		ClientDisplay = 15,
		/// <summary>
		/// WF_CLIENT_CACHE
		/// </summary>
		ClientCache = 16,
		/// <summary>
		///  WF_CLIENT_DRIVES
		/// </summary>
		ClientDrives = 17,
		/// <summary>
		/// ULONG
		/// </summary>
		ICABufferLength = 18,
		/// <summary>
		/// Returns NULL-terminated string
		/// </summary>
		ApplicationName = 19,
		/// <summary>
		/// WF_APP_INFO
		/// </summary>
		AppInfo = 20,
		/// <summary>
		/// WF_CLIENT_INFO
		/// </summary>
		ClientInfo = 21,
		/// <summary>
		/// WF_USER_INFO
		/// </summary>
		UserInfo = 22,
		/// <summary>
		/// WF_SESSION_TIME
		/// </summary>
		SessionTime = 23
	}
}
