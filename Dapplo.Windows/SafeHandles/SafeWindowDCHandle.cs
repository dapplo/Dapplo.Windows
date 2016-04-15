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
	public class SafeWindowDcHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetWindowDC(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

		private readonly IntPtr _hWnd;

		/// <summary>
		/// Create a SafeWindowDcHandle for an existing handöe
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <param name="existingDcHandle">IntPtr to the DC</param>
		[SecurityCritical]
		public SafeWindowDcHandle(IntPtr hWnd, IntPtr existingDcHandle) : base(true)
		{
			_hWnd = hWnd;
			SetHandle(existingDcHandle);
		}

		/// <summary>
		/// ReleaseDC for the original Window
		/// </summary>
		/// <returns>true if this worked</returns>
		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		protected override bool ReleaseHandle()
		{
			return ReleaseDC(_hWnd, handle);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static SafeWindowDcHandle FromDesktop()
		{
			var hWndDesktop = User32.GetDesktopWindow();
			var hDcDesktop = GetWindowDC(hWndDesktop);
			return new SafeWindowDcHandle(hWndDesktop, hDcDesktop);
		}
	}
}
