#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787535(v=vs.85).aspx">SCROLLBARINFO structure</a>
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct ScrollBarInfo
	{
		/// <summary>
		///     Size of this struct
		/// </summary>
		public uint cbSize;

		/// <summary>
		///     Coordinates of the scroll bar as specified in a RECT structure.
		/// </summary>
		public RECT rcScrollBar;

		/// <summary>
		///    Height or width of the thumb.
		/// </summary>
		public int dxyLineButton;

		/// <summary>
		///    Position of the bottom or right of the thumb.
		/// </summary>
		public int xyThumbBottom;

		/// <summary>
		///    Position of the top or left of the thumb.
		/// </summary>
		public int xyThumbTop;

		/// <summary>
		///    Reserved.
		/// </summary>
		public int reserved;

		/// <summary>
		///    An array of DWORD elements. Each element indicates the state of a scroll bar component, the element is specified via the ScrollBarStateIndexes.
		/// </summary>
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
		public ObjectStates[] states;

		/// <inheritdoc />
		public override string ToString()
		{
			var statesString = string.Join(",", states);
			return $"{{rcScrollBar = {rcScrollBar}; dxyLineButton = {dxyLineButton};xyThumbBottom = {xyThumbBottom};xyThumbTop = {xyThumbTop};states = {statesString};}}";
		}

		/// <summary>
		/// Create a ScrollInfo struct with the specified mask
		/// </summary>
		public ScrollBarInfo(bool? dummy = false)
		{
			cbSize = (uint) Marshal.SizeOf(typeof(ScrollBarInfo));
			states = new ObjectStates[6];
			rcScrollBar = new RECT();
			dxyLineButton = 0;
			xyThumbBottom = 0;
			xyThumbTop = 0;
			reserved = 0;
		}
	}
}