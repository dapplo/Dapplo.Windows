// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

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