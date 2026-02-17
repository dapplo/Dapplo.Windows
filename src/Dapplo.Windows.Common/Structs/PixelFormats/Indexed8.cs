// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Common.Structs.PixelFormats;

/// <summary>
/// Represents an indexed pixel - 8 bits per pixel.
/// This is the format for Format8bppIndexed in System.Drawing.
/// The value is an index into a color palette.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Indexed8 : IEquatable<Indexed8>
{
    /// <summary>
    /// The palette index value (0-255).
    /// </summary>
    public byte Index;

    /// <summary>
    /// Initializes a new instance of the <see cref="Indexed8"/> struct.
    /// </summary>
    /// <param name="index">The palette index.</param>
    public Indexed8(byte index)
    {
        Index = index;
    }

    /// <summary>
    /// Compares two <see cref="Indexed8"/> objects for equality.
    /// </summary>
    public bool Equals(Indexed8 other) => Index == other.Index;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Indexed8 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Index.GetHashCode();

    /// <summary>
    /// Compares two <see cref="Indexed8"/> objects for equality.
    /// </summary>
    public static bool operator ==(Indexed8 left, Indexed8 right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Indexed8"/> objects for inequality.
    /// </summary>
    public static bool operator !=(Indexed8 left, Indexed8 right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Indexed8({Index})";
}
#endif
