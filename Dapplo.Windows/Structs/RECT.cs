/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
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

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd162897.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential), Serializable]
	public struct RECT
	{
		private int _Left;
		private int _Top;
		private int _Right;
		private int _Bottom;

		public RECT(RECT rectangle)
			: this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
		{
		}
		public RECT(Rect rectangle)
			: this((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Right, (int)rectangle.Bottom)
		{
		}
		public RECT(System.Drawing.Rectangle rectangle) : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
		{
		}
		public RECT(int left, int top, int right, int bottom)
		{
			_Left = left;
			_Top = top;
			_Right = right;
			_Bottom = bottom;
		}

		public int X
		{
			get
			{
				return _Left;
			}
			set
			{
				_Left = value;
			}
		}
		public int Y
		{
			get
			{
				return _Top;
			}
			set
			{
				_Top = value;
			}
		}
		public int Left
		{
			get
			{
				return _Left;
			}
			set
			{
				_Left = value;
			}
		}
		public int Top
		{
			get
			{
				return _Top;
			}
			set
			{
				_Top = value;
			}
		}
		public int Right
		{
			get
			{
				return _Right;
			}
			set
			{
				_Right = value;
			}
		}
		public int Bottom
		{
			get
			{
				return _Bottom;
			}
			set
			{
				_Bottom = value;
			}
		}
		public int Height
		{
			get
			{
				return unchecked(_Bottom - _Top);
			}
			set
			{
				_Bottom = unchecked(value - _Top);
			}
		}
		public int Width
		{
			get
			{
				return unchecked(_Right - _Left);
			}
			set
			{
				_Right = unchecked(value + _Left);
			}
		}
		public Point Location
		{
			get
			{
				return new Point(Left, Top);
			}
			set
			{
				_Left = (int)value.X;
				_Top = (int)value.Y;
			}
		}
		public Size Size
		{
			get
			{
				return new Size(Width, Height);
			}
			set
			{
				_Right = unchecked((int)value.Width + _Left);
				_Bottom = unchecked((int)value.Height + _Top);
			}
		}

		public static implicit operator Rect(RECT rectangle)
		{
			return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
		}
		public static implicit operator RECT(Rect rectangle)
		{
			return new RECT((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Right, (int)rectangle.Bottom);
		}

		public static implicit operator System.Drawing.Rectangle(RECT rectangle)
		{
			return new System.Drawing.Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
		}

		public static implicit operator RECT(System.Drawing.Rectangle rectangle)
		{
			return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		}

		public static bool operator ==(RECT rectangle1, RECT rectangle2)
		{
			return rectangle1.Equals(rectangle2);
		}
		public static bool operator !=(RECT rectangle1, RECT rectangle2)
		{
			return !rectangle1.Equals(rectangle2);
		}

		public override string ToString()
		{
			return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public bool Equals(RECT rectangle)
		{
			return rectangle.Left == _Left && rectangle.Top == _Top && rectangle.Right == _Right && rectangle.Bottom == _Bottom;
		}

		public Rect ToRect()
		{
			if (Width >= 0 && Height >= 0)
			{
				return new Rect(Left, Top, Width, Height);
			}
			return Rect.Empty;
		}


		public System.Drawing.Rectangle ToRectangle()
		{
			return new System.Drawing.Rectangle(Left, Top, Width, Height);
		}

		public override bool Equals(object Object)
		{
			if (Object is RECT)
			{
				return Equals((RECT)Object);
			}
			if (Object is Rect)
			{
				return Equals(new RECT((Rect)Object));
			}
			if (Object is System.Drawing.Rectangle)
			{
				return Equals(new RECT((System.Drawing.Rectangle)Object));
			}
			return false;
		}
	}
}
