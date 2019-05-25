//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Text;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    ///     Information on keyboard changes
    /// </summary>
    public class KeyboardHookEventArgs : EventArgs
    {
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
        /// Detaila on the keyboard event
        /// </summary>
        public ExtendedKeyFlags Flags { get; internal set; } = ExtendedKeyFlags.None;

        /// <summary>
        /// Test if this event is injected by another process
        /// </summary>
        public bool IsInjectedByProcess => (Flags & ExtendedKeyFlags.Injected) != 0;

        /// <summary>
        /// Test if this event is injected by another process with a lower integrity level
        /// </summary>
        public bool IsInjectedByLowerInterityLevelProcess => (Flags & ExtendedKeyFlags.Injected) != 0 && (Flags & ExtendedKeyFlags.LowerIntegretyInjected) != 0;

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
}