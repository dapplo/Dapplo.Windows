//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     See: https://msdn.microsoft.com/en-us/library/windows/desktop/dd162897.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
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
			: this((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Right, (int) rectangle.Bottom)
		{
		}

		public RECT(Rectangle rectangle) : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
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
			get { return _Left; }
			set { _Left = value; }
		}

		public int Y
		{
			get { return _Top; }
			set { _Top = value; }
		}

		public int Left
		{
			get { return _Left; }
			set { _Left = value; }
		}

		public int Top
		{
			get { return _Top; }
			set { _Top = value; }
		}

		public int Right
		{
			get { return _Right; }
			set { _Right = value; }
		}

		public int Bottom
		{
			get { return _Bottom; }
			set { _Bottom = value; }
		}

		/// <summary>
		///     Heigh of the RECT
		/// </summary>
		public int Height
		{
			get { return unchecked(_Bottom - _Top); }
			set { _Bottom = unchecked(value - _Top); }
		}

		/// <summary>
		///     Width of the RECT
		/// </summary>
		public int Width
		{
			get { return unchecked(_Right - _Left); }
			set { _Right = unchecked(value + _Left); }
		}

		/// <summary>
		///     Location for this RECT
		/// </summary>
		public Point Location
		{
			get { return new Point(Left, Top); }
			set
			{
				_Left = (int) value.X;
				_Top = (int) value.Y;
			}
		}

		/// <summary>
		///     Size for this RECT
		/// </summary>
		public Size Size
		{
			get { return new Size(Width, Height); }
			set
			{
				_Right = unchecked((int) value.Width + _Left);
				_Bottom = unchecked((int) value.Height + _Top);
			}
		}

		/// <summary>
		///     Cast RECT to Rect
		/// </summary>
		/// <param name="rectangle">RECT</param>
		/// <returns>Rect</returns>
		public static implicit operator Rect(RECT rectangle)
		{
			return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
		}

		/// <summary>
		///     Cast Rect to RECT
		/// </summary>
		/// <param name="rectangle">Rect</param>
		/// <returns>RECT</returns>
		public static implicit operator RECT(Rect rectangle)
		{
			return new RECT((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Right, (int) rectangle.Bottom);
		}

		/// <summary>
		///     Cast RECT to Rectangle
		/// </summary>
		/// <param name="rectangle">RECT</param>
		/// <returns>Rectangle</returns>
		public static implicit operator Rectangle(RECT rectangle)
		{
			return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
		}

		/// <summary>
		///     Cast Rectangle to RECT
		/// </summary>
		/// <param name="rectangle"></param>
		/// <returns>RECT</returns>
		public static implicit operator RECT(Rectangle rectangle)
		{
			return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		}

		/// <summary>
		///     Equals for RECT
		/// </summary>
		/// <param name="rectangle1">RECT</param>
		/// <param name="rectangle2">RECT</param>
		/// <returns>bool true if they are equal</returns>
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
			return (rectangle.Left == _Left) && (rectangle.Top == _Top) && (rectangle.Right == _Right) && (rectangle.Bottom == _Bottom);
		}

		public Rect ToRect()
		{
			if ((Width >= 0) && (Height >= 0))
			{
				return new Rect(Left, Top, Width, Height);
			}
			return Rect.Empty;
		}


		public Rectangle ToRectangle()
		{
			return new Rectangle(Left, Top, Width, Height);
		}

		public override bool Equals(object Object)
		{
			if (Object is RECT)
			{
				return Equals((RECT) Object);
			}
			if (Object is Rect)
			{
				return Equals(new RECT((Rect) Object));
			}
			if (Object is Rectangle)
			{
				return Equals(new RECT((Rectangle) Object));
			}
			return false;
		}
	}
}