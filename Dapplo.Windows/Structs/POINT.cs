#region Dapplo 2017 - GNU Lesser General Public License

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
using System.Windows;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// POINT structure for calling native methods
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
	public struct POINT
	{
		private int _x;
		private int _y;

		/// <summary>
		/// The X coordinate
		/// </summary>
		public int X
		{
			get { return _x; }
			set { _x = value; }
		}

		/// <summary>
		/// The Y coordinate
		/// </summary>
		public int Y
		{
			get { return _y; }
			set { _y = value; }
		}

		/// <summary>
		/// Constructor with x and y coordinates
		/// </summary>
		/// <param name="x">int</param>
		/// <param name="y">int</param>
		public POINT(int x, int y)
		{
			_x = x;
			_y = y;
		}

		/// <summary>
		/// Implicit cast from POINT to Point
		/// </summary>
		/// <param name="point">POINT</param>
		public static implicit operator Point(POINT point)
		{
			return new Point(point.X, point.Y);
		}

		/// <summary>
		/// Implicit cast from POINT to System.Drawing.Point
		/// </summary>
		/// <param name="point">POINT</param>
		public static implicit operator System.Drawing.Point(POINT point)
		{
			return new System.Drawing.Point(point.X, point.Y);
		}

		/// <summary>
		/// Implicit cast from Point to POINT
		/// </summary>
		/// <param name="point">Point</param>
		public static implicit operator POINT(Point point)
		{
			return new POINT((int) point.X, (int) point.Y);
		}

		/// <summary>
		/// Implicit cast from System.Drawing.Point to POINT
		/// </summary>
		/// <param name="point">System.Drawing.Point</param>
		public static implicit operator POINT(System.Drawing.Point point)
		{
			return new POINT(point.X, point.Y);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return X + "," + Y;
		}
	}
}