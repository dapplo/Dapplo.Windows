// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

/// <summary>
/// The three DWORD color masks (red, green, blue, in this order) which follow a 40 byte BITMAPINFOHEADER when biCompression is BI_BITFIELDS
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
public readonly struct BitfieldColorMask : IEquatable<BitfieldColorMask>
{
    private readonly uint _red;
    private readonly uint _green;
    private readonly uint _blue;

    /// <summary>
    /// Red component of the mask
    /// </summary>
    public uint Red => _red;

    /// <summary>
    /// Green component of the mask
    /// </summary>
    public uint Green => _green;

    /// <summary>
    /// Blue component of the mask
    /// </summary>
    public uint Blue => _blue;

    /// <summary>
    /// Constructor of the BitfieldColorMask
    /// </summary>
    /// <param name="red">uint with the mask for the red component, e.g. 0x00FF0000</param>
    /// <param name="green">uint with the mask for the green component, e.g. 0x0000FF00</param>
    /// <param name="blue">uint with the mask for the blue component, e.g. 0x000000FF</param>
    public BitfieldColorMask(uint red, uint green, uint blue)
    {
        _red = red;
        _green = green;
        _blue = blue;
    }

    /// <summary>
    /// The masks for 32 bits per pixel with 8 bits per component, as used by 32bpp BI_RGB (0x00RRGGBB)
    /// </summary>
    public static BitfieldColorMask Rgb888 { get; } = new BitfieldColorMask(0x00FF0000, 0x0000FF00, 0x000000FF);

    /// <summary>
    /// The masks for 16 bits per pixel with 5 bits for red and blue and 6 for green
    /// </summary>
    public static BitfieldColorMask Rgb565 { get; } = new BitfieldColorMask(0xF800, 0x07E0, 0x001F);

    /// <summary>
    /// The masks for 16 bits per pixel with 5 bits per component, as used by 16bpp BI_RGB
    /// </summary>
    public static BitfieldColorMask Rgb555 { get; } = new BitfieldColorMask(0x7C00, 0x03E0, 0x001F);

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