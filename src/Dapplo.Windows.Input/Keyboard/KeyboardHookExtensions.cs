// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
/// Extensions to assist with the Keyboard Hooks
/// </summary>
public static class KeyboardHookExtensions
{
    // The handlers which are currently used by a subscription, an IKeyboardHookEventHandler keeps state and may only be used by one subscription at a time
    private static readonly ConditionalWeakTable<IKeyboardHookEventHandler, object> HandlersInUse = new();

    /// <summary>
    /// Filter the KeyboardHookEventArgs with a IKeyboardHookEventHandler.
    /// An IKeyboardHookEventHandler keeps track of the pressed keys, so an instance can only be used by one subscription at a time,
    /// subscribing a second time while the first subscription is active results in an <see cref="InvalidOperationException"/> (OnError).
    /// Use the overload with a factory when the observable is subscribed more than once.
    /// </summary>
    /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
    /// <param name="keyboardHookEventHandler">IKeyboardHookEventHandler</param>
    /// <returns>IObservable with the KeyboardHookEventArgs which was handled</returns>
    public static IObservable<KeyboardHookEventArgs> Where(this IObservable<KeyboardHookEventArgs> keyboardObservable, IKeyboardHookEventHandler keyboardHookEventHandler)
    {
        if (keyboardObservable is null) throw new ArgumentNullException(nameof(keyboardObservable));
        if (keyboardHookEventHandler is null) throw new ArgumentNullException(nameof(keyboardHookEventHandler));

        return Observable.Create<KeyboardHookEventArgs>(observer =>
        {
            lock (HandlersInUse)
            {
                if (HandlersInUse.TryGetValue(keyboardHookEventHandler, out _))
                {
                    observer.OnError(new InvalidOperationException("This IKeyboardHookEventHandler is already used by another subscription, it keeps state and can only be used once. Use a new instance, or the Where overload with a factory."));
                    return Disposable.Empty;
                }
                HandlersInUse.Add(keyboardHookEventHandler, null);
            }
            var subscription = keyboardObservable.Where(keyboardHookEventHandler.Handle).Subscribe(observer);
            return Disposable.Create(() =>
            {
                subscription.Dispose();
                lock (HandlersInUse)
                {
                    HandlersInUse.Remove(keyboardHookEventHandler);
                }
            });
        });
    }

    /// <summary>
    /// Filter the KeyboardHookEventArgs with a IKeyboardHookEventHandler, every subscription gets its own handler from the factory.
    /// Use this when the resulting observable can be subscribed more than once.
    /// </summary>
    /// <param name="keyboardObservable">IObservable of KeyboardHookEventArgs, e.g. coming from KeyboardHook.KeyboardEvents</param>
    /// <param name="keyboardHookEventHandlerFactory">Func which creates a new IKeyboardHookEventHandler for every subscription</param>
    /// <returns>IObservable with the KeyboardHookEventArgs which was handled</returns>
    public static IObservable<KeyboardHookEventArgs> Where(this IObservable<KeyboardHookEventArgs> keyboardObservable, Func<IKeyboardHookEventHandler> keyboardHookEventHandlerFactory)
    {
        if (keyboardObservable is null) throw new ArgumentNullException(nameof(keyboardObservable));
        if (keyboardHookEventHandlerFactory is null) throw new ArgumentNullException(nameof(keyboardHookEventHandlerFactory));

        return Observable.Defer(() =>
        {
            var keyboardHookEventHandler = keyboardHookEventHandlerFactory() ?? throw new InvalidOperationException("The factory returned no IKeyboardHookEventHandler");
            return keyboardObservable.Where(keyboardHookEventHandler.Handle);
        });
    }
}
