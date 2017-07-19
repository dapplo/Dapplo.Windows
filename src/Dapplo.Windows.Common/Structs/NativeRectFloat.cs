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
    ///     NativeRect represents the native RECTF structure for calling native methods.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms534497(v=vs.85).aspx">RectF class</a>
    ///     It has conversions from and to System.Drawing.RectangleF or System.Windows.Rect
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [TypeConverter(typeof(NativeRectFloatTypeConverter))]
    public struct NativeRectFloat
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        /// <summary>
        ///     Constructor from left, top, right, bottom
        /// </summary>
        /// <param name="left">float</param>
        /// <param name="top">float</param>
        /// <param name="right">float</param>
        /// <param name="bottom">float</param>
        public NativeRectFloat(float left, float top, float right, float bottom)
        {
            _x = left;
            _y = top;
            _width = right - left;
            _height = bottom - top;
        }

        /// <summary>
        ///     Constructor from x,y, (width,height)
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <param name="nativeSizeFloat">NativeSizeFloat</param>
        public NativeRectFloat(float x, float y, NativeSizeFloat nativeSizeFloat)
        {
            _x = x;
            _y = y;
            _width = nativeSizeFloat.Width;
            _height = nativeSizeFloat.Height;
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
        ///     Heigh of the NativeRectFloat
        /// </summary>
        public float Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        ///     Width of the NativeRectFloat
        /// </summary>
        public float Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        ///     Location for this NativeRectFloat
        /// </summary>
        public NativePoint Location
        {
            get { return new NativePoint((int) Left, (int) Top); }
            set
            {
                _x = value.X;
                _y = value.Y;
            }
        }

        /// <summary>
        ///     Size for this NativeRectFloat
        /// </summary>
        public NativeSize Size
        {
            get { return new NativeSize((int) Width, (int) Height); }
            set
            {
                _width = value.Width + _x;
                _height = value.Height + _y;
            }
        }

        /// <summary>
        ///     Cast NativeRectFloat to Rect
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>Rect</returns>
        public static implicit operator Rect(NativeRectFloat rectangle)
        {
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Rect to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">Rect</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(Rect rectangle)
        {
            return new NativeRectFloat((int) rectangle.Left, (int) rectangle.Top, (int) rectangle.Right, (int) rectangle.Bottom);
        }

        /// <summary>
        ///     Cast NativeRectFloat to RectangleF
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>RectangleF</returns>
        public static implicit operator RectangleF(NativeRectFloat rectangle)
        {
            return new RectangleF(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast NativeRectFloat to Rectangle
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>Rectangle</returns>
        public static implicit operator Rectangle(NativeRectFloat rectangle)
        {
            return new Rectangle((int) rectangle.X, (int) rectangle.Y, (int) rectangle.Width, (int) rectangle.Height);
        }

        /// <summary>
        ///     Cast RectangleF to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">RectangleF</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(RectangleF rectangle)
        {
            return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Cast Rectangle to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">Rectangle</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(Rectangle rectangle)
        {
            return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

        /// <summary>
        ///     Equals for NativeRectFloat
        /// </summary>
        /// <param name="rectangle1">NativeRectFloat</param>
        /// <param name="rectangle2">NativeRectFloat</param>
        /// <returns>bool true if they are equal</returns>
        public static bool operator ==(NativeRectFloat rectangle1, NativeRectFloat rectangle2)
        {
            return rectangle1.Equals(rectangle2);
        }

        /// <summary>
        ///     Not is operator
        /// </summary>
        /// <param name="rectangle1">NativeRectFloat</param>
        /// <param name="rectangle2">NativeRectFloat</param>
        /// <returns>bool</returns>
        public static bool operator !=(NativeRectFloat rectangle1, NativeRectFloat rectangle2)
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
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>bool</returns>
        public bool Equals(NativeRectFloat rectangle)
        {
            return Math.Abs(rectangle._x - _x) < float.Epsilon
                   && Math.Abs(rectangle._y - _y) < float.Epsilon
                   && Math.Abs(rectangle._width - _width) < float.Epsilon
                   && Math.Abs(rectangle._height - _height) < float.Epsilon;
        }

        /// <summary>
        ///     Checks if this NativeRectFloat is empty
        /// </summary>
        /// <returns>true when empty</returns>
        public bool IsEmpty => Math.Abs(_width * _height) < float.Epsilon;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is NativeRectFloat)
            {
                return Equals((NativeRectFloat) obj);
            }
            if (obj is Rect)
            {
                NativeRectFloat rect = (Rect) obj;
                return Equals(rect);
            }
            if (obj is RectangleF)
            {
                NativeRectFloat rect = (RectangleF) obj;
                return Equals(rect);
            }
            return false;
        }

        /// <summary>
        ///     Test if this NativeRectFloat contains the specified NativePoint
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <returns>true if it contains</returns>
        public bool Contains(NativePoint point)
        {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        ///     Empty NativeRectFloat
        /// </summary>
        public static NativeRectFloat Empty { get; } = new NativeRectFloat();
    }
}