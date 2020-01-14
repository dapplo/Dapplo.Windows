// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    /// This can handle KeyboardHookEventArgs, the handle method returns true if the key was handled.
    /// </summary>
    public interface IKeyboardHookEventHandler
    {
        /// <summary>
        /// Handle a KeyboardHookEventArgs
        /// </summary>
        /// <param name="keyboardHookEventArgs"></param>
        /// <returns>bool true if handled</returns>
        bool Handle(KeyboardHookEventArgs keyboardHookEventArgs);

        /// <summary>
        /// Test if this event handler currently has keys pressed
        /// </summary>
        bool HasKeysPressed { get; }
    }
}
