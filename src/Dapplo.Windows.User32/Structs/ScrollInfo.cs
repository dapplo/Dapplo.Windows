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
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
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
		private uint _cbSize;

		/// <summary>
		///     Mask specifying which values to get
		/// </summary>
		private ScrollInfoMask _fMask;
		private int _nMin;
		private int _nMax;
		private uint _nPage;
		private int _nPos;
		private int _nTrackPos;

		/// <summary>
		///     Minimum value to scroll to, e.g. the start
		/// </summary>
		public int Minimum => _nMin;


		/// <summary>
		///     Maximum value to scroll to, e.g. the end
		/// </summary>
		public int Maximum => _nMax;


		/// <summary>
		///     Size of a page
		/// </summary>
		public uint PageSize => _nPage;

		/// <summary>
		///     Current position
		/// </summary>
		public int Position
		{
			get
			{
				return _nPos;
			}
			set
			{
				_nPos = value;
			}
		}


		/// <summary>
		///     Current tracking position
		/// </summary>
		public int TrackingPosition
		{
			get
			{
				return _nTrackPos;
			}
			set
			{
				_nTrackPos = value;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{{Minimum = {Minimum}; Maximum = {Maximum};PageSize = {PageSize};Position = {Position};TrackingPosition = {TrackingPosition};}}";
		}

		/// <summary>
		///     Create a ScrollInfo struct with the specified mask
		/// </summary>
		/// <param name="mask">ScrollInfoMask</param>
		public static ScrollInfo Create(ScrollInfoMask mask)
		{
			return new ScrollInfo
			{
				_cbSize = (uint)Marshal.SizeOf(typeof(ScrollInfo)),
				_fMask = mask,
				_nMin = 0,
				_nMax = 0,
				_nPage = 0,
				_nPos = 0,
				_nTrackPos = 0
			};
		}
	}
}