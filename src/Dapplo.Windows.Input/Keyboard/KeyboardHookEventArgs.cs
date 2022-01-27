// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Text;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
///     Information on keyboard changes
/// </summary>
public class KeyboardHookEventArgs : EventArgs
{
    /// <summary>
    /// Generate KeyboardHookEventArgs for a key down
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>KeyboardHookEventArgs</returns>
    public static KeyboardHookEventArgs KeyDown(VirtualKeyCode virtualKeyCode)
    {
        return new KeyboardHookEventArgs
        {
            Key = virtualKeyCode,
            IsKeyDown = true,
            IsModifier = virtualKeyCode.IsModifier()
        };
    }

    /// <summary>
    /// Generate KeyboardHookEventArgs for a key up
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>KeyboardHookEventArgs</returns>
    public static KeyboardHookEventArgs KeyUp(VirtualKeyCode virtualKeyCode)
    {
        return new KeyboardHookEventArgs
        {
            Key = virtualKeyCode,
            IsKeyDown = false,
            IsModifier = virtualKeyCode.IsModifier()
        };
    }

    /// <summary>
    ///     Set this to true if the event is handled, other event-handlers in the chain will not be called
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Specifies if this event is for a modifier key (shift, control, alt etc)
    /// </summary>
    public bool IsModifier { get; internal set; }

    /// <summary>
    ///     True if Alt key is pressed
    /// </summary>
    public bool IsAlt => IsLeftAlt || IsRightAlt;

    /// <summary>
    ///     Is the caps-lock currently active?
    /// </summary>
    public bool IsCapsLockActive { get; internal set; }

    /// <summary>
    ///     True if control is pressed
    /// </summary>
    public bool IsControl => IsLeftControl || IsRightControl;

    /// <summary>
    ///     Is this a key down, else it's up
    /// </summary>
    public bool IsKeyDown { get; internal set; }

    /// <summary>
    ///     Is the alt on the left side pressed?
    /// </summary>
    public bool IsLeftAlt { get; internal set; }

    /// <summary>
    ///     Is the control on the left side pressed?
    /// </summary>
    public bool IsLeftControl { get; internal set; }

    /// <summary>
    ///     Is the shift on the left side pressed?
    /// </summary>
    public bool IsLeftShift { get; internal set; }

    /// <summary>
    ///     Is the windows key on the left side pressed?
    /// </summary>
    public bool IsLeftWindows { get; internal set; }

    /// <summary>
    ///     Is the num-lock currently active?
    /// </summary>
    public bool IsNumLockActive { get; internal set; }

    /// <summary>
    ///     Is the alt on the right side pressed?
    /// </summary>
    public bool IsRightAlt { get; internal set; }

    /// <summary>
    ///     Is the control on the right side pressed?
    /// </summary>
    public bool IsRightControl { get; internal set; }

    /// <summary>
    ///     Is the shift on the right side pressed?
    /// </summary>
    public bool IsRightShift { get; internal set; }

    /// <summary>
    ///     Is the windows key on the right side pressed?
    /// </summary>
    public bool IsRightWindows { get; internal set; }

    /// <summary>
    ///     Is the scroll-lock currently active?
    /// </summary>
    public bool IsScrollLockActive { get; internal set; }

    /// <summary>
    ///     True if shift is pressed
    /// </summary>
    public bool IsShift => IsLeftShift || IsRightShift;

    /// <summary>
    ///     Is this a system key
    /// </summary>
    public bool IsSystemKey { get; internal set; }

    /// <summary>
    ///     True if shift is pressed
    /// </summary>
    public bool IsWindows => IsLeftWindows || IsRightWindows;

    /// <summary>
    ///     The key code itself
    /// </summary>
    public VirtualKeyCode Key { get; internal set; } = VirtualKeyCode.None;

    /// <summary>
    /// Timestamp of the event, a DateTime can be calculated by using EventTime instead
    /// </summary>
    public uint TimeStamp { get; internal set; }

    /// <summary>
    /// Returns the DateTimeOffset for this event
    /// </summary>
    public DateTimeOffset EventTime
    {
        get
        {
            var runningTimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount - TimeStamp);
            return DateTimeOffset.Now.Subtract(runningTimeSpan);
        }
    }

    /// <summary>
    /// Details on the keyboard event
    /// </summary>
    public ExtendedKeyFlags Flags { get; internal set; } = ExtendedKeyFlags.None;

    /// <summary>
    /// Test if this event is injected by another process
    /// </summary>
    public bool IsInjectedByProcess => (Flags & ExtendedKeyFlags.Injected) != 0;

    /// <summary>
    /// Test if this event is injected by another process with a lower integrity level
    /// </summary>
    public bool IsInjectedByLowerIntegrityLevelProcess => (Flags & ExtendedKeyFlags.Injected) != 0 && (Flags & ExtendedKeyFlags.LowerIntegretyInjected) != 0;

    /// <inheritdoc />
    public override string ToString()
    {
        var dump = new StringBuilder();
        if (!IsModifier)
        {
            if (IsShift)
            {
                if (IsLeftShift)
                {
                    dump.Append("left ");
                }
                if (IsRightShift)
                {
                    dump.Append("right ");
                }
                dump.Append("shift +");
            }

            if (IsControl)
            {
                if (IsLeftControl)
                {
                    dump.Append("left ");
                }
                if (IsRightControl)
                {
                    dump.Append("right ");
                }
                dump.Append("control +");
            }
            if (IsLeftAlt)
            {
                dump.Append(" with left-alt");
            }
            if (IsRightAlt)
            {
                dump.Append(" with right-alt");
            }
            if (IsLeftWindows)
            {
                dump.Append(" with left-windows");
            }
            if (IsRightWindows)
            {
                dump.Append(" with right-windows");
            }
        }

        dump.Append(Key).Append(IsKeyDown ? " down" : " up");
        dump.Append(Handled ? " (" : " (not ").Append("handled)");
        if (IsScrollLockActive)
        {
            dump.Append(" ScrollLocked");
        }
        if (IsNumLockActive)
        {
            dump.Append(" NumLock active");
        }
        if (IsCapsLockActive)
        {
            dump.Append(" CapsLock active");
        }
        return dump.ToString();
    }
}