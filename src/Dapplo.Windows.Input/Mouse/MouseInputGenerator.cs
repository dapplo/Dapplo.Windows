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

using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

#endregion

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
