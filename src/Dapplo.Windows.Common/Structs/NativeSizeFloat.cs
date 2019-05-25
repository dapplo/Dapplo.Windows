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
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.TypeConverters;

#endregion

namespace Dapplo.Windows.Common.Structs
{
    /// <summary>
    ///     This structure should be used everywhere where native methods need a size-f struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    [TypeConverter(typeof(NativeSizeFloatTypeConverter))]
    public readonly struct NativeSizeFloat : IEquatable<NativeSizeFloat>, IComparable<NativeSizeFloat>
    {
        private readonly float _width;
        private readonly float _height;

        /// <summary>
        ///     The Width of the size struct
        /// </summary>
        public float Width => _width;

        /// <summary>
        ///     The Width of the size struct
        /// </summary>
        public float Height => _height;

        /// <summary>
        ///     Returns an empty size
        /// </summary>
        public static NativeSizeFloat Empty { get; } = new NativeSizeFloat(0, 0);

        /// <summary>
        ///     Constructor from S.D.Size
        /// </summary>
        /// <param name="size"></param>
        public NativeSizeFloat(System.Drawing.Size size) : this(size.Width, size.Height)
        {
        }

        /// <summary>
        ///     Size contructor
        /// </summary>
        /// <param name="width">float</param>
        /// <param name="height">float</param>
        public NativeSizeFloat(float width, float height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        ///     Size contructor
        /// </summary>
        /// <param name="width">double</param>
        /// <param name="height">double</param>
        public NativeSizeFloat(double width, double height)
        {
            _width = (float)width;
            _height = (float)height;
        }

#if !NETSTANDARD2_0
        /// <summary>
        ///     Constructor from S.W.Size
        /// </summary>
        /// <param name="size"></param>
        public NativeSizeFloat(System.Windows.Size size) : this((float) size.Width, (float) size.Height)
        {
        }

        /// <summary>
        ///     Implicit cast from NativeSizeFloat to System.Windows.Size
        /// </summary>
        /// <param name="size">NativeSize</param>
        /// <returns>System.Windows.Size</returns>
        public static implicit operator System.Windows.Size(NativeSizeFloat size)
        {
            return new System.Windows.Size(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from Size to NativeSizeFloat
        /// </summary>
        /// <param name="size">System.Windows.Size</param>
        /// <returns>NativeSizeFloat</returns>
        public static implicit operator NativeSizeFloat(System.Windows.Size size)
        {
            return new NativeSizeFloat((float)size.Width, (float)size.Height);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">System.Windows.Size</param>
        /// <returns>bool</returns>
        public static bool operator ==(NativeSizeFloat float1, System.Windows.Size float2)
        {
            return float1.Equals(float2);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">System.Windows.Size</param>
        /// <returns>bool</returns>
        public static bool operator !=(NativeSizeFloat float1, System.Windows.Size float2)
        {
            return !float1.Equals(float2);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="float1">System.Windows.Size</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator ==(System.Windows.Size float1, NativeSizeFloat float2)
        {
            return float2.Equals(float1);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="float1">System.Windows.Size</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator !=(System.Windows.Size float1, NativeSizeFloat float2)
        {
            return !float2.Equals(float1);
        }
#endif

        /// <summary>
        ///     Implicit cast from NativeSize to System.Drawing.Size
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <returns>System.Drawing.Size</returns>
        public static implicit operator System.Drawing.Size(NativeSizeFloat size)
        {
            return new System.Drawing.Size((int)size.Width, (int)size.Height);
        }

        /// <summary>
        ///     Implicit cast from NativeSize to System.Drawing.SizeF
        /// </summary>
        /// <param name="size">NativeSizeFloat</param>
        /// <returns>System.Drawing.SizeF</returns>
        public static implicit operator System.Drawing.SizeF(NativeSizeFloat size)
        {
            return new System.Drawing.SizeF(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.Size to NativeSizeFloat
        /// </summary>
        /// <param name="size">System.Drawing.Size</param>
        /// <returns>NativeSizeFloat</returns>
        public static implicit operator NativeSizeFloat(System.Drawing.Size size)
        {
            return new NativeSizeFloat(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from NativeSize to NativeSizeFloat
        /// </summary>
        /// <param name="size">NativeSize</param>
        /// <returns>NativeSizeFloat</returns>
        public static implicit operator NativeSizeFloat(NativeSize size)
        {
            return new NativeSizeFloat(size.Width, size.Height);
        }

        /// <summary>
        ///     Implicit cast from System.Drawing.SizeF to NativeSizeFloat
        /// </summary>
        /// <param name="size">System.Drawing.Size</param>
        /// <returns>NativeSizeFloat</returns>
        public static implicit operator NativeSizeFloat(System.Drawing.SizeF size)
        {
            return new NativeSizeFloat(size.Width, size.Height);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator ==(NativeSizeFloat float1, NativeSizeFloat float2)
        {
            return float1.Equals(float2);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator !=(NativeSizeFloat float1, NativeSizeFloat float2)
        {
            return !float1.Equals(float2);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">System.Windows.Size</param>
        /// <returns>bool</returns>
        public static bool operator ==(NativeSizeFloat float1, System.Drawing.Size float2)
        {
            return float1.Equals(float2);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="float1">NativeSizeFloat</param>
        /// <param name="float2">System.Windows.Size</param>
        /// <returns>bool</returns>
        public static bool operator !=(NativeSizeFloat float1, System.Drawing.Size float2)
        {
            return !float1.Equals(float2);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="float1">System.Windows.Size</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator ==(System.Drawing.Size float1, NativeSizeFloat float2)
        {
            return float2.Equals(float1);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="float1">System.Windows.Size</param>
        /// <param name="float2">NativeSizeFloat</param>
        /// <returns>bool</returns>
        public static bool operator !=(System.Drawing.Size float1, NativeSizeFloat float2)
        {
            return !float2.Equals(float1);
        }

        /// <summary>
        ///     Checks if the width * height are 0
        /// </summary>
        /// <returns>true if the size is empty</returns>
        [Pure]
        public bool IsEmpty => Math.Abs(_width * _height) < float.Epsilon;

        /// <inheritdoc />
        [Pure]
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case NativeSizeFloat f:
                    return Equals(f);
#if !NETSTANDARD2_0
                case System.Windows.Size size:
                    return Equals(size);
#endif
                case System.Drawing.Size drawingSize:
                    return Equals(drawingSize);
            }

            return false;
        }

        /// <inheritdoc />
        [Pure]
        public bool Equals(NativeSizeFloat other)
        {
            return Math.Abs(_width - other._width) < float.Epsilon &&
                   Math.Abs(_height - other._height) < float.Epsilon;
        }

        /// <inheritdoc />
        [Pure]
        public int CompareTo(NativeSizeFloat other) => unchecked(other.Width * other.Height).CompareTo(unchecked (Width * Height));

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            unchecked
            {
                return (_width.GetHashCode() * 397) ^ _height.GetHashCode();
            }
        }

        /// <summary>
        /// Decontructor for tuples
        /// </summary>
        /// <param name="width">float</param>
        /// <param name="height">float</param>
        public void Deconstruct(out float width, out float height)
        {
            width = Width;
            height = Height;
        }

        /// <inheritdoc />
        [Pure]
        public override string ToString()
        {
            return $"{{Width: {_width}; Height: {_height};}}";
        }
    }
}