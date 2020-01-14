// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

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