// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapplo.Windows.Input.Keyboard;

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
    public void Configure(IEnumerable<IKeyboardHookEventHandler> keyCombinations)
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