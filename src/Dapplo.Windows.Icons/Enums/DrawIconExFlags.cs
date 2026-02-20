// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Gdi32.Enums;

/// <summary>
///     A raster-operation code. These codes define how the color data for the source rectangle is to be combined with the
///     color data for the destination rectangle to achieve the final color.
/// </summary>
[Flags]
public enum DrawIconExFlags : uint
{
    /// <summary>
    /// Draws the icon or cursor using the mask.
    /// </summary>
    DI_MASK = 0x0001,

    /// <summary>
    /// Draws the icon or cursor using the image.
    /// </summary>
    DI_IMAGE = 0x0002,

    /// <summary>
    /// Combination of DI_IMAGE and DI_MASK.
    /// </summary>
    DI_NORMAL = 0x0003,

    /// <summary>
    /// This flag is ignored.
    /// </summary>
    DI_COMPAT = 0x0004,

    /// <summary>
    /// Draws the icon or cursor using the width and height specified by the system metric 
    /// values for icons, if the cxWidth and cyWidth parameters are set to zero.
    /// </summary>
    DI_DEFAULTSIZE = 0x0008,

    /// <summary>
    /// Draws the icon as an unmirrored icon. By default, the icon is drawn as a mirrored 
    /// icon if hdc is mirrored.
    /// </summary>
    DI_NOMIRROR = 0x0010
}