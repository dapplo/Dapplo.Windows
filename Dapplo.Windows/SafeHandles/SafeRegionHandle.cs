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
using System.Security;
using Dapplo.Windows.Native;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A hRegion SafeHandle implementation
	/// </summary>
	public class SafeRegionHandle : SafeObjectHandle
	{
		/// <summary>
		/// Default constructor is needed to support marshalling!!
		/// </summary>
		[SecurityCritical]
		public SafeRegionHandle() : base(true)
		{
		}

		/// <summary>
		/// Create a SafeRegionHandle from an existing handle
		/// </summary>
		/// <param name="preexistingHandle">IntPtr to region</param>
		[SecurityCritical]
		public SafeRegionHandle(IntPtr preexistingHandle) : base(true)
		{
			SetHandle(preexistingHandle);
		}

		/// <summary>
		/// Directly call Gdi32.CreateRectRgn
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <returns>SafeRegionHandle</returns>
		public static SafeRegionHandle CreateRectRgn(int left, int top, int right, int bottom)
		{
			return Gdi32.CreateRectRgn(left, top, right, bottom);
		}
	}
}
