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

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// The structure for the WindowInfo
	/// See: http://msdn.microsoft.com/en-us/library/windows/desktop/ms632610.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential), Serializable]
	internal struct WINDOWINFO {
		public uint cbSize;
		public RECT rcWindow;
		public RECT rcClient;
		public uint dwStyle;
		public uint dwExStyle;
		public uint dwWindowStatus;
		public uint cxWindowBorders;
		public uint cyWindowBorders;
		public ushort atomWindowType;
		public ushort wCreatorVersion;
		// Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
		public WINDOWINFO(bool? filler)
			: this() {
			cbSize = (uint)(Marshal.SizeOf(typeof(WINDOWINFO)));
		}
	}
}
