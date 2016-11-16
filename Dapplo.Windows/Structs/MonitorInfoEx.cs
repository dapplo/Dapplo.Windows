//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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

using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     The MONITORINFOEX structure contains information about a display monitor.
	///     The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
	///     The MONITORINFOEX structure is a superset of the MONITORINFO structure. The MONITORINFOEX structure adds a string
	///     member to contain a name
	///     for the display monitor.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct MonitorInfoEx
	{
		/// <summary>
		///     The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the
		///     GetMonitorInfo function.
		///     Doing so lets the function determine the type of structure you are passing to it.
		/// </summary>
		public int Size;

		/// <summary>
		///     A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
		///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
		///     values.
		/// </summary>
		public RECT Monitor;

		/// <summary>
		///     A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
		///     expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
		///     The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
		///     Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative
		///     values.
		/// </summary>
		public RECT WorkArea;

		/// <summary>
		///     The attributes of the display monitor.
		///     This member can be the following value:
		///     1 : MONITORINFOF_PRIMARY
		/// </summary>
		public uint Flags;

		// size of a device name string
		private const int CCHDEVICENAME = 32;

		/// <summary>
		///     A string that specifies the device name of the monitor being used. Most applications have no use for a display
		///     monitor name,
		///     and so can save some bytes by using a MONITORINFO structure.
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)] public string DeviceName;

		public void Init()
		{
			Size = Marshal.SizeOf(this);
			DeviceName = string.Empty;
		}
	}
}