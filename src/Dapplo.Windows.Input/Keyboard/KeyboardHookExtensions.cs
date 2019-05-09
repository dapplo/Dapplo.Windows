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
using System.Reactive.Linq;

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// Extensions to assist with the Keyboard Hooks
    /// </summary>
    public static class KeyboardHookExtensions
    {
        /// <summary>
        /// Filter the KeyboardHookEventArgs with a IKeyboardHookEventHandler
        /// </summary>
        /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
        /// <param name="keyboardHookEventHandler">IKeyboardHookEventHandler</param>
        /// <returns>IObservable with the KeyboardHookEventArgs which was handled</returns>
        public static IObservable<KeyboardHookEventArgs> Where(this IObservable<KeyboardHookEventArgs> keyboardObservable, IKeyboardHookEventHandler keyboardHookEventHandler)
        {
            return keyboardObservable.Where(keyboardHookEventHandler.Handle);
        }
    }
}
