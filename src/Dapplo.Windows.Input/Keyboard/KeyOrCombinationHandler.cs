// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;

namespace Dapplo.Windows.Input.Keyboard;

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