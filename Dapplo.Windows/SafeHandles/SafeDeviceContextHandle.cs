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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A DeviceContext SafeHandle implementation
	/// </summary>
	public class SafeDeviceContextHandle : SafeDcHandle
	{
		private Graphics _graphics;

		/// <summary>
		/// Default constructor is needed to support marshalling!!
		/// </summary>
		[SecurityCritical]
		public SafeDeviceContextHandle() : base(true)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="preexistingHandle"></param>
		[SecurityCritical]
		public SafeDeviceContextHandle(Graphics graphics, IntPtr preexistingHandle) : base(true)
		{
			_graphics = graphics;
			SetHandle(preexistingHandle);
		}

		/// <summary>
		/// Call graphics.ReleaseHdc
		/// </summary>
		/// <returns>always true</returns>
		protected override bool ReleaseHandle()
		{
			_graphics.ReleaseHdc(handle);
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newHandle"></param>
		/// <returns></returns>
		public SafeSelectObjectHandle SelectObject(SafeHandle newHandle)
		{
			return new SafeSelectObjectHandle(this, newHandle);
		}

		/// <summary>
		/// Create a SafeDeviceContextHandle from a Graphics object
		/// </summary>
		/// <param name="graphics">Graphics object</param>
		/// <returns>SafeDeviceContextHandle</returns>
		public static SafeDeviceContextHandle FromGraphics(Graphics graphics)
		{
			return new SafeDeviceContextHandle(graphics, graphics.GetHdc());
		}
	}
}
