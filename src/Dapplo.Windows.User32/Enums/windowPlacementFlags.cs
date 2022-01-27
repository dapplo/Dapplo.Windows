// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632611(v=vs.85).aspx">WINDOWPLACEMENT structure</a>
/// </summary>
[Flags]
public enum WindowPlacementFlags : uint
{
    /// <summary>
    /// When no flags are used
    /// </summary>
    None = 0,

    /// <summary>
    ///     The coordinates of the minimized window may be specified.
    ///     This flag must be specified if the coordinates are set in the ptMinPosition member.
    /// </summary>
    SetMinPosition = 0x0001,

    /// <summary>
    ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
    ///     the request to the thread that owns the window.
    ///     This prevents the calling thread from blocking its execution while other threads process the request.
    /// </summary>
    AsyncWindowPlacement = 0x0004,

    /// <summary>
    ///     The restored window will be maximized, regardless of whether it was maximized before it was minimized.
    ///     This setting is only valid the next time the window is restored.
    ///     It does not change the default restoration behavior.
    ///     This flag is only valid when the SW_SHOWMINIMIZED value is specified for the showCmd member.
    /// </summary>
    RestoreToMaximized = 0x0002
}