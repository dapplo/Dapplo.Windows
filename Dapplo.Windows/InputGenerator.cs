//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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

using System.Windows.Forms;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Keyboard.Native;
using Dapplo.Windows.Mouse.Native;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows
{
	/// <summary>
	///     This is a utility class to help to generate input for mouse and keyboard
	/// </summary>
	public static class InputGenerator
	{
		/// <summary>
		///     Generate key down
		/// </summary>
		/// <param name="keycodes">VirtualKeyCodes for the key downs</param>
		/// <returns>number of input events generated</returns>
		public static uint KeyDown(params VirtualKeyCodes[] keycodes)
		{
			var keyboardInputs = new KeyboardInput[keycodes.Length];
			var index = 0;
			foreach (var virtualKeyCode in keycodes)
			{
				keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
			}
			return User32.SendInput(Input.CreateKeyboardInputs(keyboardInputs));
		}

		/// <summary>
		///     Generate key press(es)
		/// </summary>
		/// <param name="keycodes">params VirtualKeyCodes</param>
		public static uint KeyPress(params VirtualKeyCodes[] keycodes)
		{
			var keyboardInputs = new KeyboardInput[keycodes.Length * 2];
			var index = 0;
			foreach (var virtualKeyCode in keycodes)
			{
				keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
				keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
			}

			return User32.SendInput(Input.CreateKeyboardInputs(keyboardInputs));
		}

		/// <summary>
		///     Generate key(s) up
		/// </summary>
		/// <param name="keycodes">VirtualKeyCodes for the keys to release</param>
		/// <returns>number of input events generated</returns>
		public static uint KeyUp(params VirtualKeyCodes[] keycodes)
		{
			var keyboardInputs = new KeyboardInput[keycodes.Length];
			var index = 0;
			foreach (var virtualKeyCode in keycodes)
			{
				keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
			}
			return User32.SendInput(Input.CreateKeyboardInputs(keyboardInputs));
		}

		/// <summary>
		///     Generate mouse button(s) click
		/// </summary>
		/// <param name="mouseButtons">MouseButtons specifying which buttons are pressed</param>
		/// <param name="location">optional POINT to specify where the mouse click takes place</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>number of input events generated</returns>
		public static uint MouseClick(MouseButtons mouseButtons, POINT? location = null, uint timestamp = 0)
		{
			return User32.SendInput(Input.CreateMouseInputs(MouseInput.MouseDown(mouseButtons, location, timestamp), MouseInput.MouseDown(mouseButtons, location, timestamp)));
		}

		/// <summary>
		///     Generate mouse button(s) down
		/// </summary>
		/// <param name="mouseButtons">MouseButtons specifying which buttons are down</param>
		/// <param name="location">optional POINT to specify where the mouse down takes place</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>number of input events generated</returns>
		public static uint MouseDown(MouseButtons mouseButtons, POINT? location = null, uint timestamp = 0)
		{
			var mouseWheelInput = MouseInput.MouseDown(mouseButtons, location, timestamp);
			return User32.SendInput(Input.CreateMouseInputs(mouseWheelInput));
		}

		/// <summary>
		///     Generate mouse button(s) Up
		/// </summary>
		/// <param name="mouseButtons">MouseButtons specifying which buttons are up</param>
		/// <param name="location">optional POINT to specify where the mouse up takes place</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>number of input events generated</returns>
		public static uint MouseUp(MouseButtons mouseButtons, POINT? location = null, uint timestamp = 0)
		{
			var mouseWheelInput = MouseInput.MouseUp(mouseButtons, location, timestamp);
			return User32.SendInput(Input.CreateMouseInputs(mouseWheelInput));
		}

		/// <summary>
		///     Generate mouse wheel moves
		/// </summary>
		/// <param name="location">POINT to specify where the mouse moves</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>number of input events generated</returns>
		public static uint MoveMouse(POINT location, uint timestamp = 0)
		{
			var mouseMoveInput = MouseInput.MouseMove(location, timestamp);
			return User32.SendInput(Input.CreateMouseInputs(mouseMoveInput));
		}

		/// <summary>
		///     Generate mouse wheel moves
		/// </summary>
		/// <param name="wheelDelta"></param>
		/// <param name="location">optional POINT to specify where the mouse wheel takes place</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>number of input events generated</returns>
		public static uint MoveMouseWheel(int wheelDelta, POINT? location = null, uint timestamp = 0)
		{
			var mouseWheelInput = MouseInput.MoveMouseWheel(wheelDelta, location, timestamp);
			return User32.SendInput(Input.CreateMouseInputs(mouseWheelInput));
		}
	}
}