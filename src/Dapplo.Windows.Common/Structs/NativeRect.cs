//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Dapplo.Windows.Common.TypeConverters;

#endregion

namespace Dapplo.Windows.Common.Structs
{
    /// <summary>
    ///     NativeRect represents the native RECT structure for calling native methods.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd162897.aspx">RECT struct</a>
    ///     It has conversions from and to System.Drawing.Rectangle or System.Windows.Rect
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [TypeConverter(typeof(NativeRectTypeConverter))]
    public struct NativeRect
    {
        private int _left;
        private int _top;
        private int _right;
        private int _bottom;

        /// <summary>
        ///     Constructor from left, right, top, bottom
        /// </summary>
        /// <param name="left">int</param>
        /// <param name="top">int</param>
        /// <param name="right">int</param>
        /// <param name="bottom">int</param>
        public NativeRect(int left, int top, int right, int bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        /// <summary>
        ///     Constructor from location and size
        /// </summary>
        /// <param name="location">NativePoint</param>
        /// <param name="size">NativeSize</param>
        public NativeRect(NativePoint location, NativeSize size)
        {
            _left = location.X;
            _top = location.Y;
            _right = _left + size.Width;
            _bottom = _top + size.Height;
        }

        /// <summary>
        ///     Constructor from left, right and size
        /// </summary>
        /// <param name="left">int</param>
        /// <param name="top">int</param>
        /// <param name="size">NativeSize</param>
        public NativeRect(int left, int top, NativeSize size) : this(new NativePoint(left, top), size)
        {
        }

        /// <summary>
        ///     X value
        /// </summary>
        public int X
        {
            get { return _left; }
            set { _left = value; }
        }

        /// <summary>
        ///     X location of the rectangle
        /// </summary>
        public int Y
        {
            get { return _top; }
            set { _top = value; }
        }

        /// <summary>
        ///     Left value of the rectangle
        /// </summary>
        public int Left
        {
            get { return _left; }
            set { _left = value; }
        }

        /// <summary>
        ///     Top of the rectangle
        /// </summary>
        public int Top
        {
            get { return _top; }
            set { _top = value; }
        }

        /// <summary>
        ///     Right of the rectangle
        /// </summary>
        public int Right
        {
            get { return _right; }
            set { _right = value; }
        }

        /// <summary>
        ///     Bottom of the rectangle
        /// </summary>
        public int Bottom
        {
            get { return _bottom; }
            set { _bottom = value; }
        }

        /// <summary>
        ///     Heigh of the RECT
        /// </summary>
        public int Height
        {
            get { return unchecked(_bottom - _top); }
            set { _bottom = unchecked(value - _top); }
        }

        /// <summary>
        ///     Width of the RECT
        /// </summary>
        public int Width
        {
            get { return unchecked(_right - _left); }
            set { _right = unchecked(value + _left); }
        }

        /// <summary>
        ///     Location (for this RECT
        /// </summary>
        public NativePoint Location => new NativePoint(Left, Top);

        /// <summary>
        ///     Size for this RECT
        /// </summary>
        public NativeSize Size => new NativeSize(Width, Height);

        /// <summary>
        ///     Coordinates of the bottom left
        /// </summary>
        public NativePoint BottomLeft => new NativePoint(X, Y + Height);

        /// <summary>
        ///     Coordinates of the top left
        /// </summary>
        public NativePoint TopLeft => new NativePoint(X, Y);

        /// <summary>
        ///     Coordinates of the bottom right
        /// </summary>
        public NativePoint BottomRight => new NativePoint(X + Width, Y + Height);

        /// <summary>
        ///     Coordinates of the top right
        /// </summary>
        public NativePoint TopRight => new NativePoint(X + Width, Y);

        /// <summary>
        ///     Cast RECT to Rect
        /// </summary>
        /// <param name="rectangle">RECT</param>
        /// <returns>Rect</returns>
        public static implicit operator Rect(NativeRect rectangle)
        {
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Rect to RECT
        /// </summary>
        /// <param name="rectangle">Rect</param>
        /// <returns>RECT</returns>
        public static implicit operator NativeRect(Rect rectangle)
        {
            return new NativeRect((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Right, (int) rectangle.Bottom);
        }

        /// <summary>
        ///     Cast RECT to Rectangle
        /// </summary>
        /// <param name="rectangle">RECT</param>
        /// <returns>Rectangle</returns>
        public static implicit operator Rectangle(NativeRect rectangle)
        {
            return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Rectangle to RECT
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns>RECT</returns>
        public static implicit operator NativeRect(Rectangle rectangle)
        {
            return new NativeRect(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Equals for RECT
        /// </summary>
        /// <param name="rectangle1">RECT</param>
        /// <param name="rectangle2">RECT</param>
        /// <returns>bool true if they are equal</returns>
        public static bool operator ==(NativeRect rectangle1, NativeRect rectangle2)
        {
            return rectangle1.Equals(rectangle2);
        }

        /// <summary>
        ///     Not is operator
        /// </summary>
        /// <param name="rectangle1"></param>
        /// <param name="rectangle2"></param>
        /// <returns>bool</returns>
        public static bool operator !=(NativeRect rectangle1, NativeRect rectangle2)
        {
            return !rectangle1.Equals(rectangle2);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{Left: {_left}; Top: {_top}; Right: {_right}; Bottom: {_bottom};}}";
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        ///     Equalss
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns>bool</returns>
        public bool Equals(NativeRect rectangle)
        {
            return rectangle.Left == _left && rectangle.Top == _top && rectangle.Right == _right && rectangle.Bottom == _bottom;
        }

        /// <summary>
        ///     Checks if this RECT is empty
        /// </summary>
        /// <returns>true when empty</returns>
        public bool IsEmpty => Width * Height == 0;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is NativeRect)
            {
                return Equals((NativeRect) obj);
            }
            if (obj is Rect)
            {
                NativeRect rect = (Rect) obj;
                return Equals(rect);
            }
            if (obj is Rectangle)
            {
                NativeRect rect = (Rectangle) obj;
                return Equals(rect);
            }
            return false;
        }

        /// <summary>
        ///     Empty NativeRect
        /// </summary>
        public static NativeRect Empty { get; } = new NativeRect();

        /// <summary>
        ///     SizeOf for this struct
        /// </summary>
        public static int SizeOf => Marshal.SizeOf(typeof(NativeRect));
    }
}