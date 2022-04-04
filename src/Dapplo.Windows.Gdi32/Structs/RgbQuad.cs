// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Gdi32.Structs;

/// <summary>
/// The RGBQUAD structure describes a color consisting of relative intensities of red, green, and blue.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RgbQuad
{
    private byte rgbBlue;
    private byte rgbGreen;
    private byte rgbRed;
    private byte rgbReserved;

    /// <summary>
    ///     The intensity of blue in the color.
    /// </summary>
    public byte Blue
    {
        get => rgbBlue;
        set => rgbBlue = value;
    }

    /// <summary>
    ///     The intensity of green in the color.
    /// </summary>
    public byte Green
    {
        get => rgbGreen;
        set => rgbGreen = value;
    }

    /// <summary>
    ///     The intensity of red in the color.
    /// </summary>
    public byte Red
    {
        get => rgbRed;
        set => rgbRed = value;
    }

    /// <summary>
    ///     The reserved value
    /// </summary>
    public byte Reserved
    {
        get => rgbReserved;
        set => rgbReserved = value;
    }
}