// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Gdi32.Enums;

/// <summary>
/// The rendering intent of a DIB, the value of the bV5Intent field.
/// For values see the bV5Intent property <a href="https://docs.microsoft.com/en-gb/windows/desktop/api/wingdi/ns-wingdi-bitmapv5header">here</a>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ColorSpaceIntent : uint
{
    /// <summary>
    ///     Maintains saturation. Used for business charts and other situations in which undithered colors are required.
    /// </summary>
    LCS_GM_BUSINESS = 0x00000001,

    /// <summary>
    ///     Maintains colorimetric match. Used for graphic designs and named colors.
    /// </summary>
    LCS_GM_GRAPHICS = 0x00000002,

    /// <summary>
    ///     Maintains contrast. Used for photographs and natural images.
    /// </summary>
    LCS_GM_IMAGES = 0x00000004,

    /// <summary>
    ///     Maintains the white point. Matches the colors to their nearest color in the destination gamut.
    /// </summary>
    LCS_GM_ABS_COLORIMETRIC = 0x00000008
}
