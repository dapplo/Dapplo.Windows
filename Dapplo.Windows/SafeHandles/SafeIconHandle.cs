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
using System.Drawing;
using System.Security.Permissions;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A SafeHandle class implementation for the hIcon
	/// </summary>
	public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		/// <summary>
		/// Create a SafeIconHandle from a bitmap by calling GetHicon
		/// </summary>
		/// <param name="bitmap">Bitmap</param>
		public SafeIconHandle(Bitmap bitmap) : base(true)
		{
			SetHandle(bitmap.GetHicon());
		}
	
		/// <summary>
		/// Create a SafeIconHandle from a hIcon
		/// </summary>
		/// <param name="hIcon">IntPtr to an icon</param>
		public SafeIconHandle(IntPtr hIcon) : base(true)
		{
			SetHandle(hIcon);
		}
		/// <summary>
		/// Call destroy icon
		/// </summary>
		/// <returns>true if this worked</returns>
		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		protected override bool ReleaseHandle()
		{
			return User32.DestroyIcon(handle);
		}
	}
}
