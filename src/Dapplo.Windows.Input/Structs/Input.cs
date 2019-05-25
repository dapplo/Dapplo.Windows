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

using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     A struct used by SendInput to store information for synthesizing input events such as keystrokes, mouse movement,
    ///     and mouse clicks.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646270(v=vs.85).aspx">INPUT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        /// <summary>
        ///     The type of the input event. This member can be one of the following values.
        /// </summary>
        public InputTypes InputType;

        /// <summary>
        ///     A union which contains the MouseInput, KeyboardInput or HardwareInput
        /// </summary>
        public InputUnion InputUnion;

        /// <summary>
        ///     A factory method to simplify creating mouse input
        /// </summary>
        /// <returns>Array of Input structs</returns>
        public static Input[] CreateMouseInputs(params MouseInput[] mouseInputs)
        {
            var result = new Input[mouseInputs.Length];
            var index = 0;
            foreach (var mouseInput in mouseInputs)
            {
                result[index++] = new Input
                {
                    InputType = InputTypes.Mouse,
                    InputUnion = new InputUnion
                    {
                        MouseInput = mouseInput
                    }
                };
            }
            return result;
        }

        /// <summary>
        ///     A factory method to simplify creating input
        /// </summary>
        /// <returns>Array of Input structs</returns>
        public static Input[] CreateKeyboardInputs(params KeyboardInput[] keyboardInputs)
        {
            var result = new Input[keyboardInputs.Length];
            var index = 0;
            foreach (var keyboardInput in keyboardInputs)
            {
                result[index++] = new Input
                {
                    InputType = InputTypes.Keyboard,
                    InputUnion = new InputUnion
                    {
                        KeyboardInput = keyboardInput
                    }
                };
            }
            return result;
        }

        /// <summary>
        ///     Used as the Size in the SendInput call
        /// </summary>
        public static int Size => Marshal.SizeOf(typeof(Input));
    }
}