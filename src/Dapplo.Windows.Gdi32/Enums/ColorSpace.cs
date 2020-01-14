// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Gdi32.Enums
{
    /// <summary>
    /// For values see the bV5CSType property <a href="https://docs.microsoft.com/en-gb/windows/desktop/api/wingdi/ns-wingdi-bitmapv5header">here</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ColorSpace : uint
    {
        /// <summary>
        ///     Color values are calibrated red green blue (RGB) values.
        /// </summary>
        LCS_CALIBRATED_RGB = 0,

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
        LCS_GM_ABS_COLORIMETRIC = 0x00000008,

        /// <summary>
        ///     The value is an encoding of the ASCII characters "sRGB", and it indicates that the color values are sRGB values.
        /// </summary>
        LCS_sRGB = 1934772034,

        /// <summary>
        ///     The value is an encoding of the ASCII characters "Win ", including the trailing space, and it indicates that the
        ///     color values are Windows default color space values.
        /// </summary>
        LCS_WINDOWS_COLOR_SPACE = 1466527264,

        /// <summary>
        ///     This value indicates that bV5ProfileData points to the file name of the profile to use (gamma and endpoints values
        ///     are ignored).
        /// </summary>
        PROFILE_LINKED,

        /// <summary>
        ///     This value indicates that bV5ProfileData points to a memory buffer that contains the profile to be used (gamma and
        ///     endpoints values are ignored).
        /// </summary>
        PROFILE_EMBEDDED
    }
}