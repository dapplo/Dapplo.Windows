// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Common.Structs.PixelFormats;

/// <summary>
/// Represents a pixel in BGR format (Blue, Green, Red) - 24 bits per pixel.
/// This is the native format for Format24bppRgb in System.Drawing.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Bgr24 : IEquatable<Bgr24>
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
    /// Initializes a new instance of the <see cref="Bgr24"/> struct.
    /// </summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    public Bgr24(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Compares two <see cref="Bgr24"/> objects for equality.
    /// </summary>
    public bool Equals(Bgr24 other) => B == other.B && G == other.G && R == other.R;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Bgr24 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(B, G, R);

    /// <summary>
    /// Compares two <see cref="Bgr24"/> objects for equality.
    /// </summary>
    public static bool operator ==(Bgr24 left, Bgr24 right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Bgr24"/> objects for inequality.
    /// </summary>
    public static bool operator !=(Bgr24 left, Bgr24 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Bgr24({R}, {G}, {B})";
}
#endif
