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

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Kernel 32 functionality
	/// </summary>
	public class Kernel32
	{
		/// <summary>
		/// default value if not specifing a process ID
		/// </summary>
		public const uint ATTACHCONSOLE_ATTACHPARENTPROCESS = 0x0ffffffff;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AllocConsole();

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		/// <summary>
		///     Method to get the process path
		/// </summary>
		/// <param name="processid"></param>
		/// <returns></returns>
		public static string GetProcessPath(int processid)
		{
			var pathBuffer = new StringBuilder(512);
			// Try the GetModuleFileName method first since it's the fastest. 
			// May return ACCESS_DENIED (due to VM_READ flag) if the process is not owned by the current user.
			// Will fail if we are compiled as x86 and we're trying to open a 64 bit process...not allowed.
			var hprocess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead, false, processid);
			if (hprocess != IntPtr.Zero)
			{
				try
				{
					if (PsAPI.GetModuleFileNameEx(hprocess, IntPtr.Zero, pathBuffer, (uint) pathBuffer.Capacity) > 0)
					{
						return pathBuffer.ToString();
					}
				}
				finally
				{
					CloseHandle(hprocess);
				}
			}

			hprocess = OpenProcess(ProcessAccessFlags.QueryInformation, false, processid);
			if (hprocess != IntPtr.Zero)
			{
				try
				{
					// Try this method for Vista or higher operating systems
					var size = (uint) pathBuffer.Capacity;
					if (Environment.OSVersion.Version.Major >= 6 && QueryFullProcessImageName(hprocess, 0, pathBuffer, ref size) && size > 0)
					{
						return pathBuffer.ToString();
					}

					// Try the GetProcessImageFileName method
					if (PsAPI.GetProcessImageFileName(hprocess, pathBuffer, (uint) pathBuffer.Capacity) > 0)
					{
						var dospath = pathBuffer.ToString();
						foreach (var drive in Environment.GetLogicalDrives())
						{
							if (QueryDosDevice(drive.TrimEnd('\\'), pathBuffer, (uint) pathBuffer.Capacity) > 0)
							{
								if (dospath.StartsWith(pathBuffer.ToString()))
								{
									return drive + dospath.Remove(0, pathBuffer.Length);
								}
							}
						}
					}
				}
				finally
				{
					CloseHandle(hprocess);
				}
			}

			return string.Empty;
		}

		/// <summary>
		///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724358.aspx">GetProductInfo function</a>
		/// </summary>
		/// <param name="osMajorVersion">
		///     The major version number of the operating system. The minimum value is 6.
		///     The combination of the dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion, and dwSpMinorVersion parameters
		///     describes the maximum target operating system version for the application. For example, Windows Vista and Windows
		///     Server 2008 are version 6.0.0.0 and Windows 7 and Windows Server 2008 R2 are version 6.1.0.0.
		/// </param>
		/// <param name="osMinorVersion">The minor version number of the operating system. The minimum value is 0.</param>
		/// <param name="spMajorVersion">The major version number of the operating system service pack. The minimum value is 0.</param>
		/// <param name="spMinorVersion">The minor version number of the operating system service pack. The minimum value is 0.</param>
		/// <param name="edition">WindowsProducts</param>
		/// <returns></returns>
		[DllImport("Kernel32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProductInfo(int osMajorVersion, int osMinorVersion, int spMajorVersion, int spMinorVersion, out WindowsProducts edition);

		/// <summary>
		///     See
		///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724451(v=vs.85).aspx">GetVersionEx function</a>
		/// </summary>
		/// <param name="osVersionInfo">OsVersionInfoEx</param>
		/// <returns>If the function fails, the return value is false. To get extended error information, call GetLastError.</returns>
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetVersionEx(ref OsVersionInfoEx osVersionInfo);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint uuchMax);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);
	}
}