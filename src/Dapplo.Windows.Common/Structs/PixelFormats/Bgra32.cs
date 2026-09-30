// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Alpha blends a source pixel onto a target pixel, using the Porter-Duff "source over" operator.
    /// Both pixels use straight (non-premultiplied) alpha, as in Format32bppArgb.
    /// </summary>
    /// <remarks>
    /// When the target is opaque (A = 255), this is the classic <c>src * a + dst * (1 - a)</c>.
    /// For a (semi-)transparent target the resulting alpha is <c>a_src + a_dst * (1 - a_src)</c> and the colors are weighted accordingly.
    /// For a target which has no alpha channel (e.g. Format32bppRgb), set target.A to 255 before blending.
    /// </remarks>
    /// <param name="target">The target pixel to blend onto (modified in place), straight alpha.</param>
    /// <param name="source">The source pixel to blend from, straight alpha.</param>
    public static void AlphaBlend(ref Bgra32 target, in Bgra32 source)
    {
        if (source.A == 0)
        {
            // Fully transparent, no change
            return;
        }

        if (source.A == 255 || target.A == 0)
        {
            // Fully opaque source or fully transparent target, direct copy
            target = source;
            return;
        }

        int sourceAlpha = source.A;
        int invSourceAlpha = 255 - sourceAlpha;
        if (target.A == 255)
        {
            // Opaque target, the result is opaque
            target.B = (byte)Div255(source.B * sourceAlpha + target.B * invSourceAlpha);
            target.G = (byte)Div255(source.G * sourceAlpha + target.G * invSourceAlpha);
            target.R = (byte)Div255(source.R * sourceAlpha + target.R * invSourceAlpha);
            return;
        }

        // Porter-Duff over for straight alpha, all values scaled by 255 * 255
        int sourceWeight = sourceAlpha * 255;
        int targetWeight = target.A * invSourceAlpha;
        int resultAlpha = sourceWeight + targetWeight;
        int half = resultAlpha / 2;
        target.B = (byte)((source.B * sourceWeight + target.B * targetWeight + half) / resultAlpha);
        target.G = (byte)((source.G * sourceWeight + target.G * targetWeight + half) / resultAlpha);
        target.R = (byte)((source.R * sourceWeight + target.R * targetWeight + half) / resultAlpha);
        target.A = (byte)Div255(resultAlpha);
    }

    /// <summary>
    /// Alpha blends a source pixel onto a target pixel, using the Porter-Duff "source over" operator.
    /// Both pixels use premultiplied alpha, as in Format32bppPArgb: <c>result = src + dst * (1 - a_src)</c> for all channels.
    /// </summary>
    /// <param name="target">The target pixel to blend onto (modified in place), premultiplied alpha.</param>
    /// <param name="source">The source pixel to blend from, premultiplied alpha.</param>
    public static void AlphaBlendPremultiplied(ref Bgra32 target, in Bgra32 source)
    {
        if (source.A == 0 && source.R == 0 && source.G == 0 && source.B == 0)
        {
            // Fully transparent, no change
            return;
        }

        if (source.A == 255)
        {
            target = source;
            return;
        }

        int invSourceAlpha = 255 - source.A;
        target.B = ClampToByte(source.B + Div255(target.B * invSourceAlpha));
        target.G = ClampToByte(source.G + Div255(target.G * invSourceAlpha));
        target.R = ClampToByte(source.R + Div255(target.R * invSourceAlpha));
        target.A = ClampToByte(source.A + Div255(target.A * invSourceAlpha));
    }

    /// <summary>
    /// Convert a straight (non-premultiplied) alpha pixel to a premultiplied alpha pixel.
    /// </summary>
    /// <returns>Bgra32 with premultiplied color components</returns>
    public readonly Bgra32 ToPremultiplied()
    {
        if (A == 255)
        {
            return this;
        }
        return new Bgra32((byte)Div255(R * A), (byte)Div255(G * A), (byte)Div255(B * A), A);
    }

    /// <summary>
    /// Convert a premultiplied alpha pixel to a straight (non-premultiplied) alpha pixel.
    /// </summary>
    /// <returns>Bgra32 with straight color components</returns>
    public readonly Bgra32 ToStraight()
    {
        if (A == 255)
        {
            return this;
        }
        if (A == 0)
        {
            return default;
        }
        int half = A / 2;
        return new Bgra32(ClampToByte((R * 255 + half) / A), ClampToByte((G * 255 + half) / A), ClampToByte((B * 255 + half) / A), A);
    }

    /// <summary>
    /// Divide by 255 with rounding, for values in the range 0 - 65025
    /// </summary>
    internal static int Div255(int value) => (value + 127) / 255;

    private static byte ClampToByte(int value) => value > 255 ? (byte)255 : (byte)value;
}
