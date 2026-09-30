// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Gdi32.Enums;

/// <summary>
/// The color space of a DIB, the value of the bV5CSType field (the rendering intent is <see cref="ColorSpaceIntent"/>).
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
    ///     The value is an encoding of the ASCII characters "sRGB", and it indicates that the color values are sRGB values.
    /// </summary>
    LCS_sRGB = 0x73524742,

    /// <summary>
    ///     The value is an encoding of the ASCII characters "Win ", including the trailing space, and it indicates that the
    ///     color values are Windows default color space values.
    /// </summary>
    LCS_WINDOWS_COLOR_SPACE = 0x57696E20,

    /// <summary>
    ///     This value indicates that bV5ProfileData points to the file name of the profile to use (gamma and endpoints values
    ///     are ignored).
    /// </summary>
    PROFILE_LINKED = 0x4C494E4B,

    /// <summary>
    ///     This value indicates that bV5ProfileData points to a memory buffer that contains the profile to be used (gamma and
    ///     endpoints values are ignored).
    /// </summary>
    PROFILE_EMBEDDED = 0x4D424544
}