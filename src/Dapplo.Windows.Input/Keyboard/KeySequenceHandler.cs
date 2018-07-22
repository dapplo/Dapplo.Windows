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
        private readonly IList<IKeyboardHookEventHandler> _keyCombinations;
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
            _keyCombinations = keyCombinations.ToList();
        }

        /// <summary>
        /// Create a KeySequenceHandler for the specified Key Combinations
        /// </summary>
        /// <param name="keyCombinations">params with KeyCombinationHandler</param>
        public KeySequenceHandler(params IKeyboardHookEventHandler[] keyCombinations)
        {
            _keyCombinations = keyCombinations.ToList();
        }

        /// <summary>
        /// This does a reset of the offset
        /// </summary>
        private void Reset()
        {
            _expireAfter = null;
            _offset = 0;
        }

        /// <summary>
        /// Check if the combinations are pressed
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        public bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            // Check if timeout passed
            var now = DateTimeOffset.Now;
            var isExpired = _expireAfter.HasValue && _expireAfter.Value < now;
            if (isExpired || _offset == _keyCombinations.Count)
            {
                Reset();
            }
            var combinationHandled = _keyCombinations[_offset].Handle(keyboardHookEventArgs);
            if (combinationHandled)
            {
                // If a timeout is wanted, this is default, the expireAfter is set
                if (Timeout.HasValue)
                {
                    _expireAfter = now.Add(Timeout.Value);
                }
                _offset++;
                return _offset == _keyCombinations.Count;
            }

            // Don't let the second key pass through
            if (_offset > 0)
            {
                keyboardHookEventArgs.Handled = true;
            }
            if (!keyboardHookEventArgs.IsKeyDown)
            {
                return false;
            }

            // Reset offset, start again
            if (!keyboardHookEventArgs.IsKeyDown && _offset > 0 && !keyboardHookEventArgs.IsModifier)
            {
                Reset();
            }
            return false;
        }
    }
}
