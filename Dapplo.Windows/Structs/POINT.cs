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
		public int X;
		public int Y;

		public POINT(int x, int y)
		{
			X = x;
			Y = y;
		}

		public POINT(Point point)
		{
			X = (int) point.X;
			Y = (int) point.Y;
		}

		public POINT(System.Drawing.Point point)
		{
			X = point.X;
			Y = point.Y;
		}

		public static implicit operator Point(POINT p)
		{
			return new Point(p.X, p.Y);
		}

		public static implicit operator System.Drawing.Point(POINT p)
		{
			return new System.Drawing.Point(p.X, p.Y);
		}

		public static implicit operator POINT(Point p)
		{
			return new POINT((int) p.X, (int) p.Y);
		}

		/// <summary>
		/// Create System.Drawing.Point or System.Windows.Point depending on the generic type
		/// </summary>
		/// <typeparam name="TPoint">Type for the point</typeparam>
		/// <returns>S.D.Point or S.W.Point</returns>
		public TPoint ToPoint<TPoint>()
		{
			if (typeof(TPoint) == typeof(Point))
			{
				return (TPoint)(object)new Point(X, Y);
			}
			return (TPoint)(object)new System.Drawing.Point(X, Y);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return X + "," + Y;
		}
	}
}