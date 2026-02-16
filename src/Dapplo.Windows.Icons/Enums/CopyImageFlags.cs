// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Icons.Enums;

/// <summary>
/// Defines flags that specify options for copying images, such as color depth, resource handling, and object lifetime.
/// </summary>
/// <remarks>Use this enumeration with image-related functions to control behaviors like creating monochrome
/// images, using system default sizes, or managing the original image resource. Multiple flags can be combined using a
/// bitwise OR operation to specify more than one option.</remarks>
[Flags]
public enum CopyImageFlags : uint
{
    /// <summary>The default flag. All it means is "not LR_MONOCHROME".</summary>
    LR_DEFAULTCOLOR = 0x00000000,

    /// <summary>Creates a new monochrome image.</summary>
    LR_MONOCHROME = 0x00000001,

    /// <summary>
    /// Returns the original handle if it satisfies the criteria (size/color depth). 
    /// If this flag is not specified, a new object is always created.
    /// </summary>
    LR_COPYRETURNORG = 0x00000004,

    /// <summary>Deletes the original image after creating the copy.</summary>
    LR_COPYDELETEORG = 0x00000008,

    /// <summary>
    /// Uses the system metric values for cursors/icons if cx or cy are zero.
    /// </summary>
    LR_DEFAULTSIZE = 0x00000040,

    /// <summary>If uType is IMAGE_BITMAP, creates a DIB section instead of a DDB.</summary>
    LR_CREATEDIBSECTION = 0x00002000,

    /// <summary>
    /// Tries to reload the icon or cursor from the original resource file 
    /// to get a better quality resize.
    /// </summary>
    LR_COPYFROMRESOURCE = 0x00004000
}