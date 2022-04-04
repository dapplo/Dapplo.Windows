// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     Flags for the CURSOR_INFO "flags" field, see:
///     https://msdn.microsoft.com/en-us/library/windows/desktop/ms648381.aspx
/// </summary>
[Flags]
public enum CursorInfoFlags : uint
{
    /// <summary>
    ///     Cursor is hidden
    /// </summary>
    Hidden = 0,

    /// <summary>
    ///     Cursor is showing
    /// </summary>
    Showing = 1,

    /// <summary>
    ///     Cursor is suppressed
    /// </summary>
    Suppressed = 2
}