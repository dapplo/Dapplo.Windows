#region Copyright (C) 2016-2019 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2019 Dapplo
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
#endregion

#region using

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Windows;
#endif
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
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public readonly struct NativeRect : IEquatable<NativeRect>
    {
        private readonly int _left;
        private readonly int _top;
        private readonly int _right;
        private readonly int _bottom;

        /// <summary>
        ///     Constructor from left, right, top, bottom
        /// </summary>
        /// <param name="left">int</param>
        /// <param name="top">int</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        public NativeRect(int left, int top, int width, int height)
        {
            _left = left;
            _top = top;
            _right = left + width;
            _bottom = top + height;
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
        ///     Constructor from topLeft and bottomRight
        /// </summary>
        /// <param name="topLeft">NativePoint</param>
        /// <param name="bottomRight">NativePoint</param>
        public NativeRect(NativePoint topLeft, NativePoint bottomRight) : this(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y)
        {
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
        public int X => _left;

        /// <summary>
        ///     X location of the NativeRect
        /// </summary>
        public int Y => _top;

        /// <summary>
        ///     Left value of the NativeRect
        /// </summary>
        public int Left => _left;

        /// <summary>
        ///     Top of the NativeRect
        /// </summary>
        public int Top => _top;

        /// <summary>
        ///     Right of the NativeRect
        /// </summary>
        public int Right => _right;

        /// <summary>
        ///     Bottom of the NativeRect
        /// </summary>
        public int Bottom => _bottom;

        /// <summary>
        ///     Height of the NativeRect
        /// </summary>
        public int Height => unchecked(_bottom - _top);

        /// <summary>
        ///     Width of the NativeRect
        /// </summary>
        public int Width => unchecked(_right - _left);

        /// <summary>
        ///     Location of this NativeRect
        /// </summary>
        public NativePoint Location => new NativePoint(Left, Top);

        /// <summary>
        ///     Size for this NativeRect
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

#if !NETSTANDARD2_0
        /// <summary>
        ///     Cast NativeRect to Rect
        /// </summary>
        /// <param name="rectangle">NativeRect</param>
        /// <returns>Rect</returns>
        public static implicit operator Rect(NativeRect rectangle)
        {
            return new Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }
    
        /// <summary>
        ///     Cast NativeRect to Int32Rect
        /// </summary>
        /// <param name="rectangle">NativeRect</param>
        /// <returns>Int32Rect</returns>
        public static implicit operator Int32Rect(NativeRect rectangle)
        {
            return new Int32Rect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Int32Rect to NativeRect
        /// </summary>
        /// <param name="rectangle">Int32Rect</param>
        /// <returns>NativeRect</returns>
        public static implicit operator NativeRect(Int32Rect rectangle)
        {
            return new NativeRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
#endif

        /// <summary>
        ///     Cast NativeRect to RectangleF
        /// </summary>
        /// <param name="rectangle">NativeRect</param>
        /// <returns>RectangleF</returns>
        public static implicit operator RectangleF(NativeRect rectangle)
        {
            return new RectangleF(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast NativeRect to Rectangle
        /// </summary>
        /// <param name="rectangle">NativeRect</param>
        /// <returns>Rectangle</returns>
        public static implicit operator Rectangle(NativeRect rectangle)
        {
            return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Cast Rectangle to NativeRect
        /// </summary>
        /// <param name="rectangle">Rectangle</param>
        /// <returns>NativeRect</returns>
        public static implicit operator NativeRect(Rectangle rectangle)
        {
            return new NativeRect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        ///     Equals for NativeRect
        /// </summary>
        /// <param name="rectangle1">NativeRect</param>
        /// <param name="rectangle2">NativeRect</param>
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
        [Pure]
        public override string ToString()
        {
            return $"{{Left: {_left}; Top: {_top}; Right: {_right}; Bottom: {_bottom};}}";
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">NativeRect</param>
        /// <returns>bool</returns>
        [Pure]
        public bool Equals(NativeRect other)
        {
            return other.Left == _left && other.Top == _top && other.Right == _right && other.Bottom == _bottom;
        }

        /// <summary>
        ///     Checks if this NativeRect is empty
        /// </summary>
        /// <returns>true when empty</returns>
        public bool IsEmpty => unchecked (Width * Height) == 0;

        /// <inheritdoc />
        [Pure]
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case NativeRect nativeRect:
                    return Equals(nativeRect);
                case Rectangle rectangle:
                    NativeRect rect = rectangle;
                    return Equals(rect);
            }

            return false;
        }

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _left;
                hashCode = (hashCode * 397) ^ _top;
                hashCode = (hashCode * 397) ^ _right;
                hashCode = (hashCode * 397) ^ _bottom;
                return hashCode;
            }
        }

        /// <summary>
        /// Decontructor for tuples
        /// </summary>
        /// <param name="location">NativePoint</param>
        /// <param name="size">NativeSize</param>
        [Pure]
        public void Deconstruct(out NativePoint location, out NativeSize size)
        {
            location = Location;
            size = Size;
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