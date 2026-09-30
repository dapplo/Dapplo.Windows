// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Input.Mouse;

/// <summary>
///     Information on mouse changes
///     TODO: Make the information a lot clearer, than processing WindowsMessages
/// </summary>
public class MouseHookEventArgs : EventArgs
{
    /// <summary>
    ///     Set this to true to swallow the event, other applications will not see it.
    ///     Only honoured when set synchronously in a subscriber of <see cref="MouseHook.MouseEvents"/>.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
    /// </summary>
    public NativePoint Point { get; set; }

    /// <summary>
    ///     The mouse message
    /// </summary>
    public WindowsMessages WindowsMessage { get; set; }

    /// <summary>
    ///     The mouseData of the MSLLHOOKSTRUCT, see <see cref="WheelDelta"/> and <see cref="XButton"/> for the interpreted values.
    /// </summary>
    public uint MouseData { get; set; }

    /// <summary>
    ///     For WM_MOUSEWHEEL and WM_MOUSEHWHEEL: the wheel delta, a positive value indicates that the wheel was rotated forward (away from the user) or to the right.
    ///     One wheel click is 120 (WHEEL_DELTA).
    /// </summary>
    public short WheelDelta => unchecked((short)(MouseData >> 16));

    /// <summary>
    ///     For WM_XBUTTONDOWN, WM_XBUTTONUP and WM_XBUTTONDBLCLK: which X button was pressed or released, 1 for XBUTTON1 and 2 for XBUTTON2.
    /// </summary>
    public ushort XButton => unchecked((ushort)(MouseData >> 16));

    /// <summary>
    ///     The event-injected flags
    /// </summary>
    public ExtendedMouseFlags Flags { get; set; }

    /// <summary>
    ///     Test if this event was injected (e.g. by SendInput)
    /// </summary>
    public bool IsInjectedByProcess => (Flags & ExtendedMouseFlags.Injected) != 0;

    /// <summary>
    ///     Test if this event was injected by a process running at a lower integrity level
    /// </summary>
    public bool IsInjectedByLowerIntegrityLevelProcess => (Flags & ExtendedMouseFlags.Injected) != 0 && (Flags & ExtendedMouseFlags.LowerIntegrityInjected) != 0;

    /// <summary>
    ///     The time stamp of the event, equivalent to what GetMessageTime would return for this message.
    /// </summary>
    public uint TimeStamp { get; set; }
}
