// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Vfw32
{
    /// <summary>This represents the four character code (FOURCC) widely used for codecs.</summary>
    public readonly struct FourCC
    {
        private readonly uint _value;

        /// <summary>
        /// Creates a new instance of <see cref="FourCC"/> with an integer value.
        /// </summary>
        /// <param name="value">Integer value of FOURCC.</param>
        public FourCC(uint value)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a new instance of <see cref="FourCC"/> with a string value.
        /// </summary>
        /// <param name="value">string value of FOURCC.</param>
        /// <remarks>If the value of <paramref name="value"/> is shorter than 4 characters, it is right-padded with spaces.</remarks>
        public FourCC(string value)
        {
            value ??= string.Empty;

            var valueString = value.PadRight(4);
            _value = valueString[0] + ((uint)valueString[1] << 8) + ((uint)valueString[2] << 16) + ((uint)valueString[3] << 24);
        }

        /// <summary>
        /// Returns string representation of the FourCC.
        /// </summary>
        /// <returns>string value if all bytes are printable ASCII characters. Otherwise, the hexadecimal representation of integer value.</returns>
        public override string ToString() => new string
            (
                new[]
                {
                    (char)(_value & 0xFF),
                    (char)((_value & 0xFF00) >> 8),
                    (char)((_value & 0xFF0000) >> 16),
                    (char)((_value & 0xFF000000U) >> 24)
                }
            );

        /// <summary>
        /// Gets hash code of this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Determines whether this instance is equal to other object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is FourCC cc)
            {
                return cc == this;
            }

            return base.Equals(obj);
        }


        /// <summary>
        /// Converts an integer value to <see cref="FourCC"/>.
        /// </summary>
        public static implicit operator FourCC(uint value)
        {
            return new FourCC(value);
        }

        /// <summary>
        /// Converts a string value to <see cref="FourCC"/>.
        /// </summary>
        public static implicit operator FourCC(string value)
        {
            return new FourCC(value);
        }

        /// <summary>
        /// Gets the integer value of <see cref="FourCC"/> instance.
        /// </summary>
        public static explicit operator uint(FourCC value)
        {
            return value._value;
        }

        /// <summary>
        /// Gets the string value of <see cref="FourCC"/> instance.
        /// </summary>
        public static explicit operator string(FourCC value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Determines whether two instances of <see cref="FourCC"/> are equal.
        /// </summary>
        public static bool operator ==(FourCC value1, FourCC value2)
        {
            return value1._value == value2._value;
        }

        /// <summary>
        /// Determines whether two instances of <see cref="FourCC"/> are not equal.
        /// </summary>
        public static bool operator !=(FourCC value1, FourCC value2)
        {
            return !(value1 == value2);
        }

        /// <summary>
        /// Const for well known CodecType VIDEO
        /// </summary>
        public static FourCC CodecTypeVideo { get; } = new FourCC("VIDC");
    }
}
