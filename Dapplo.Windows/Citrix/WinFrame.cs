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
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.Citrix
{
	/// <summary>
	/// Helper class for the WinFrame API, which is used by Citrix XenApp and XenDesktop.
	/// </summary>
	public class WinFrame
	{
		/// <summary>
		/// See <a href="https://www.citrix.com/content/dam/citrix/en_us/documents/downloads/sdk/wf-api-sdk-guide.pdf">WFQuerySessionInformation</a>
		/// </summary>
		/// <param name="hServer">A handle to a server. Use the WFOpenServer() function to obtain a handle for a particular server, or specify WF_CURRENT_SERVER_HANDLE to use the current server.</param>
		/// <param name="iSessionId">The session ID of the target session. WF_CURRENT_SESSION specifies the current session.</param>
		/// <param name="infotype">WFInfoClasses, Type of information to be retrieved from the specified session.</param>
		/// <param name="ppBuffer">IntPtr to the buffer where the string, struct or whatever resides</param>
		/// <param name="pBytesReturned">number of bytes returned</param>
		/// <returns>bool if ok</returns>
		[DllImport("WFAPI", EntryPoint = "WFQuerySessionInformationW")]
		private static extern bool WFQuerySessionInformation(IntPtr hServer, int iSessionId, WinFrameInfoClasses infotype, out IntPtr ppBuffer, out int pBytesReturned);

		/// <summary>
		/// Free the memory which was reserved by the call to e.g. WFQuerySessionInformation 
		/// </summary>
		/// <param name="pMemory">IntPtr</param>
		[DllImport("WFAPI")]
		private static extern void WFFreeMemory(IntPtr pMemory);

		/// <summary>
		/// This function waits for an event (ICA session create/delete/connect, user logon/logoff, and so on)
		///before it returns.
		/// </summary>
		/// <param name="hServer">A handle to a server. Use the WFOpenServer() function to obtain a handle for a particular server, or specify WF_CURRENT_SERVER_HANDLE to use the current server.</param>
		/// <param name="eventMask">EventMask mask</param>
		/// <param name="pEventFlags">EventMask as result</param>
		/// <returns>HResult</returns>
		[DllImport("WFAPI")]
		private static extern HResult WFWaitSystemEvent(IntPtr hServer, EventMask eventMask, out EventMask pEventFlags);

		private const int CurrentSession = -1;
		private static readonly IntPtr CurrentServer = IntPtr.Zero;

		/// <summary>
		/// Retrieve the ip-address of the client PC
		/// </summary>
		/// <returns>string with the ip address</returns>
		public static string GetClientIpAddress()
		{
			return GetStruct<ClientAddress>(WinFrameInfoClasses.ClientAddress).IpAddress;
		}

		/// <summary>
		/// Retrieve the specified struct
		/// </summary>
		/// <typeparam name="T">type of the struct to return</typeparam>
		/// <returns>struct of type T</returns>
		public static T GetStruct<T>(WinFrameInfoClasses infoClass)
		{
			IntPtr addr;
			int returned;
			if (!WFQuerySessionInformation(CurrentServer, CurrentSession, infoClass, out addr, out returned))
			{
				return default(T);
			}
			try
			{
				return (T)Marshal.PtrToStructure(addr, typeof(T));
			}
			finally
			{
				WFFreeMemory(addr);
			}
		}

		/// <summary>
		/// Retrieve a string value from the WFQuerySessionInformation
		/// </summary>
		/// <returns>string with the value</returns>
		public static string GetString(WinFrameInfoClasses infoClass)
		{
			IntPtr addr;
			int returned;
			if (!WFQuerySessionInformation(CurrentServer, CurrentSession, infoClass, out addr, out returned))
			{
				return null;
			}
			try
			{
				return Marshal.PtrToStringAuto(addr);
			}
			finally
			{
				WFFreeMemory(addr);
			}
		}
	}
}
