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

#region using

using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     See
	///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx">OSVERSIONINFOEX structure</a>
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct OsVersionInfoEx
	{
		/// <summary>
		///     The size of this data structure, in bytes. Set this member to sizeof(OSVERSIONINFOEX).
		/// </summary>
		public int dwOSVersionInfoSize;

		/// <summary>
		///     The major version number of the operating system.
		/// </summary>
		public int dwMajorVersion;

		/// <summary>
		///     The minor version number of the operating system.
		/// </summary>
		public int dwMinorVersion;

		/// <summary>
		///     The build number of the operating system.
		/// </summary>
		public int dwBuildNumber;

		/// <summary>
		///     The operating system platform. This member can be VER_PLATFORM_WIN32_NT (2).
		/// </summary>
		public int dwPlatformId;

		/// <summary>
		///     A null-terminated string, such as "Service Pack 3", that indicates the latest Service Pack installed on the system.
		///     If no Service Pack has been installed, the string is empty.
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szCSDVersion;

		/// <summary>
		///     The major version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
		///     major version number is 3.
		///     If no Service Pack has been installed, the value is zero.
		/// </summary>
		public short wServicePackMajor;

		/// <summary>
		///     The minor version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
		///     minor version number is 0.
		/// </summary>
		public short wServicePackMinor;

		/// <summary>
		///     A bit mask that identifies the product suites available on the system. This member can be a combination of the
		///     following values.
		/// </summary>
		public WindowsSuites wSuiteMask;

		/// <summary>
		///     Any additional information about the system.
		/// </summary>
		public WindowsProductTypes wProductType;

		/// <summary>
		///     Reserved for future use.
		/// </summary>
		public byte wReserved;
	}
}