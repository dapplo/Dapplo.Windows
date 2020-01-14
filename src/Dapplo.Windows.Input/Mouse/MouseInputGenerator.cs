// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input.Mouse
{
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
            return NativeInput.SendInput(Structs.Input.CreateMouseInputs(MouseInput.MouseDown(mouseButtons, location, timestamp), MouseInput.MouseDown(mouseButtons, location, timestamp)));
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
    }
}
