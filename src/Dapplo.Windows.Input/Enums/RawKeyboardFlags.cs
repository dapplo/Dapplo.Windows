// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    /// Enumeration containing flags for raw keyboard input.
    /// </summary>
    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        /// <summary>
        /// The key is down.
        /// </summary>
        Make = 0,
        /// <summary>
        /// The key is up.
        /// </summary>
        Break = 1,
        /// <summary>
        /// The scan code has the E0 prefix.
        /// </summary>
        E0 = 2,
        /// <summary>
        /// The scan code has the E1 prefix.
        /// </summary>
        E1 = 4,
        /// <summary>
        /// No clue
        /// </summary>
        // ReSharper disable once InconsistentNaming
        TerminalServerSetLED = 8,
        /// <summary>
        /// No clue
        /// </summary>
        TerminalServerShadow = 0x10,
        /// <summary>
        /// No clue
        /// </summary>
        TerminalServerVkPacket = 0x20
    }
}
