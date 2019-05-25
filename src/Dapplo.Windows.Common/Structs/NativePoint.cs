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
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Windows;
#endif
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
    public readonly struct NativePoint : IEquatable<NativePoint>
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

#if !NETSTANDARD2_0
        /// <summary>
        ///     Implicit cast from NativePoint to Point
        /// </summary>
        /// <param name="point">NativePoint</param>
        public static implicit operator Point(NativePoint point)
        {
            return new Point(point.X, point.Y);
        }
#endif

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
            return !point1.Equals(point2);
        }

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            unchecked
            {
                return (_x * 397) ^ _y;
            }
        }

        /// <inheritdoc />
        [Pure]
        public override string ToString() => $"{{X: {_x}; >: {_y};}}";

        /// <inheritdoc />
        [Pure]
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case NativePoint point:
                    return Equals(point);
                case System.Drawing.Point drawingPoint:
                    return Equals(drawingPoint);
            }

            return false;
        }

        /// <inheritdoc />
        [Pure]
        public bool Equals(NativePoint other) => _x == other._x && _y == other._y;

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