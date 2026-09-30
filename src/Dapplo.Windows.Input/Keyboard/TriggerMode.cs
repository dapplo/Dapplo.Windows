// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
/// Specifies when a <see cref="KeyCombinationHandler"/> triggers.
/// </summary>
public enum TriggerMode
{
    /// <summary>
    /// Trigger on the key-down which completes the combination: all keys of the combination are down and no other key.
    /// The key events of the combination are marked as handled (swallowed), unless <see cref="KeyCombinationHandler.IsPassThrough"/> is set.
    /// </summary>
    KeyDown,

    /// <summary>
    /// Trigger on the first key-up of a key of the combination, if all keys of the combination were down together and no other key is down.
    /// The other keys of the combination can still be down at that moment.
    /// The key events are never marked as handled.
    /// </summary>
    FirstKeyUp,

    /// <summary>
    /// Trigger once, when the last key of a complete combination is released: all keys of the combination were down together, without another key,
    /// and all of them are up again. When the trigger arrives no key of the combination is down anymore, so input can be sent right away.
    /// A key which is not part of the combination, pressed while a key of the combination is down, cancels the trigger;
    /// pressing the complete combination again (with a new key-down, auto-repeat doesn't count) arms it again.
    /// The key events are never marked as handled.
    /// </summary>
    AllKeysUp
}
