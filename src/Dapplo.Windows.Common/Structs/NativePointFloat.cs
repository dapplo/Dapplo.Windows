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
    public struct NativePointFloat
    {
        private readonly float _x;
        private readonly float _y;

        /// <summary>
        ///     The X coordinate
        /// </summary>
        public float X => _x;

        /// <summary>
        ///     The Y coordinate
        /// </summary>
        public float Y => _y;

        /// <summary>
        ///     Constructor with x and y coordinates
        /// </summary>
        /// <param name="x">float</param>
        /// <param name="y">float</param>
        public NativePointFloat(float x, float y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        ///     Implicit cast from NativePoint to Point
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        public static implicit operator Point(NativePointFloat point)
        {
            return new Point(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from NativePoint to System.Drawing.Point
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        public static implicit operator System.Drawing.Point(NativePointFloat point)
        {
            return new System.Drawing.Point((int) point.X, (int) point.Y);
        }

        /// <summary>
        ///     Implicit cast from NativePoint to System.Drawing.PointF
        /// </summary>
        /// <param name="point">NativePointFloat</param>
        public static implicit operator System.Drawing.PointF(NativePointFloat point)
        {
            return new System.Drawing.PointF(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from Point to NativePointFloat
        /// </summary>
        /// <param name="point">Point</param>
        public static implicit operator NativePointFloat(Point point)
        {
            return new NativePointFloat((float) point.X, (float) point.Y);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.Point to NativePointFloat
        /// </summary>
        /// <param name="point">System.Drawing.Point</param>
        public static implicit operator NativePointFloat(System.Drawing.Point point)
        {
            return new NativePointFloat(point.X, point.Y);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.PointF to NativePointFloat
        /// </summary>
        /// <param name="point">System.Drawing.PointF</param>
        public static implicit operator NativePointFloat(System.Drawing.PointF point)
        {
            return new NativePointFloat(point.X, point.Y);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is NativePointFloat && Equals((NativePointFloat)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
            }
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other">NativePointFloat</param>
        /// <returns>bool true if the values are equal</returns>
        public bool Equals(NativePointFloat other)
        {
            return Math.Abs(X - other.X) < float.Epsilon && Math.Abs(Y - other.Y) < float.Epsilon;
        }

        /// <summary>
        /// Equal
        /// </summary>
        /// <param name="lhs">NativePointFloat lefthandside</param>
        /// <param name="rhs">NativePointFloat righthandside</param>
        /// <returns>bool true if the values are equal</returns>
        public static bool operator ==(NativePointFloat lhs, NativePointFloat rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Not equal
        /// </summary>
        /// <param name="lhs">NativePointFloat lefthandside</param>
        /// <param name="rhs">NativePointFloat righthandside</param>
        /// <returns>bool true if the values are not equal</returns>
        public static bool operator !=(NativePointFloat lhs, NativePointFloat rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return X + "," + Y;
        }

        /// <summary>
        ///     Empty NativePointFloat
        /// </summary>
        public static NativePointFloat Empty { get; } = new NativePointFloat();
    }
}