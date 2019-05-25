//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
{
	/// <summary>
	/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648381(v=vs.85).aspx"></a>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct CursorInfo
	{
		/// <summary>
		/// Size of the struct
		/// </summary>
		private int _cbSize;

		private readonly CursorInfoFlags _flags;
		private readonly IntPtr _hCursor;
		private readonly NativePoint _ptScreenPos;


		/// <summary>
		/// The cursor state, as CursorInfoFlags
		/// </summary>
		public CursorInfoFlags Flags => _flags;

		/// <summary>
		/// Handle (IntPtr) to the Cursor
		/// </summary>
		public IntPtr CursorHandle => _hCursor;

		/// <summary>
		/// A structure that receives the screen coordinates of the cursor.
		/// </summary>
		public NativePoint Location => _ptScreenPos;

		/// <summary>
		/// Factory for the structure
		/// </summary>
		public static CursorInfo Create()
		{
			var result = new CursorInfo
			{
				_cbSize = Marshal.SizeOf(typeof(CursorInfo))
			};
			return result;
		}
	}
}