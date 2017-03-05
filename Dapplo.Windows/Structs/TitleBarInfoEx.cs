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
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     The structure for the TITLEBARINFOEX
	///     See
	///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969233(v=vs.85).aspx">TITLEBARINFOEX struct</a>
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct TitleBarInfoEx
	{
		/// <summary>
		///     The size of the structure, in bytes.
		///     The caller must set this member to sizeof(TITLEBARINFOEX).
		/// </summary>
		private uint _cbSize;

		private RECT _rcTitleBar;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		private ObjectStates[] _rgState;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		private RECT[] _rgRect;


		/// <summary>
		///     Factory method for a default TitleBarInfoEx.
		/// </summary>
		public static TitleBarInfoEx Create()
		{
			return new TitleBarInfoEx
			{
				_cbSize = (uint) Marshal.SizeOf(typeof(TitleBarInfoEx)),
				_rgState = new ObjectStates[6],
				_rgRect = new RECT[6],
				_rcTitleBar = RECT.Empty
			};
		}
	}
}