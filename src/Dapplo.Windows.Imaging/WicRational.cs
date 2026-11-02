// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// An unsigned rational, e.g. an EXIF RATIONAL like XResolution or ExposureTime.
/// WIC stores it as VT_UI8 with the numerator in the low and the denominator in the high 32 bits.
/// </summary>
public readonly struct WicRational : IEquatable<WicRational>
{
    /// <summary>
    /// Create a rational
    /// </summary>
    public WicRational(uint numerator, uint denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>The numerator</summary>
    public uint Numerator { get; }

    /// <summary>The denominator</summary>
    public uint Denominator { get; }

    /// <summary>The value as double, NaN when the denominator is 0</summary>
    public double ToDouble() => Denominator == 0 ? double.NaN : (double)Numerator / Denominator;

    /// <summary>The VT_UI8 form WIC uses</summary>
    public ulong ToPacked() => Numerator | ((ulong)Denominator << 32);

    /// <summary>Create from the VT_UI8 form WIC uses</summary>
    public static WicRational FromPacked(ulong packed) => new WicRational(unchecked((uint)packed), (uint)(packed >> 32));

    /// <inheritdoc />
    public bool Equals(WicRational other) => Numerator == other.Numerator && Denominator == other.Denominator;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is WicRational other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => ToPacked().GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"{Numerator}/{Denominator}";
}

/// <summary>
/// A signed rational, e.g. an EXIF SRATIONAL like ExposureBiasValue.
/// WIC stores it as VT_I8 with the numerator in the low and the denominator in the high 32 bits.
/// </summary>
public readonly struct WicSignedRational : IEquatable<WicSignedRational>
{
    /// <summary>
    /// Create a signed rational
    /// </summary>
    public WicSignedRational(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>The numerator</summary>
    public int Numerator { get; }

    /// <summary>The denominator</summary>
    public int Denominator { get; }

    /// <summary>The value as double, NaN when the denominator is 0</summary>
    public double ToDouble() => Denominator == 0 ? double.NaN : (double)Numerator / Denominator;

    /// <summary>The VT_I8 form WIC uses</summary>
    public long ToPacked() => unchecked((long)((uint)Numerator | ((ulong)(uint)Denominator << 32)));

    /// <summary>Create from the VT_I8 form WIC uses</summary>
    public static WicSignedRational FromPacked(long packed) => new WicSignedRational(unchecked((int)packed), unchecked((int)(packed >> 32)));

    /// <inheritdoc />
    public bool Equals(WicSignedRational other) => Numerator == other.Numerator && Denominator == other.Denominator;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is WicSignedRational other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => ToPacked().GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"{Numerator}/{Denominator}";
}
