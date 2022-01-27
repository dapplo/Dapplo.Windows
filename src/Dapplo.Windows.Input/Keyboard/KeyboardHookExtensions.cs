// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reactive.Linq;

namespace Dapplo.Windows.Input.Keyboard;

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