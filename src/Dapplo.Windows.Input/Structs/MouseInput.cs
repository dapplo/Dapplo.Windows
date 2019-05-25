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
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.User32;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about a simulated mouse event.
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646273(v=vs.85).aspx">MOUSEINPUT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        private const MouseEventFlags MouseMoveMouseEventFlags = MouseEventFlags.Absolute | MouseEventFlags.Virtualdesk | MouseEventFlags.Move;

        /// <summary>
        ///     The absolute position of the mouse, or the amount of motion since the last mouse event was generated,
        ///     depending on the value of the dwFlags member.
        ///     Absolute data is specified as the x coordinate of the mouse;
        ///     relative data is specified as the number of pixels moved.
        /// </summary>
        private int dx;

        /// <summary>
        ///     The absolute position of the mouse, or the amount of motion since the last mouse event was generated,
        ///     depending on the value of the dwFlags member.
        ///     Absolute data is specified as the y coordinate of the mouse;
        ///     relative data is specified as the number of pixels moved.
        /// </summary>
        private int dy;

        /// <summary>
        ///     If dwFlags contains MOUSEEVENTF_WHEEL, then mouseData specifies the amount of wheel movement.
        ///     A positive value indicates that the wheel was rotated forward, away from the user;
        ///     a negative value indicates that the wheel was rotated backward, toward the user.
        ///     One wheel click is defined as WHEEL_DELTA, which is 120.
        ///     Windows Vista: If dwFlags contains MOUSEEVENTF_HWHEEL, then dwData specifies the amount of wheel movement.
        ///     A positive value indicates that the wheel was rotated to the right;
        ///     a negative value indicates that the wheel was rotated to the left.
        ///     One wheel click is defined as WHEEL_DELTA, which is 120.
        ///     If dwFlags does not contain MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, or MOUSEEVENTF_XUP, then mouseData should be
        ///     zero.
        ///     If dwFlags contains MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP, then mouseData specifies which X buttons were pressed or
        ///     released.
        ///     This value may be any combination of the following flags:
        ///     XBUTTON1 0x0001 Set if the first X button is pressed or released.
        ///     XBUTTON2 0x0002 Set if the second X button is pressed or released.
        /// </summary>
        private int MouseData;

        /// <summary>
        ///     A set of bit flags that specify various aspects of mouse motion and button clicks.
        ///     The bits in this member can be any reasonable combination of the following values.
        /// </summary>
        private MouseEventFlags MouseEventFlags;

        /// <summary>
        ///     The time stamp for the event, in milliseconds. If this parameter is 0, the system will provide its own time stamp.
        /// </summary>
        private uint Timestamp;

        /// <summary>
        ///     An additional value associated with the mouse event. An application calls GetMessageExtraInfo to obtain this extra
        ///     information.
        /// </summary>
        private readonly UIntPtr dwExtraInfo;

        /// <summary>
        ///     The coordinates need to be mapped from 0-65535 where 0 is left and 65535 is right
        /// </summary>
        /// <param name="location">NativePoint</param>
        /// <returns>NativePoint</returns>
        private static NativePoint RemapLocation(NativePoint location)
        {
            var bounds = DisplayInfo.ScreenBounds;
            if (bounds.Width * bounds.Height == 0)
            {
                return location;
            }
            return new NativePoint(location.X * (65535 / bounds.Width), location.Y * (65535 / bounds.Height));
        }

        /// <summary>
        ///     Create a MouseInput struct for a wheel move
        /// </summary>
        /// <param name="wheelDelta">How much does the wheel move</param>
        /// <param name="location">Location of the event</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>MouseInput</returns>
        public static MouseInput MoveMouseWheel(int wheelDelta, NativePoint? location = null, uint? timestamp = null)
        {
            if (location.HasValue)
            {
                location = RemapLocation(location.Value);
            }
            var mouseEventFlags = location.HasValue ? MouseMoveMouseEventFlags : MouseEventFlags.None;
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new MouseInput
            {
                MouseData = wheelDelta,
                dx = location?.X ?? 0,
                dy = location?.Y ?? 0,
                Timestamp = messageTime,
                MouseEventFlags = mouseEventFlags | MouseEventFlags.Wheel
            };
        }

        /// <summary>
        ///     Create a MouseInput struct for a mouse move
        /// </summary>
        /// <param name="location">Where is the click located</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>MouseInput</returns>
        public static MouseInput MouseMove(NativePoint location, uint? timestamp = null)
        {
            location = RemapLocation(location);
            var bounds = DisplayInfo.ScreenBounds;
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new MouseInput
            {
                dx = location.X * (65535 / bounds.Width),
                dy = location.Y * (65535 / bounds.Height),
                Timestamp = messageTime,
                MouseEventFlags = MouseMoveMouseEventFlags
            };
        }

        /// <summary>
        ///     Create a MouseInput struct for a mouse button down
        /// </summary>
        /// <param name="mouseButtons">MouseButtons to specify which mouse buttons</param>
        /// <param name="location">Where is the click located</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>MouseInput</returns>
        public static MouseInput MouseDown(MouseButtons mouseButtons, NativePoint? location = null, uint? timestamp = null)
        {
            if (location.HasValue)
            {
                location = RemapLocation(location.Value);
            }
            var mouseEventFlags = location.HasValue ? MouseMoveMouseEventFlags : MouseEventFlags.None;

            if ((mouseButtons & MouseButtons.Left) != 0)
            {
                mouseEventFlags |= MouseEventFlags.LeftDown;
            }
            if ((mouseButtons & MouseButtons.Right) != 0)
            {
                mouseEventFlags |= MouseEventFlags.RightDown;
            }
            if ((mouseButtons & MouseButtons.Middle) != 0)
            {
                mouseEventFlags |= MouseEventFlags.MiddleDown;
            }
            var mouseData = 0;
            if ((mouseButtons & MouseButtons.XButton1) != 0)
            {
                mouseEventFlags |= MouseEventFlags.XDown;
                mouseData |= 1;
            }
            if ((mouseButtons & MouseButtons.XButton2) != 0)
            {
                mouseEventFlags |= MouseEventFlags.XDown;
                mouseData |= 2;
            }
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new MouseInput
            {
                dx = location?.X ?? 0,
                dy = location?.Y ?? 0,
                Timestamp = messageTime,
                MouseData = mouseData,
                MouseEventFlags = mouseEventFlags
            };
        }

        /// <summary>
        ///     Create a MouseInput struct for a mouse button up
        /// </summary>
        /// <param name="mouseButtons">MouseButtons to specify which mouse buttons</param>
        /// <param name="location">Where is the click located</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>MouseInput</returns>
        public static MouseInput MouseUp(MouseButtons mouseButtons, NativePoint? location = null, uint? timestamp = null)
        {
            if (location.HasValue)
            {
                location = RemapLocation(location.Value);
            }
            var mouseEventFlags = location.HasValue ? MouseMoveMouseEventFlags : MouseEventFlags.None;

            if ((mouseButtons & MouseButtons.Left) != 0)
            {
                mouseEventFlags |= MouseEventFlags.LeftUp;
            }
            if ((mouseButtons & MouseButtons.Right) != 0)
            {
                mouseEventFlags |= MouseEventFlags.RightUp;
            }
            if ((mouseButtons & MouseButtons.Middle) != 0)
            {
                mouseEventFlags |= MouseEventFlags.MiddleUp;
            }
            var mouseData = 0;
            if ((mouseButtons & MouseButtons.XButton1) != 0)
            {
                mouseEventFlags |= MouseEventFlags.XUp;
                mouseData |= 1;
            }
            if ((mouseButtons & MouseButtons.XButton2) != 0)
            {
                mouseEventFlags |= MouseEventFlags.XUp;
                mouseData |= 2;
            }
            var messageTime = timestamp ?? (uint)Environment.TickCount;
            return new MouseInput
            {
                dx = location?.X ?? 0,
                dy = location?.Y ?? 0,
                Timestamp = messageTime,
                MouseData = mouseData,
                MouseEventFlags = mouseEventFlags
            };
        }
    }
}