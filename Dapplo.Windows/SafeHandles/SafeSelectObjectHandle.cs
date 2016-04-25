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
using System.Security;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A select object safehandle implementation
	/// This will select the passed SafeHandle to the HDC and replace the returned value when disposing
	/// </summary>
	public class SafeSelectObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		/// <summary>
		/// The SelectObject function selects an object into the specified device context (DC).
		/// The new object replaces the previous object of the same type.
		/// </summary>
		/// <param name="hDc">IntPtr to DC</param>
		/// <param name="hObject">IntPtr to the Object</param>
		/// <returns></returns>
		[DllImport("gdi32.dll", SetLastError = true)]
		private static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

		private readonly SafeHandle _hdc;

		/// <summary>
		/// Default constructor is needed to support marshalling!!
		/// </summary>
		[SecurityCritical]
		public SafeSelectObjectHandle() : base(true)
		{
		}

		/// <summary>
		/// Constructor for the SafeSelectObjectHandle
		/// </summary>
		/// <param name="hdc">SafeHandle for the DC</param>
		/// <param name="newObjectSafeHandle">SafeHandle to the object which is select to the DC</param>
		[SecurityCritical]
		public SafeSelectObjectHandle(SafeHandle hdc, SafeHandle newObjectSafeHandle) : base(true)
		{
			_hdc = hdc;
			SetHandle(SelectObject(hdc.DangerousGetHandle(), newObjectSafeHandle.DangerousGetHandle()));
		}

		/// <summary>
		/// Place the original object back on the DC
		/// </summary>
		/// <returns>allways true (except for exceptions)</returns>
		protected override bool ReleaseHandle()
		{
			SelectObject(_hdc.DangerousGetHandle(), handle);
			return true;
		}
	}
}
