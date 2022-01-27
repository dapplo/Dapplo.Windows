// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

/// <summary>
///     CIE XYZ 1931 color space
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "ConvertToAutoPropertyWhenPossible")]
[SuppressMessage("Sonar Code Smell", "S2292:Trivial properties should be auto-implemented", Justification = "Interop!")]
public struct CieXyz
{
    private uint ciexyzX;
    private uint ciexyzY;
    private uint ciexyzZ;

    /// <summary>
    ///     is a mix of cone response curves chosen to be orthogonal to luminance and non-negative
    ///     FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
    /// </summary>
    public uint X
    {
        get { return ciexyzX; }
        set { ciexyzX = value; }
    }

    /// <summary>
    ///     Luminance
    ///     FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
    /// </summary>
    public uint Y
    {
        get { return ciexyzY; }
        set { ciexyzY = value; }
    }

    /// <summary>
    ///     is somewhat equal to blue
    ///     FXPT2DOT30 is a fixed-point values with a 2-bit integer part and a 30-bit fractional part.
    /// </summary>
    public uint Z {
        get { return ciexyzZ; }
        set { ciexyzZ = value; }
    }

    /// <summary>
    ///     Factory for a CieXyz
    /// </summary>
    /// <param name="fxPt2Dot30">uint</param>
    public static CieXyz Create(uint fxPt2Dot30)
    {
        return new CieXyz
        {
            ciexyzX = fxPt2Dot30,
            ciexyzY = fxPt2Dot30,
            ciexyzZ = fxPt2Dot30
        };
    }
}