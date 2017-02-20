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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Description of PsAPI.
	/// </summary>
	public class PsAPI
	{
		[DllImport("psapi")]
		private static extern int EmptyWorkingSet(IntPtr hwProc);

		/// <summary>
		///     Make the process use less memory by emptying the working set
		/// </summary>
		public static void EmptyWorkingSet()
		{
			using (var currentProcess = Process.GetCurrentProcess())
			{
				EmptyWorkingSet(currentProcess.Handle);
			}
		}

		[DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

		[DllImport("psapi", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetProcessImageFileName(IntPtr hProcess, StringBuilder lpImageFileName, uint nSize);
	}
}