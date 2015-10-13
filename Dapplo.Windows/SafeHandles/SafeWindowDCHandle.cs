/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
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

using Dapplo.Windows.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A WindowDC SafeHandle implementation
	/// </summary>
	public class SafeWindowDCHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetWindowDC(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		private IntPtr hWnd;
		[SecurityCritical]
		private SafeWindowDCHandle() : base(true)
		{
		}

		[SecurityCritical]
		public SafeWindowDCHandle(IntPtr hWnd, IntPtr preexistingHandle) : base(true)
		{
			this.hWnd = hWnd;
			SetHandle(preexistingHandle);
		}

		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		protected override bool ReleaseHandle()
		{
			bool returnValue = ReleaseDC(hWnd, handle);
			return returnValue;
		}

		public static SafeWindowDCHandle fromDesktop()
		{
			IntPtr hWndDesktop = User32.GetDesktopWindow();
			IntPtr hDCDesktop = GetWindowDC(hWndDesktop);
			return new SafeWindowDCHandle(hWndDesktop, hDCDesktop);
		}
	}
}
