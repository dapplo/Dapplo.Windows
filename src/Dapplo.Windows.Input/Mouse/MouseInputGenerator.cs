// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Input.Mouse;

/// <summary>
///     This is a utility class to help to generate input for the mouse
/// </summary>
public static class MouseInputGenerator
{
    /// <summary>
    ///     Generate mouse button(s) click
    /// </summary>
    /// <param name="mouseButtons">MouseButtons specifying which buttons are pressed</param>
    /// <param name="location">optional NativePoint to specify where the mouse click takes place</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MouseClick(MouseButtons mouseButtons, NativePoint? location = null, uint? timestamp = null)
    {
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(MouseInput.MouseDown(mouseButtons, location, timestamp), MouseInput.MouseUp(mouseButtons, location, timestamp)));
    }

    /// <summary>
    ///     Generate mouse button(s) down
    /// </summary>
    /// <param name="mouseButtons">MouseButtons specifying which buttons are down</param>
    /// <param name="location">optional NativePoint to specify where the mouse down takes place</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MouseDown(MouseButtons mouseButtons, NativePoint? location = null, uint? timestamp = null)
    {
        var mouseInput = MouseInput.MouseDown(mouseButtons, location, timestamp);
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(mouseInput));
    }

    /// <summary>
    ///     Generate mouse button(s) Up
    /// </summary>
    /// <param name="mouseButtons">MouseButtons specifying which buttons are up</param>
    /// <param name="location">optional NativePoint to specify where the mouse up takes place</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MouseUp(MouseButtons mouseButtons, NativePoint? location = null, uint? timestamp = null)
    {
        var mouseInput = MouseInput.MouseUp(mouseButtons, location, timestamp);
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(mouseInput));
    }

    /// <summary>
    ///     Generate mouse moves
    /// </summary>
    /// <param name="location">NativePoint to specify where the mouse moves</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MoveMouse(NativePoint location, uint? timestamp = null)
    {
        var mouseInput = MouseInput.MouseMove(location, timestamp);
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(mouseInput));
    }

    /// <summary>
    ///     Generate mouse wheel moves
    /// </summary>
    /// <param name="wheelDelta"></param>
    /// <param name="location">optional NativePoint to specify where the mouse wheel takes place</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MoveMouseWheel(int wheelDelta, NativePoint? location = null, uint? timestamp = null)
    {
        var mouseInput = MouseInput.MoveMouseWheel(wheelDelta, location, timestamp);
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(mouseInput));
    }

    /// <summary>
    ///     Generate a horizontal mouse wheel movement (MOUSEEVENTF_HWHEEL), a positive value scrolls to the right
    /// </summary>
    /// <param name="wheelDelta">int with the delta, one notch is 120 (WHEEL_DELTA)</param>
    /// <param name="location">optional NativePoint to specify where the mouse wheel takes place, the cursor moves there</param>
    /// <param name="timestamp">The time stamp for the event</param>
    /// <returns>number of input events generated</returns>
    public static uint MoveMouseHorizontalWheel(int wheelDelta, NativePoint? location = null, uint? timestamp = null)
    {
        var mouseInput = MouseInput.MoveMouseHorizontalWheel(wheelDelta, location, timestamp);
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(mouseInput));
    }

    /// <summary>
    ///     Generate a mouse wheel movement at the specified location. Like <see cref="MoveMouseWheel"/> the cursor moves to the
    ///     location, which is where the system delivers the wheel input. With <paramref name="restoreCursor"/> the cursor is moved
    ///     back to where it was afterwards, in the same SendInput call, so the wheel input is still processed at the location.
    /// </summary>
    /// <remarks>
    ///     The location and the restored cursor position are physical screen coordinates; call this from a (per-monitor) DPI aware
    ///     process, otherwise the system scales the coordinates and the cursor doesn't return to exactly the same place.
    /// </remarks>
    /// <param name="wheelDelta">int with the delta, one notch is 120 (WHEEL_DELTA); negative scrolls down, for the horizontal wheel negative scrolls left</param>
    /// <param name="location">NativePoint, in screen coordinates, where the mouse wheel takes place</param>
    /// <param name="restoreCursor">true to move the cursor back to its current location after the wheel input</param>
    /// <param name="horizontal">true for a horizontal wheel movement (MOUSEEVENTF_HWHEEL)</param>
    /// <returns>true when all input events were inserted into the input stream</returns>
    public static bool MoveMouseWheelAt(int wheelDelta, NativePoint location, bool restoreCursor, bool horizontal = false)
    {
        var wheelInput = horizontal
            ? MouseInput.MoveMouseHorizontalWheel(wheelDelta, location)
            : MouseInput.MoveMouseWheel(wheelDelta, location);
        if (!restoreCursor)
        {
            return NativeInput.SendInput(Structs.Input.CreateMouseInputs(wheelInput)) == 1;
        }
        var cursorLocation = User32Api.GetCursorLocation();
        return NativeInput.SendInput(Structs.Input.CreateMouseInputs(wheelInput, MouseInput.MouseMove(cursorLocation))) == 2;
    }
}
