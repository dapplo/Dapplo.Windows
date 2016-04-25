/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// Abstract class SafeObjectHandle which contains all handles that are cleaned with DeleteObject
	/// </summary>
	public abstract class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr hObject);

		/// <summary>
		/// Create SafeObjectHandle
		/// </summary>
		/// <param name="ownsHandle">True if the class owns the handle</param>
		protected SafeObjectHandle(bool ownsHandle) : base(ownsHandle)
		{
		}

		/// <summary>
		/// Call DeleteObject
		/// </summary>
		/// <returns>true if this worked</returns>
		protected override bool ReleaseHandle()
		{
			return DeleteObject(handle);
		}
	}
}
