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
	///     See <a href="http://pinvoke.net/default.aspx/Structures/SCROLLINFO.html">here</a>
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct ScrollInfo
	{
		/// <summary>
		///     Size of this struct
		/// </summary>
		public uint cbSize;

		/// <summary>
		///     Mask specifying which values to get
		/// </summary>
		public uint fMask;

		/// <summary>
		///     Minimum value to scroll to, e.g. the start
		/// </summary>
		public int nMin;

		/// <summary>
		///     Maximum value to scroll to, e.g. the end
		/// </summary>
		public int nMax;

		/// <summary>
		///     Size of a page
		/// </summary>
		public uint nPage;

		/// <summary>
		///     Current position
		/// </summary>
		public int nPos;

		/// <summary>
		///     Current tracking position
		/// </summary>
		public int nTrackPos;

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{{nMin = {nMin}; nMax = {nMax};nPage = {nPage};nPos = {nPos};nTrackPos = {nTrackPos};}}";
		}

		/// <summary>
		/// Create a ScrollInfo struct with the specified mask
		/// </summary>
		/// <param name="mask">ScrollInfoMask</param>
		public ScrollInfo(ScrollInfoMask mask)
		{
			cbSize = (uint) Marshal.SizeOf(typeof(ScrollInfo));
			fMask = (uint) mask;
			nMin = 0;
			nMax = 0;
			nPage = 0;
			nPos = 0;
			nTrackPos = 0;
		}
	}
}