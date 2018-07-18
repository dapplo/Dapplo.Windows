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
using System.Reactive.Linq;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// Extensions to assist with the Keyboard Hooks
    /// </summary>
    public static class KeyboardHookExtensions
    {
        /// <summary>
        /// Filter the KeyboardHookEventArgs with a KeyCombinationHandler
        /// </summary>
        /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
        /// <param name="keyCombination">IEnumerable with VirtualKeyCodes</param>
        /// <returns>IObservable with the KeyCombinationHandler</returns>
        public static IObservable<KeyCombinationHandler> WhereKeyCombination(this IObservable<KeyboardHookEventArgs> keyboardObservable, params VirtualKeyCodes[] keyCombination)
        {
            var keyCombinationHandler = new KeyCombinationHandler(keyCombination);
            return keyboardObservable.WhereKeyCombination(keyCombinationHandler);
        }

        /// <summary>
        /// Filter the KeyboardHookEventArgs with a KeyCombinationHandler, if this combination is triggered it's passed in the resulting IObservable
        /// </summary>
        /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
        /// <param name="keyCombination">IEnumerable with VirtualKeyCodes</param>
        /// <returns>IObservable with the KeyCombinationHandler</returns>
        public static IObservable<KeyCombinationHandler> WhereKeyCombination(this IObservable<KeyboardHookEventArgs> keyboardObservable, IEnumerable<VirtualKeyCodes> keyCombination)
        {
            var keyCombinationHandler = new KeyCombinationHandler(keyCombination);
            return keyboardObservable.WhereKeyCombination(keyCombinationHandler);
        }

        /// <summary>
        /// Filter the KeyboardHookEventArgs with a KeyCombinationHandler, if this combination is triggered it's passed in the resulting IObservable
        /// </summary>
        /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
        /// <param name="keyCombinationHandler">KeyCombinationHandler defines which key combination you want to work with</param>
        /// <returns>IObservable with the KeyCombinationHandler</returns>
        public static IObservable<KeyCombinationHandler> WhereKeyCombination(this IObservable<KeyboardHookEventArgs> keyboardObservable, KeyCombinationHandler keyCombinationHandler)
        {
            return keyboardObservable.Where(keyCombinationHandler.Handle).Select(e => keyCombinationHandler);
        }
    }
}
