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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

#endregion

namespace Dapplo.Windows.Common.Structs
{
    /// <summary>
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms534497(v=vs.85).aspx">RectF class</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RECTF
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        /// <summary>
        ///     Constructor from x,y,width,height
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        public RECTF(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        ///     X value
        /// </summary>
        public float X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        ///     X location of the rectangle
        /// </summary>
        public float Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        ///     Left value of the rectangle
        /// </summary>
        public float Left
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        ///     Top of the rectangle
        /// </summary>
        public float Top
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        ///     Right of the rectangle
        /// </summary>
        public float Right
        {
            get { return _x + _width; }
            set { _width = value - _x; }
        }

        /// <summary>
        ///     Bottom of the rectangle
        /// </summary>
        public float Bottom
        {
            get { return _y + _height; }
            set { _height = value - _y; }
        }

        /// <summary>
        ///     Heigh of the RECT
        /// </summary>
        public float Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        ///     Width of the RECT
        /// </summary>
        public float Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        ///     Location for this RECT
        /// </summary>
        public POINT Location
        {
            get { return new POINT((int) Left, (int) Top); }
            set
            {
                _x = value.X;
                _y = value.Y;
            }
        }

        /// <summary>
        ///     Size for this RECT
        /// </summary>
        public SIZE Size
        {
            get { return new SIZE((int) Width, (int) Height); }
            set
            {
                _width = value.Width + _x;
                _height = value.Height + _y;
            }
        }

        /// <summary>
        ///     Cast RECTF to Rect
        /// </summary>
        /// <param name="rectangle">RECT</param>
        /// <returns>Rect</returns>
        public static implicit operator Rect(RECTF rectangle)
        {
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Rect to RECTF
        /// </summary>
        /// <param name="rectangle">Rect</param>
        /// <returns>RECTF</returns>
        public static implicit operator RECTF(Rect rectangle)
        {
            return new RECTF((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Right, (int) rectangle.Bottom);
        }

        /// <summary>
        ///     Cast RECTF to RectangleF
        /// </summary>
        /// <param name="rectangle">RECT</param>
        /// <returns>Rectangle</returns>
        public static implicit operator RectangleF(RECTF rectangle)
        {
            return new RectangleF(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast RECTF to Rectangle
        /// </summary>
        /// <param name="rectangle">RECT</param>
        /// <returns>Rectangle</returns>
        public static implicit operator Rectangle(RECTF rectangle)
        {
            return new Rectangle((int) rectangle.X, (int) rectangle.Y, (int) rectangle.Width, (int) rectangle.Height);
        }

        /// <summary>
        ///     Cast RectangleF to RECTF
        /// </summary>
        /// <param name="rectangle">RectangleF</param>
        /// <returns>RECTF</returns>
        public static implicit operator RECTF(RectangleF rectangle)
        {
            return new RECTF(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Cast Rectangle to RECTF
        /// </summary>
        /// <param name="rectangle">Rectangle</param>
        /// <returns>RECTF</returns>
        public static implicit operator RECTF(Rectangle rectangle)
        {
            return new RECTF(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Equals for RECTF
        /// </summary>
        /// <param name="rectangle1">RECTF</param>
        /// <param name="rectangle2">RECTF</param>
        /// <returns>bool true if they are equal</returns>
        public static bool operator ==(RECTF rectangle1, RECTF rectangle2)
        {
            return rectangle1.Equals(rectangle2);
        }

        /// <summary>
        ///     Not is operator
        /// </summary>
        /// <param name="rectangle1">RECTF</param>
        /// <param name="rectangle2">RECTF</param>
        /// <returns>bool</returns>
        public static bool operator !=(RECTF rectangle1, RECTF rectangle2)
        {
            return !rectangle1.Equals(rectangle2);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "{Left: " + _x + "; " + "Top: " + _y + "; Right: " + _width + "; Bottom: " + _height + "}";
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
        public bool Equals(RECTF rectangle)
        {
            return Math.Abs(rectangle._x - _x) < float.Epsilon
                   && Math.Abs(rectangle._y - _y) < float.Epsilon
                   && Math.Abs(rectangle._width - _width) < float.Epsilon
                   && Math.Abs(rectangle._height - _height) < float.Epsilon;
        }

        /// <summary>
        ///     Checks if this RECT is empty
        /// </summary>
        /// <returns>true when empty</returns>
        public bool IsEmpty => Math.Abs(_width * _height) < float.Epsilon;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is RECTF)
            {
                return Equals((RECTF) obj);
            }
            if (obj is Rect)
            {
                RECTF rect = (Rect) obj;
                return Equals(rect);
            }
            if (obj is RectangleF)
            {
                RECTF rect = (RectangleF) obj;
                return Equals(rect);
            }
            return false;
        }

        /// <summary>
        ///     Test if this RECT contains the specified POINT
        /// </summary>
        /// <param name="point">POINT</param>
        /// <returns>true if it contains</returns>
        public bool Contains(POINT point)
        {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        ///     Empty RECT
        /// </summary>
        public static RECTF Empty => new RECTF();
    }
}