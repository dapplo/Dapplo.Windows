// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     GetWindowsDisplayAffinity Enum values are described here: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum WindowDisplayAffinity
{
    /// <summary>
    /// Non affinity.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enable window contents to be displayed on a monitor.
    /// </summary>
    Monitor = 1
}