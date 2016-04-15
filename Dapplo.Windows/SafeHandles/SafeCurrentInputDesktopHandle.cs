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

using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Security.Permissions;
using Dapplo.LogFacade;

namespace Dapplo.Windows.SafeHandles
{
	/// <summary>
	/// A SafeHandle class implementation for the current input desktop
	/// </summary>
	public class SafeCurrentInputDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Default constructor, this opens the input destop with GENERIC_ALL
		/// </summary>
		public SafeCurrentInputDesktopHandle() : base(true)
		{
			var hDesktop = User32.OpenInputDesktop(0, true, DesktopAccessRight.GENERIC_ALL);
			if (hDesktop != IntPtr.Zero)
			{
				// Got desktop, store it as handle for the ReleaseHandle
				SetHandle(hDesktop);
				if (User32.SetThreadDesktop(hDesktop))
				{
					Log.Debug().WriteLine("Switched to desktop {0}", hDesktop);
				}
				else
				{
					Log.Warn().WriteLine("Couldn't switch to desktop {0}", hDesktop);
					Log.Error().WriteLine(User32.CreateWin32Exception("SetThreadDesktop"));
				}
			}
			else
			{
				Log.Warn().WriteLine("Couldn't get current desktop.");
				Log.Error().WriteLine(User32.CreateWin32Exception("OpenInputDesktop"));
			}
		}

		/// <summary>
		/// Close the desktop
		/// </summary>
		/// <returns>true if this succeeded</returns>
		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		protected override bool ReleaseHandle()
		{
			return User32.CloseDesktop(handle);
		}
	}
}
