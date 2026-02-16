// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Icons.Enums;

/// <summary>
/// Specifies options for loading images, such as icons, cursors, or bitmaps, including color mode, size, source, and
/// sharing behavior.
/// </summary>
/// <remarks>This enumeration supports bitwise combination of its member values to specify multiple loading
/// options simultaneously. It is typically used with image loading functions to control how images are loaded from
/// files or resources, including whether to use system colors, load images as DIB sections, or share image handles
/// across multiple loads.</remarks>
[Flags]
public enum LoadImageFlags : uint
{
    /// <summary>The default flag; it does nothing. All it means is "not LR_MONOCHROME".</summary>
    LR_DEFAULTCOLOR = 0x00000000,

    /// <summary>Loads the image in black and white.</summary>
    LR_MONOCHROME = 0x00000001,

    /// <summary>Loads the image in color.</summary>
    LR_COLOR = 0x00000002,

    /// <summary>Searches the color table for the image and replaces the first instance of 
    /// a gray pixel with the system color COLOR_WINDOW.</summary>
    LR_LOADTRANSPARENT = 0x00000020,

    /// <summary>Uses the width or height specified by the system metric values for cursors or icons, 
    /// if the cxDesired or cyDesired values are set to zero.</summary>
    LR_DEFAULTSIZE = 0x00000040,

    /// <summary>Uses true VGA colors.</summary>
    LR_VGACOLOR = 0x00000080,

    /// <summary>Loads the standalone image from the file specified by lpszName (icon, cursor, or bitmap file).</summary>
    LR_LOADFROMFILE = 0x00000010,

    /// <summary>Searches the color table for the image and replaces shades of gray with the 
    /// corresponding 3-D objects.</summary>
    LR_LOADMAP3DCOLORS = 0x00001000,

    /// <summary>Retrieves the color information from the color table of the first image and 
    /// maps it to the current desktop colors.</summary>
    LR_CREATEDIBSECTION = 0x00002000,

    /// <summary>Loads the image as a DIB section rather than a compatible bitmap.</summary>
    LR_COPYFROMRESOURCE = 0x00004000,

    /// <summary>Shares the image handle if the image is loaded multiple times.</summary>
    LR_SHARED = 0x00008000
}