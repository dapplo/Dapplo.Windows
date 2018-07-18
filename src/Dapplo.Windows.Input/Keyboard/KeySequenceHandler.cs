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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// This defines a certain key sequence which needs to be fullfilled
    /// </summary>
    public class KeySequenceHandler : IKeyboardHookEventHandler
    {
        private readonly IList<IKeyboardHookEventHandler> _keyCombinations;
        private int _offset = 0;

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
        /// Check if the combinations are pressed
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        public bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            if (_offset == _keyCombinations.Count)
            {
                _offset = 0;
            }
            var combinationHandled = _keyCombinations[_offset].Handle(keyboardHookEventArgs);
            if (combinationHandled)
            {
                Debug.WriteLine("Combination handled");
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

            Debug.WriteLine("Combination not handled");

            // Reset offset, start again
            if (!keyboardHookEventArgs.IsKeyDown && _offset > 0 && !keyboardHookEventArgs.IsModifier)
            {
                Debug.WriteLine("Reset offset");
                _offset = 0;
            }
            return false;
        }
    }
}
