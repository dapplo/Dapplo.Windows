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

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Description of Kernel32.
	/// </summary>
	public class Kernel32
	{
		public const uint ATTACHCONSOLE_ATTACHPARENTPROCESS = 0x0ffffffff; // default value if not specifing a process ID

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
			var _PathBuffer = new StringBuilder(512);
			// Try the GetModuleFileName method first since it's the fastest. 
			// May return ACCESS_DENIED (due to VM_READ flag) if the process is not owned by the current user.
			// Will fail if we are compiled as x86 and we're trying to open a 64 bit process...not allowed.
			var hprocess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMRead, false, processid);
			if (hprocess != IntPtr.Zero)
			{
				try
				{
					if (PsAPI.GetModuleFileNameEx(hprocess, IntPtr.Zero, _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
					{
						return _PathBuffer.ToString();
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
					var size = (uint) _PathBuffer.Capacity;
					if ((Environment.OSVersion.Version.Major >= 6) && QueryFullProcessImageName(hprocess, 0, _PathBuffer, ref size) && (size > 0))
					{
						return _PathBuffer.ToString();
					}

					// Try the GetProcessImageFileName method
					if (PsAPI.GetProcessImageFileName(hprocess, _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
					{
						var dospath = _PathBuffer.ToString();
						foreach (var drive in Environment.GetLogicalDrives())
						{
							if (QueryDosDevice(drive.TrimEnd('\\'), _PathBuffer, (uint) _PathBuffer.Capacity) > 0)
							{
								if (dospath.StartsWith(_PathBuffer.ToString()))
								{
									return drive + dospath.Remove(0, _PathBuffer.Length);
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

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint uuchMax);

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

		[DllImport("kernel32", SetLastError = true)]
		public static extern int ResumeThread(IntPtr hThread);

		[DllImport("kernel32", SetLastError = true)]
		public static extern uint SuspendThread(IntPtr hThread);
	}
}