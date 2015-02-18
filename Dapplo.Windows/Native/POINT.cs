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
