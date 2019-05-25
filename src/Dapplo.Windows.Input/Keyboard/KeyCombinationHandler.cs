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

using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// This is an IKeyboardHookEventHandler which can handle a combination of VirtualKeyCode presses.
    /// </summary>
    public class KeyCombinationHandler : IKeyboardHookEventHandler
    {
        /// <summary>
        /// The keys that do not makeup the combination, where there are any Handle cannot return true
        /// </summary>
        protected ISet<VirtualKeyCode> OtherPressedKeys { get; } = new HashSet<VirtualKeyCode>();

        /// <summary>
        /// An array with all the current available keys, the locations represent the TriggerCombination array.
        /// </summary>
        protected bool[] AvailableKeys;

        /// <summary>
        /// Get the VirtualKeyCodes which trigger the combination
        /// </summary>
        public VirtualKeyCode[] TriggerCombination {get; protected set; }

        /// <summary>
        /// Defines if repeats are allowed, default is false
        /// </summary>
        public bool CanRepeat { get; set; } = false;

        /// <summary>
        /// Defines if generated (injected) key presses need to be ignored,
        /// By default (true) only "real" key presses are handled
        /// </summary>
        public bool IgnoreInjected { get; set; } = true;

        /// <summary>
        /// Defines if the key press needs to be passed through to other applications.
        /// By default (false) a keypress which is specified is marked as handled and will not be seen by others
        /// </summary>
        public bool IsPassthrough { get; set; }

        /// <summary>
        /// Create a KeyCombinationHandler for the specified VirtualKeyCodes
        /// </summary>
        /// <param name="keyCombination">IEnumerable with VirtualKeyCodes</param>
        public KeyCombinationHandler(IEnumerable<VirtualKeyCode> keyCombination)
        {
            Configure(keyCombination);
        }

        /// <summary>
        /// Configure the settings
        /// </summary>
        /// <param name="keyCombination">IEnumerable of VirtualKeyCode</param>
        protected void Configure(IEnumerable<VirtualKeyCode> keyCombination)
        {
            TriggerCombination = keyCombination.Distinct().ToArray();
            AvailableKeys = new bool[TriggerCombination.Length];
        }

        /// <summary>
        /// Create a KeyCombinationHandler for the specified VirtualKeyCodes
        /// </summary>
        /// <param name="keyCombination">params with VirtualKeyCodes</param>
        public KeyCombinationHandler(params VirtualKeyCode[] keyCombination)
        {
            Configure(keyCombination);
        }

        /// <summary>
        /// Handle key presses to test if the combination is available
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        public virtual bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (IgnoreInjected && keyboardHookEventArgs.IsInjectedByProcess)
            {
                return false;
            }

            bool keyMatched = false;
            bool isRepeat = false;
            for (int i = 0; i < TriggerCombination.Length; i++)
            {
                if (!CompareVirtualKeyCode(keyboardHookEventArgs.Key, TriggerCombination[i]))
                {
                    continue;
                }

                isRepeat = AvailableKeys[i];
                AvailableKeys[i] = keyboardHookEventArgs.IsKeyDown;
                keyMatched = true;
                break;
            }

            if (!keyMatched)
            {
                if (keyboardHookEventArgs.IsKeyDown)
                {
                    OtherPressedKeys.Add(keyboardHookEventArgs.Key);
                }
                else
                {
                    OtherPressedKeys.Remove(keyboardHookEventArgs.Key);
                }
            }

            //  check if this combination is handled
            var isHandled = keyboardHookEventArgs.IsKeyDown && OtherPressedKeys.Count == 0 && AvailableKeys.All(b => b);

            // Mark as handled if the key combination is handled and we don't have passthrough
            if (isHandled && !IsPassthrough)
            {
                keyboardHookEventArgs.Handled = true;
            }

            // Do not return a true, if this is a repeat and CanRepeat is disabled
            if (!CanRepeat && isRepeat)
            {
                return false;
            }
            return isHandled;
        }

        /// <inheritdoc />
        public bool HasKeysPressed => OtherPressedKeys.Count > 0 || AvailableKeys.Any(b => b);

        /// <summary>
        /// Helper method to compare VirtualKeyCode
        /// </summary>
        /// <param name="current">VirtualKeyCode</param>
        /// <param name="expected">VirtualKeyCode</param>
        /// <returns>bool true if match</returns>
        protected virtual bool CompareVirtualKeyCode(VirtualKeyCode current, VirtualKeyCode expected)
        {
            if (current == expected)
            {
                return true;
            }

            switch (expected)
            {
                case VirtualKeyCode.Shift:
                    return VirtualKeyCode.LeftShift == current || VirtualKeyCode.RightShift == current;
                case VirtualKeyCode.Control:
                    return VirtualKeyCode.LeftControl == current || VirtualKeyCode.RightControl == current;
                case VirtualKeyCode.Menu:
                    return VirtualKeyCode.LeftMenu == current || VirtualKeyCode.RightMenu == current;
            }

            return false;
        }
    }
}
