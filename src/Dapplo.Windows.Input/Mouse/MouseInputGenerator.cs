//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.User32;

#endregion

namespace Dapplo.Windows.Input.Mouse
{
    /// <summary>
    ///     This is a utility class to help to generate input for mouse and keyboard
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
        public static uint MouseClick(MouseButtons mouseButtons, NativePoint? location = null, uint timestamp = 0)
        {
            return SendInput(Structs.Input.CreateMouseInputs(MouseInput.MouseDown(mouseButtons, location, timestamp), MouseInput.MouseDown(mouseButtons, location, timestamp)));
        }

        /// <summary>
        ///     Generate mouse button(s) down
        /// </summary>
        /// <param name="mouseButtons">MouseButtons specifying which buttons are down</param>
        /// <param name="location">optional NativePoint to specify where the mouse down takes place</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>number of input events generated</returns>
        public static uint MouseDown(MouseButtons mouseButtons, NativePoint? location = null, uint timestamp = 0)
        {
            var mouseWheelInput = MouseInput.MouseDown(mouseButtons, location, timestamp);
            return SendInput(Structs.Input.CreateMouseInputs(mouseWheelInput));
        }

        /// <summary>
        ///     Generate mouse button(s) Up
        /// </summary>
        /// <param name="mouseButtons">MouseButtons specifying which buttons are up</param>
        /// <param name="location">optional NativePoint to specify where the mouse up takes place</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>number of input events generated</returns>
        public static uint MouseUp(MouseButtons mouseButtons, NativePoint? location = null, uint timestamp = 0)
        {
            var mouseWheelInput = MouseInput.MouseUp(mouseButtons, location, timestamp);
            return SendInput(Structs.Input.CreateMouseInputs(mouseWheelInput));
        }

        /// <summary>
        ///     Generate mouse wheel moves
        /// </summary>
        /// <param name="location">NativePoint to specify where the mouse moves</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>number of input events generated</returns>
        public static uint MoveMouse(NativePoint location, uint timestamp = 0)
        {
            var mouseMoveInput = MouseInput.MouseMove(location, timestamp);
            return SendInput(Structs.Input.CreateMouseInputs(mouseMoveInput));
        }

        /// <summary>
        ///     Generate mouse wheel moves
        /// </summary>
        /// <param name="wheelDelta"></param>
        /// <param name="location">optional NativePoint to specify where the mouse wheel takes place</param>
        /// <param name="timestamp">The time stamp for the event</param>
        /// <returns>number of input events generated</returns>
        public static uint MoveMouseWheel(int wheelDelta, NativePoint? location = null, uint timestamp = 0)
        {
            var mouseWheelInput = MouseInput.MoveMouseWheel(wheelDelta, location, timestamp);
            return SendInput(Structs.Input.CreateMouseInputs(mouseWheelInput));
        }


        /// <summary>
        ///     Wrapper to simplify sending of inputs
        /// </summary>
        /// <param name="inputs">Input array</param>
        /// <returns>inputs send</returns>
        private static uint SendInput(Structs.Input[] inputs)
        {
            return SendInput((uint)inputs.Length, inputs, Structs.Input.Size);
        }

        #region DllImports
        /// <summary>
        ///     Synthesizes keystrokes, mouse motions, and button clicks.
        ///     The function returns the number of events that it successfully inserted into the keyboard or mouse input stream.
        ///     If the function returns zero, the input was already blocked by another thread.
        ///     To get extended error information, call GetLastError.
        /// </summary>
        [DllImport(User32Api.User32, SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] [In] Structs.Input[] inputs, int cbSize);
        #endregion
    }
}