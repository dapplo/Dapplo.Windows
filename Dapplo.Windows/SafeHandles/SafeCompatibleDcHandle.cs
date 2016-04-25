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

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A CompatibleDC SafeHandle implementation
	/// </summary>
	public class SafeCompatibleDcHandle : SafeDcHandle
	{
		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteDC(IntPtr hDc);

		/// <summary>
		/// Default constructor is needed to support marshalling!!
		/// </summary>
		[SecurityCritical]
		public SafeCompatibleDcHandle() : base(true)
		{
		}

		/// <summary>
		/// Create SafeCompatibleDcHandle from existing handle
		/// </summary>
		/// <param name="preexistingHandle">IntPtr with existing handle</param>
		[SecurityCritical]
		public SafeCompatibleDcHandle(IntPtr preexistingHandle) : base(true)
		{
			SetHandle(preexistingHandle);
		}

		/// <summary>
		/// Select an object onto the DC
		/// </summary>
		/// <param name="objectSafeHandle">SafeHandle for object</param>
		/// <returns>SafeSelectObjectHandle</returns>
		public SafeSelectObjectHandle SelectObject(SafeHandle objectSafeHandle)
		{
			return new SafeSelectObjectHandle(this, objectSafeHandle);
		}

		/// <summary>
		/// Call DeleteDC, this disposes the unmanaged resources
		/// </summary>
		/// <returns>bool true if the DC was deleted</returns>
		protected override bool ReleaseHandle()
		{
			return DeleteDC(handle);
		}
	}
}
