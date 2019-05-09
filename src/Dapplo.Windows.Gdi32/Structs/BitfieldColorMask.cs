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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Gdi32.Structs
{
    /// <summary>
    /// Specify the color mask when the BITMAPINFOHEADER structure biCompression uses BI_BITFIELDS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public readonly struct BitfieldColorMask : IEquatable<BitfieldColorMask>
    {
        private readonly uint _blue;
        private readonly uint _green;
        private readonly uint _red;

        /// <summary>
        /// Blue component of the mask
        /// </summary>
        public uint Blue => _blue;

        /// <summary>
        /// Green component of the mask
        /// </summary>
        public uint Green => _green;

        /// <summary>
        /// Red component of the mask
        /// </summary>
        public uint Red => _red;

        /// <summary>
        /// Constructor of the BitfieldColorMask
        /// </summary>
        /// <param name="r">byte</param>
        /// <param name="g">byte</param>
        /// <param name="b">byte</param>
        public BitfieldColorMask(byte r = 255, byte g = 255, byte b = 255)
        {
            _red = (uint)r << 8;
            _green = (uint)g << 16;
            _blue = (uint)b << 24;
        }

        /// <summary>
        /// Create with BitfieldColorMask defaults
        /// </summary>
        /// <param name="r">byte value for Red component of the mask</param>
        /// <param name="g">byte value for Green component of the mask</param>
        /// <param name="b">byte value for Blue component of the mask</param>
        public static BitfieldColorMask Create(byte r = 255, byte g = 255, byte b = 255)
        {
            return new BitfieldColorMask(r,g,b);
        }

        /// <inheritdoc />
        public bool Equals(BitfieldColorMask other)
        {
            return _blue == other._blue && _green == other._green && _red == other._red;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            return obj is BitfieldColorMask mask && Equals(mask);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) _blue;
                hashCode = (hashCode * 397) ^ (int) _green;
                hashCode = (hashCode * 397) ^ (int) _red;
                return hashCode;
            }
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="left">BitfieldColorMask</param>
        /// <param name="right">BitfieldColorMask</param>
        /// <returns>bool</returns>
        public static bool operator ==(BitfieldColorMask left, BitfieldColorMask right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals
        /// </summary>
        /// <param name="left">BitfieldColorMask</param>
        /// <param name="right">BitfieldColorMask</param>
        /// <returns>bool</returns>
        public static bool operator !=(BitfieldColorMask left, BitfieldColorMask right)
        {
            return !left.Equals(right);
        }
    }
}