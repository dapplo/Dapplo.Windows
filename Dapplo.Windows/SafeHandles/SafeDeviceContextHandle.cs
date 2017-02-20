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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

#endregion

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	///     A DeviceContext SafeHandle implementation
	/// </summary>
	public class SafeDeviceContextHandle : SafeDcHandle
	{
		private readonly Graphics _graphics;

		/// <summary>
		///     Default constructor is needed to support marshalling!!
		/// </summary>
		[SecurityCritical]
		public SafeDeviceContextHandle() : base(true)
		{
		}

		/// <summary>
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
		///     Create a SafeDeviceContextHandle from a Graphics object
		/// </summary>
		/// <param name="graphics">Graphics object</param>
		/// <returns>SafeDeviceContextHandle</returns>
		public static SafeDeviceContextHandle FromGraphics(Graphics graphics)
		{
			return new SafeDeviceContextHandle(graphics, graphics.GetHdc());
		}

		/// <summary>
		///     Call graphics.ReleaseHdc
		/// </summary>
		/// <returns>always true</returns>
		protected override bool ReleaseHandle()
		{
			_graphics.ReleaseHdc(handle);
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="newHandle"></param>
		/// <returns></returns>
		public SafeSelectObjectHandle SelectObject(SafeHandle newHandle)
		{
			return new SafeSelectObjectHandle(this, newHandle);
		}
	}
}