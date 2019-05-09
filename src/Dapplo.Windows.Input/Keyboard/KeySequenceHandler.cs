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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// This is an IKeyboardHookEventHandler which can handle sequences of key combinations.
    /// </summary>
    public class KeySequenceHandler : IKeyboardHookEventHandler
    {
        private IKeyboardHookEventHandler[] _keyboardHookEventHandlers;
        private bool[] _isHandled;
        private int _offset;
        private DateTimeOffset? _expireAfter;

        /// <summary>
        /// This sets the timeout time between key presses.
        /// If the user waits longer than this TimeSpan, the sequence is reset to the start.
        /// </summary>
        public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Create a KeySequenceHandler for the specified Key Combinations
        /// </summary>
        /// <param name="keyCombinations">IEnumerable with KeyCombinationHandler</param>
        public KeySequenceHandler(IEnumerable<IKeyboardHookEventHandler> keyCombinations)
        {
            Configure(keyCombinations);
        }

        /// <summary>
        /// Create a KeySequenceHandler for the specified Key Combinations
        /// </summary>
        /// <param name="keyCombinations">params with KeyCombinationHandler</param>
        public KeySequenceHandler(params IKeyboardHookEventHandler[] keyCombinations)
        {
            Configure(keyCombinations);
        }

        /// <summary>
        /// Private method to configure the fields
        /// </summary>
        /// <param name="keyCombinations">IEnumerable of IKeyboardHookEventHandler</param>
        private void Configure(IEnumerable<IKeyboardHookEventHandler> keyCombinations)
        {
            _keyboardHookEventHandlers = keyCombinations.ToArray();
            _isHandled = new bool[_keyboardHookEventHandlers.Length];
        }

        /// <summary>
        /// This does a reset of the offset
        /// </summary>
        private void Reset()
        {
            _expireAfter = null;
            _offset = 0;
            for (var i = 0; i < _isHandled.Length; i++)
            {
                _isHandled[i] = false;
            }
        }

        /// <summary>
        /// Get the current handler
        /// </summary>
        private IKeyboardHookEventHandler CurrentHandler => _keyboardHookEventHandlers[_offset];

        /// <summary>
        /// Helper method to advance a handler
        /// </summary>
        /// <returns>bool with true if we advanced</returns>
        private bool AdvanceHandler()
        {
            if (Timeout.HasValue)
            {
                _expireAfter = DateTimeOffset.Now.Add(Timeout.Value);
            }
            return ++_offset < _keyboardHookEventHandlers.Length;
        }

        /// <summary>
        /// Check if the combinations are pressed
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        public bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            var currentHandled = CurrentHandler.Handle(keyboardHookEventArgs);
            var currentNotPressed = !CurrentHandler.HasKeysPressed;
            if (currentHandled)
            {
                _isHandled[_offset] = true;
            }
            else if (!keyboardHookEventArgs.IsKeyDown && _offset > 0 && currentNotPressed && !keyboardHookEventArgs.IsModifier)
            {
                Reset();
            }

            // Check if timeout passed, to reset the sequence
            if (_expireAfter.HasValue)
            {
                var isExpired = _expireAfter.Value < DateTimeOffset.Now;
                if (isExpired)
                {
                    if (currentNotPressed)
                    {
                        Reset();
                    }
                    return false;
                }
            }

            var allHandled = _isHandled.All(b => b);

            // check if we need to advance
            if (_isHandled[_offset] && currentNotPressed && !AdvanceHandler())
            {
                // Advance
                Reset();
            }

            return currentHandled && allHandled;
        }

        /// <inheritdoc />
        public bool HasKeysPressed => _keyboardHookEventHandlers[_offset].HasKeysPressed;
    }
}
