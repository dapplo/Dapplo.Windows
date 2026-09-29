// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Input.Structs;

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
    ///     The coordinates need to be mapped to 0-65535 where 0 is the left/top and 65535 is the right/bottom of the virtual desktop
    ///     (MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK)
    /// </summary>
    /// <param name="location">NativePoint in screen coordinates</param>
    /// <returns>NativePoint with normalized coordinates</returns>
    private static NativePoint RemapLocation(NativePoint location)
    {
        var virtualScreenX = User32Api.GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
        var virtualScreenY = User32Api.GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
        var virtualScreenWidth = User32Api.GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
        var virtualScreenHeight = User32Api.GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);
        return new NativePoint(
            NormalizeCoordinate(location.X, virtualScreenX, virtualScreenWidth),
            NormalizeCoordinate(location.Y, virtualScreenY, virtualScreenHeight));
    }

    /// <summary>
    ///     Map a screen coordinate to the normalized absolute range 0-65535 which SendInput uses,
    ///     where the origin maps to 0 and origin + size - 1 maps to 65535.
    /// </summary>
    /// <param name="coordinate">int with the screen coordinate (x or y)</param>
    /// <param name="origin">int with the origin of the (virtual) screen, e.g. SM_XVIRTUALSCREEN</param>
    /// <param name="size">int with the size of the (virtual) screen, e.g. SM_CXVIRTUALSCREEN</param>
    /// <returns>int between 0 and 65535</returns>
    public static int NormalizeCoordinate(int coordinate, int origin, int size)
    {
        if (size <= 1)
        {
            return 0;
        }
        var normalized = Math.Round(((double)coordinate - origin) * 65535.0 / (size - 1));
        return (int)Math.Max(0, Math.Min(65535, normalized));
    }

    /// <summary>
    ///     Create a MouseInput struct for a wheel move
    /// </summary>
    /// <param name="wheelDelta">How much does the wheel move</param>
    /// <param name="location">Location of the event</param>
    /// <param name="timestamp">The time stamp for the event, null or 0 lets the system provide the time stamp</param>
    /// <returns>MouseInput</returns>
    public static MouseInput MoveMouseWheel(int wheelDelta, NativePoint? location = null, uint? timestamp = null)
    {
        if (location.HasValue)
        {
            location = RemapLocation(location.Value);
        }
        var mouseEventFlags = location.HasValue ? MouseMoveMouseEventFlags : MouseEventFlags.None;
        var messageTime = timestamp ?? 0;
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
    /// <param name="timestamp">The time stamp for the event, null or 0 lets the system provide the time stamp</param>
    /// <returns>MouseInput</returns>
    public static MouseInput MouseMove(NativePoint location, uint? timestamp = null)
    {
        location = RemapLocation(location);
        var messageTime = timestamp ?? 0;
        return new MouseInput
        {
            dx = location.X,
            dy = location.Y,
            Timestamp = messageTime,
            MouseEventFlags = MouseMoveMouseEventFlags
        };
    }

    /// <summary>
    ///     Create a MouseInput struct for a mouse button down
    /// </summary>
    /// <param name="mouseButtons">MouseButtons to specify which mouse buttons</param>
    /// <param name="location">Where is the click located</param>
    /// <param name="timestamp">The time stamp for the event, null or 0 lets the system provide the time stamp</param>
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
        var messageTime = timestamp ?? 0;
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
    /// <param name="timestamp">The time stamp for the event, null or 0 lets the system provide the time stamp</param>
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
        var messageTime = timestamp ?? 0;
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