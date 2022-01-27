﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Gdi32.Enums;

/// <summary>
///     Used by GDI32.GetDeviceCaps
///     See: http://msdn.microsoft.com/en-us/library/windows/desktop/dd144877%28v=vs.85%29.aspx
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DeviceCaps
{
    /// <summary>
    ///     Device driver version
    /// </summary>
    DRIVERVERSION = 0,

    /// <summary>
    ///     Device classification
    /// </summary>
    TECHNOLOGY = 2,

    /// <summary>
    ///     Horizontal size in millimeters
    /// </summary>
    HORZSIZE = 4,

    /// <summary>
    ///     Vertical size in millimeters
    /// </summary>
    VERTSIZE = 6,

    /// <summary>
    ///     Horizontal width in pixels
    /// </summary>
    HORZRES = 8,

    /// <summary>
    ///     Vertical height in pixels
    /// </summary>
    VERTRES = 10,

    /// <summary>
    ///     Number of bits per pixel
    /// </summary>
    BITSPIXEL = 12,

    /// <summary>
    ///     Number of planes
    /// </summary>
    PLANES = 14,

    /// <summary>
    ///     Number of brushes the device has
    /// </summary>
    NUMBRUSHES = 16,

    /// <summary>
    ///     Number of pens the device has
    /// </summary>
    NUMPENS = 18,

    /// <summary>
    ///     Number of markers the device has
    /// </summary>
    NUMMARKERS = 20,

    /// <summary>
    ///     Number of fonts the device has
    /// </summary>
    NUMFONTS = 22,

    /// <summary>
    ///     Number of colors the device supports
    /// </summary>
    NUMCOLORS = 24,

    /// <summary>
    ///     Size required for device descriptor
    /// </summary>
    PDEVICESIZE = 26,

    /// <summary>
    ///     Curve capabilities
    /// </summary>
    CURVECAPS = 28,

    /// <summary>
    ///     Line capabilities
    /// </summary>
    LINECAPS = 30,

    /// <summary>
    ///     Polygonal capabilities
    /// </summary>
    POLYGONALCAPS = 32,

    /// <summary>
    ///     Text capabilities
    /// </summary>
    TEXTCAPS = 34,

    /// <summary>
    ///     Clipping capabilities
    /// </summary>
    CLIPCAPS = 36,

    /// <summary>
    ///     Bitblt capabilities
    /// </summary>
    RASTERCAPS = 38,

    /// <summary>
    ///     Length of the X leg
    /// </summary>
    ASPECTX = 40,

    /// <summary>
    ///     Length of the Y leg
    /// </summary>
    ASPECTY = 42,

    /// <summary>
    ///     Length of the hypotenuse
    /// </summary>
    ASPECTXY = 44,

    /// <summary>
    ///     Shading and Blending caps
    /// </summary>
    SHADEBLENDCAPS = 45,

    /// <summary>
    ///     Logical pixels inch in X
    /// </summary>
    LOGPIXELSX = 88,

    /// <summary>
    ///     Logical pixels inch in Y
    /// </summary>
    LOGPIXELSY = 90,

    /// <summary>
    ///     Number of entries in physical palette
    /// </summary>
    SIZEPALETTE = 104,

    /// <summary>
    ///     Number of reserved entries in palette
    /// </summary>
    NUMRESERVED = 106,

    /// <summary>
    ///     Actual color resolution
    /// </summary>
    COLORRES = 108,

    // Printing related DeviceCaps. These replace the appropriate Escapes
    /// <summary>
    ///     Physical Width in device units
    /// </summary>
    PHYSICALWIDTH = 110,

    /// <summary>
    ///     Physical Height in device units
    /// </summary>
    PHYSICALHEIGHT = 111,

    /// <summary>
    ///     Physical Printable Area x margin
    /// </summary>
    PHYSICALOFFSETX = 112,

    /// <summary>
    ///     Physical Printable Area y margin
    /// </summary>
    PHYSICALOFFSETY = 113,

    /// <summary>
    ///     Scaling factor x
    /// </summary>
    SCALINGFACTORX = 114,

    /// <summary>
    ///     Scaling factor y
    /// </summary>
    SCALINGFACTORY = 115,

    /// <summary>
    ///     Current vertical refresh rate of the display device (for displays only) in Hz
    /// </summary>
    VREFRESH = 116,

    /// <summary>
    ///     Horizontal width of entire desktop in pixels
    /// </summary>
    DESKTOPVERTRES = 117,

    /// <summary>
    ///     Vertical height of entire desktop in pixels
    /// </summary>
    DESKTOPHORZRES = 118,

    /// <summary>
    ///     Preferred blt alignment
    /// </summary>
    BLTALIGNMENT = 119
}