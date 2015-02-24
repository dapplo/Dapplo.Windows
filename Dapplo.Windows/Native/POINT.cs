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
using System.Windows;

namespace Dapplo.Windows.Native {
	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct POINT {
		public int X;
		public int Y;

		public POINT(int x, int y) {
			X = x;
			Y = y;
		}
		public POINT(Point point) {
			X = (int)point.X;
			Y = (int)point.Y;
		}

		public static implicit operator Point(POINT p) {
			return new Point(p.X, p.Y);
		}

		public static implicit operator POINT(Point p) {
			return new POINT((int)p.X, (int)p.Y);
		}

		public Point ToPoint() {
			return new Point(X, Y);
		}

		override public string ToString() {
			return X + "," + Y;
		}
	}
}
