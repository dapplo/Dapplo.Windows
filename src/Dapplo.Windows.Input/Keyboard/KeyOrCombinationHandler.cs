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

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// This is an IKeyboardHookEventHandler which checks multiple IKeyboardHookEventHandler.
    /// Handle returns true if one can handle the key press.
    /// </summary>
    public class KeyOrCombinationHandler : IKeyboardHookEventHandler
    {
        private readonly IKeyboardHookEventHandler[] _keyCombinations;

        /// <summary>
        /// Create a KeyOrCombinationHandler for the specified Key Combinations
        /// </summary>
        /// <param name="keyCombinations">IEnumerable with KeyCombinationHandler</param>
        public KeyOrCombinationHandler(IEnumerable<IKeyboardHookEventHandler> keyCombinations)
        {
            _keyCombinations = keyCombinations.ToArray();
        }

        /// <summary>
        /// Create a KeyOrCombinationHandler for the specified Key Combinations
        /// </summary>
        /// <param name="keyCombinations">params with KeyCombinationHandler</param>
        public KeyOrCombinationHandler(params IKeyboardHookEventHandler[] keyCombinations)
        {
            _keyCombinations = keyCombinations.ToArray();
        }

        /// <summary>
        /// Check if the combinations are pressed
        /// </summary>
        /// <param name="keyboardHookEventArgs">KeyboardHookEventArgs</param>
        public bool Handle(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            bool handled = false;
            foreach (var keyboardHookEventHandler in _keyCombinations)
            {
                handled |= keyboardHookEventHandler.Handle(keyboardHookEventArgs);
            }
            return handled;

        }

        /// <inheritdoc />
        public bool HasKeysPressed => _keyCombinations.Any(handler => handler.HasKeysPressed);
    }
}
