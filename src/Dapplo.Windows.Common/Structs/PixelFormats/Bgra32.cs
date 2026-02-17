// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Common.Structs.PixelFormats;

/// <summary>
/// Represents a pixel in BGRA format (Blue, Green, Red, Alpha) - 32 bits per pixel.
/// This is the native format for Format32bppArgb in System.Drawing.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Bgra32 : IEquatable<Bgra32>
{
    /// <summary>
    /// The blue component value.
    /// </summary>
    public byte B;

    /// <summary>
    /// The green component value.
    /// </summary>
    public byte G;

    /// <summary>
    /// The red component value.
    /// </summary>
    public byte R;

    /// <summary>
    /// The alpha component value.
    /// </summary>
    public byte A;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bgra32"/> struct.
    /// </summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="a">The alpha component.</param>
    public Bgra32(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Compares two <see cref="Bgra32"/> objects for equality.
    /// </summary>
    public bool Equals(Bgra32 other) => B == other.B && G == other.G && R == other.R && A == other.A;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Bgra32 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(B, G, R, A);

    /// <summary>
    /// Compares two <see cref="Bgra32"/> objects for equality.
    /// </summary>
    public static bool operator ==(Bgra32 left, Bgra32 right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Bgra32"/> objects for inequality.
    /// </summary>
    public static bool operator !=(Bgra32 left, Bgra32 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Bgra32({R}, {G}, {B}, {A})";
}
#endif
