// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     This struct is passed in the WH_MOUSE_LL hook
///     See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644970.aspx
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MouseLowLevelHookStruct
{
    /// <summary>
    ///     The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
    /// </summary>
    public NativePoint pt;

    /// <summary>
    ///     If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta.
    ///     The low-order word is reserved. A positive value indicates that the wheel was rotated forward, away from the user;
    ///     a negative value indicates that the wheel was rotated backward, toward the user.
    ///     One wheel click is defined as WHEEL_DELTA, which is 120.
    ///     If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, or
    ///     WM_NCXBUTTONDBLCLK,
    ///     the high-order word specifies which X button was pressed or released, and the low-order word is reserved.
    ///     This value can be one or more of the following values.
    ///     Otherwise, mouseData is not used.
    /// </summary>
    public uint MouseData;

    /// <summary>
    ///     The event-injected flags.
    /// </summary>
    public ExtendedMouseFlags Flags;

    /// <summary>
    ///     The time stamp for this message.
    /// </summary>
    public uint TimeStamp;

    /// <summary>
    ///     Additional information associated with the message.
    /// </summary>
    public UIntPtr dwExtraInfo;
}