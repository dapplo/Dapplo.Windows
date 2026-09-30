// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
/// The autohide and always-on-top states of an appbar (ABS_*), used with ABM_GETSTATE and ABM_SETSTATE.
/// </summary>
[Flags]
public enum AppBarStates
{
    /// <summary>
    /// ABS_MANUAL - No automatic function
    /// </summary>
    None = 0,
    /// <summary>
    /// ABS_AUTOHIDE - Autohides the AppBar
    /// </summary>
    AutoHide = 1,
    /// <summary>
    /// ABS_ALWAYSONTOP - Make sure the AppBar is always on top 
    /// </summary>
    AlwaysOnTop = 2
}