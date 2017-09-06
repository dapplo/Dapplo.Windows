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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using Dapplo.Windows.Common.TypeConverters;

#endregion

namespace Dapplo.Windows.Common.Structs
{
    /// <summary>
    ///     NativePoint represents the native POINT structure for calling native methods.
    ///     It has conversions from and to System.Drawing.Point or System.Windows.Point
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [TypeConverter(typeof(NativePointTypeConverter))]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public struct NativePoint : IEquatable<NativePoint>
    {
        private readonly int _x;
        private readonly int _y;

        /// <summary>
        ///     The X coordinate
        /// </summary>
        public int X => _x;

        /// <summary>
        ///     The Y coordinate
        /// </summary>
        public int Y => _y;

        /// <summary>
        ///     Constructor with x and y coordinates
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        public NativePoint(int x, int y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Create a new NativePoint of this with the specified offset
        /// </summary>
        /// <param name="x">int with x offset</param>
        /// <param name="y">int with y offset</param>
        /// <returns>NativePoint</returns>
        public NativePoint Offset(int x, int y)
        {
            return new NativePoint(X + x, Y + y);
        }

        /// <summary>
        /// Create a new NativePoint of this with the specified offset
        /// </summary>
        /// <param name="point">NativePoint</param>
        /// <returns>NativePoint</returns>
        public NativePoint Offset(NativePoint point)
        {
            return new NativePoint(X + point.X, Y + point.Y);
        }

        /// <summary>
        ///     Implicit cast from NativePoint to Point
        /// </summary>
        /// <param name="point">NativePoint</param>
        public static implicit operator Point(NativePoint point)
        {
            return new Point(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from NativePoint to System.Drawing.Point
        /// </summary>
        /// <param name="point">NativePoint</param>
        public static implicit operator System.Drawing.Point(NativePoint point)
        {
            return new System.Drawing.Point(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.Point to NativePoint
        /// </summary>
        /// <param name="point">System.Drawing.Point</param>
        public static implicit operator NativePoint(System.Drawing.Point point)
        {
            return new NativePoint(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.PointF to NativePoint
        /// </summary>
        /// <param name="point">System.Drawing.PointF</param>
        public static implicit operator NativePoint(System.Drawing.PointF point)
        {
            return new NativePoint((int) point.X, (int) point.Y);
        }

        /// <summary>
        /// Implicit cast from NativePointFloat to NativePoint
        /// </summary>
        /// <param name="nativePointFloat">NativePointFloat</param>
        public static implicit operator NativePoint(NativePointFloat nativePointFloat)
        {
            return new NativePoint((int)nativePointFloat.X, (int)nativePointFloat.Y);
        }

        /// <summary>
        /// Equal
        /// </summary>
        /// <param name="point1">NativePoint</param>
        /// <param name="point2">NativePoint</param>
        /// <returns>rue if equal</returns>
        public static bool operator ==(NativePoint point1, NativePoint point2)
        {
            return point1.Equals(point2);
        }

        /// <summary>
        /// Not equal
        /// </summary>
        /// <param name="point1">NativePoint</param>
        /// <param name="point2">NativePoint</param>
        /// <returns>false if the values are equal</returns>
        public static bool operator !=(NativePoint point1, NativePoint point2)
        {
            return !(point1 == point2);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (_x * 397) ^ _y;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return X + "," + Y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is NativePoint)
            {
                return Equals((NativePoint)obj);
            }
            if (obj is System.Drawing.Point)
            {
                NativePoint rect = (System.Drawing.Point)obj;
                return Equals(rect);
            }
            return false;
        }

        /// <inheritdoc />
        public bool Equals(NativePoint other)
        {
            return _x == other._x &&
                   _y == other._y &&
                   X == other.X &&
                   Y == other.Y;
        }

        /// <summary>
        /// Decontructor for tuples
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        /// <summary>
        ///     Empty NativePoint
        /// </summary>
        public static NativePoint Empty { get; } = new NativePoint();
    }
}