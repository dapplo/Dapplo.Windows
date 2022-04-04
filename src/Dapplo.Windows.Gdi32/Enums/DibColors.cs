// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Gdi32.Enums;

/// <summary>
///     The DIBColors enumeration defines how to interpret the values in the color table of a DIB.
/// </summary>
public enum DibColors : uint
{
    /// <summary>
    ///     The color table contains literal RGB values.
    /// </summary>
    RgbColors = 0x00,
    /// <summary>
    /// The color table consists of an array of 16-bit indexes into the LogPalette object (section 2.2.17) that is currently defined in the playback device context.
    /// </summary>
    PalColors = 0x01,
    /// <summary>
    /// No color table exists. The pixels in the DIB are indices into the current logical palette in the playback device context.
    /// </summary>
    PalIndices = 0x02
}