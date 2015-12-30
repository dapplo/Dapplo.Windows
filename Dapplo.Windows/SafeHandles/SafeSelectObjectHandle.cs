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
	/// This impl will select the passed SafeHandle to the HDC and replace the returned value when disposing
	/// </summary>
	public class SafeSelectObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[DllImport("gdi32.dll", SetLastError = true)]
		private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

		private SafeHandle hdc;

		[SecurityCritical]
		private SafeSelectObjectHandle() : base(true)
		{
		}

		[SecurityCritical]
		public SafeSelectObjectHandle(SafeDCHandle hdc, SafeHandle newHandle) : base(true)
		{
			this.hdc = hdc;
			SetHandle(SelectObject(hdc.DangerousGetHandle(), newHandle.DangerousGetHandle()));
		}

		protected override bool ReleaseHandle()
		{
			SelectObject(hdc.DangerousGetHandle(), handle);
			return true;
		}
	}
}
