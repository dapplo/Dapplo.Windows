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

using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

#endregion

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    ///     This is a utility class to help to generate input for mouse and keyboard
    /// </summary>
    public static class KeyboardInputGenerator
    {
        /// <summary>
        ///     Generate key down
        /// </summary>
        /// <param name="keycodes">VirtualKeyCodes for the key downs</param>
        /// <returns>number of input events generated</returns>
        public static uint KeyDown(params VirtualKeyCode[] keycodes)
        {
            var keyboardInputs = new KeyboardInput[keycodes.Length];
            var index = 0;
            foreach (var virtualKeyCode in keycodes)
            {
                keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
            }
            return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
        }

        /// <summary>
        ///     Generate a key combination press(es)
        /// </summary>
        /// <param name="keycodes">params VirtualKeyCodes</param>
        public static uint KeyCombinationPress(params VirtualKeyCode[] keycodes)
        {
            var keyboardInputs = new KeyboardInput[keycodes.Length * 2];
            var index = 0;
            // all down
            foreach (var virtualKeyCode in keycodes)
            {
                keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
            }
            // all up
            foreach (var virtualKeyCode in keycodes)
            {
                keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
            }

            return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
        }

        /// <summary>
        ///     Generate key press(es)
        /// </summary>
        /// <param name="keycodes">params VirtualKeyCodes</param>
        public static uint KeyPresses(params VirtualKeyCode[] keycodes)
        {
            var keyboardInputs = new KeyboardInput[keycodes.Length * 2];
            var index = 0;
            foreach (var virtualKeyCode in keycodes)
            {
                keyboardInputs[index++] = KeyboardInput.ForKeyDown(virtualKeyCode);
                keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
            }

            return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
        }

        /// <summary>
        ///     Generate key(s) up
        /// </summary>
        /// <param name="keycodes">VirtualKeyCodes for the keys to release</param>
        /// <returns>number of input events generated</returns>
        public static uint KeyUp(params VirtualKeyCode[] keycodes)
        {
            var keyboardInputs = new KeyboardInput[keycodes.Length];
            var index = 0;
            foreach (var virtualKeyCode in keycodes)
            {
                keyboardInputs[index++] = KeyboardInput.ForKeyUp(virtualKeyCode);
            }
            return NativeInput.SendInput(Structs.Input.CreateKeyboardInputs(keyboardInputs));
        }
    }
}