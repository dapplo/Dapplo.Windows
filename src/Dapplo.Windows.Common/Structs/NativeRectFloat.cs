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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
#if !NETSTANDARD2_0
using System.Windows;
#endif
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
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public readonly struct NativeRectFloat : IEquatable<NativeRectFloat>
    {
        private readonly float _x;
        private readonly float _y;
        private readonly float _width;
        private readonly float _height;

        /// <summary>
        ///     Constructor from left, top, width, height
        /// </summary>
        /// <param name="left">float</param>
        /// <param name="top">float</param>
        /// <param name="width">float</param>
        /// <param name="height">float</param>
        public NativeRectFloat(float left, float top, float width, float height)
        {
            _x = left;
            _y = top;
            _width = width;
            _height = height;
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
        ///     Constructor from top-left, bottom right
        /// </summary>
        /// <param name="topLeft">NativePointFloat</param>
        /// <param name="bottomRight">NativePointFloat</param>
        public NativeRectFloat(NativePointFloat topLeft, NativePointFloat bottomRight) : this(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y)
        {
        }

        /// <summary>
        ///     Constructor from location, size
        /// </summary>
        /// <param name="location">NativePoint</param>
        /// <param name="nativeSizeFloat">NativeSizeFloat</param>
        public NativeRectFloat(NativePointFloat location, NativeSizeFloat nativeSizeFloat)
        {
            _x = location.X;
            _y = location.Y;
            _width = nativeSizeFloat.Width;
            _height = nativeSizeFloat.Height;
        }

        /// <summary>
        ///     X value
        /// </summary>
        public float X => _x;

        /// <summary>
        ///     X location of the rectangle
        /// </summary>
        public float Y => _y;

        /// <summary>
        ///     Left value of the rectangle
        /// </summary>
        public float Left => _x;

        /// <summary>
        ///     Top of the rectangle
        /// </summary>
        public float Top => _y;

        /// <summary>
        ///     Right of the rectangle
        /// </summary>
        public float Right => _x + _width;

        /// <summary>
        ///     Bottom of the rectangle
        /// </summary>
        public float Bottom => _y + _height;

        /// <summary>
        ///     Heigh of the NativeRectFloat
        /// </summary>
        public float Height => _height;

        /// <summary>
        ///     Width of the NativeRectFloat
        /// </summary>
        public float Width => _width;

        /// <summary>
        ///     Coordinates of the bottom left
        /// </summary>
        public NativePointFloat BottomLeft => new NativePointFloat(X, Y + Height);

        /// <summary>
        ///     Coordinates of the top left
        /// </summary>
        public NativePointFloat TopLeft => new NativePointFloat(X, Y);

        /// <summary>
        ///     Coordinates of the bottom right
        /// </summary>
        public NativePointFloat BottomRight => new NativePointFloat(X + Width, Y + Height);

        /// <summary>
        ///     Coordinates of the top right
        /// </summary>
        public NativePointFloat TopRight => new NativePointFloat(X + Width, Y);

        /// <summary>
        ///     Location for this NativeRectFloat
        /// </summary>
        public NativePointFloat Location => new NativePointFloat(Left, Top);

        /// <summary>
        ///     Size for this NativeRectFloat
        /// </summary>
        public NativeSizeFloat Size => new NativeSizeFloat(Width, Height);

        /// <summary>
        ///     Cast NativeRect to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">NativeRect</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(NativeRect rectangle)
        {
            return new NativeRectFloat(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }

#if !NETSTANDARD2_0
        /// <summary>
        ///     Cast Rect to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">Rect</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(Rect rectangle)
        {
            return new NativeRectFloat((float)rectangle.Left, (float)rectangle.Top, (float)rectangle.Right, (float)rectangle.Bottom);
        }

        /// <summary>
        ///     Cast Int32Rect to NativeRectFloat
        /// </summary>
        /// <param name="rectangle">Int32Rect</param>
        /// <returns>NativeRectFloat</returns>
        public static implicit operator NativeRectFloat(Int32Rect rectangle)
        {
            return new NativeRectFloat(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
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
        ///     Cast NativeRectFloat to Int32Rect
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>Int32Rect</returns>
        public static implicit operator Int32Rect(NativeRectFloat rectangle)
        {
            return new Int32Rect((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Width, (int)rectangle.Height);
        }
#endif


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
        ///     Cast NativeRectFloat to NativeRect
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>NativeRect</returns>
        public static implicit operator NativeRect(NativeRectFloat rectangle)
        {
            return new NativeRect((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Width, (int)rectangle.Height);
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
        [Pure]
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("Left: ").Append(_x).Append("; ");
            builder.Append("Top: ").Append(_y).Append("; ");
            builder.Append("Right: ").Append(Right).Append("; ");
            builder.Append("Bottom: ").Append(Bottom).Append("}");
            return builder.ToString();
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="rectangle">NativeRectFloat</param>
        /// <returns>bool</returns>
        [Pure]
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
        [Pure]
        public bool IsEmpty => Math.Abs(_width * _height) < float.Epsilon;

        /// <inheritdoc />
        [Pure]
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case NativeRectFloat f:
                    return Equals(f);
#if !NETSTANDARD2_0
                case Rect rect1:
                    return Equals(rect1);
#endif
                case RectangleF rectangleF:
                    return Equals(rectangleF);
            }

            return false;
        }

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _x.GetHashCode();
                hashCode = (hashCode * 397) ^ _x.GetHashCode();
                hashCode = (hashCode * 397) ^ _y.GetHashCode();
                hashCode = (hashCode * 397) ^ _width.GetHashCode();
                hashCode = (hashCode * 397) ^ _height.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Test if this NativeRectFloat contains the specified NativePoint
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <returns>true if it contains</returns>
        [Pure]
        public bool Contains(NativePoint point)
        {
            return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        }

        /// <summary>
        /// Decontructor for tuples
        /// </summary>
        /// <param name="location">NativePointFloat</param>
        /// <param name="size">NativeSizeFloat</param>
        public void Deconstruct(out NativePointFloat location, out NativeSizeFloat size)
        {
            location = Location;
            size = Size;
        }

        /// <summary>
        ///     Empty NativeRectFloat
        /// </summary>
        public static NativeRectFloat Empty { get; } = new NativeRectFloat();
    }
}