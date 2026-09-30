// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Enums;

/// <summary>
///     These flags define which values are retrieved and if they are cached or not
/// </summary>
[Flags]
public enum InteropWindowRetrieveSettings : uint
{
    /// <summary>
    /// </summary>
    None = 0,

    /// <summary>
    ///     Forces an update of the specified flags.
    /// </summary>
    ForceUpdate = 1,

    /// <summary>
    ///     Retrieve the WindowInfo
    /// </summary>
    Info = 1 << 1,

    /// <summary>
    ///     Retrieve the caption
    /// </summary>
    Caption = 1 << 2,

    /// <summary>
    ///     Retrieve the class name
    /// </summary>
    Classname = 1 << 3,

    /// <summary>
    ///     Retrieve the matching process id
    /// </summary>
    ProcessId = 1 << 4,

    /// <summary>
    ///     Retrieve the (real) parent, this is not the owner
    /// </summary>
    Parent = 1 << 5,

    /// <summary>
    ///     Retrieve the placement
    /// </summary>
    Placement = 1 << 6,

    /// <summary>
    ///     Retrieve the is visible
    /// </summary>
    Visible = 1 << 7,

    /// <summary>
    ///     Retrieve the zoom state (maximized)
    /// </summary>
    Maximized = 1 << 8,

    /// <summary>
    ///     Retrieve the icon state (minimized)
    /// </summary>
    Minimized = 1 << 9,

    /// <summary>
    ///     Retrieve the text
    /// </summary>
    Text = 1 << 10,

    /// <summary>
    ///     Retrieve the scroll info
    /// </summary>
    ScrollInfo = 1 << 11,

    /// <summary>
    ///     Retrieve the direct children, in Z-order from top to bottom
    /// </summary>
    Children = 1 << 12,

    /// <summary>
    /// Specify if values are auto corrected, e.g. the WindowInfo bounds are cropped to the parent
    /// This enables correction, which takes a bit more time
    /// </summary>
    AutoCorrectValues = 1 << 14,

    /// <summary>
    ///     Retrieve the owner
    /// </summary>
    Owner = 1 << 15,

    /// <summary>
    ///     Cache all, except children, don't force reloading
    /// </summary>
    CacheAll = Caption | Classname | Info | Maximized | Minimized | Parent | Owner | Placement | ProcessId | Text | Visible | ScrollInfo,

    /// <summary>
    ///     Cache all, except children, don't force reloading, auto correct certain values
    /// </summary>
    CacheAllAutoCorrect = Caption | Classname | Info | Maximized | Minimized | Parent | Owner | Placement | ProcessId | Text | Visible | ScrollInfo | AutoCorrectValues,

    /// <summary>
    ///     Cache all, with children, don't force reloading
    /// </summary>
    CacheAllWithChildren = Children | Info | Caption | Classname | Maximized | Minimized | Parent | Owner | Placement | ProcessId | Text | Visible | ScrollInfo,
}