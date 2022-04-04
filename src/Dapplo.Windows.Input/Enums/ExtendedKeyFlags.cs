// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     The extended-key flag, event-injected flags, context code, and transition-state flag.
///     This member is specified as follows.
///     An application can use the following values to test the keystroke flags.
///     Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected.
///     If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a
///     process running at lower integrity level.
/// </summary>
[Flags]
public enum ExtendedKeyFlags : uint
{
    /// <summary>
    /// No flags
    /// </summary>
    None = 0,

    /// <summary>
    ///     Test the extended-key flag.
    /// </summary>
    Extended = 0x01,

    /// <summary>
    ///     Test the event-injected (from a process running at lower integrity level) flag.
    /// </summary>
    LowerIntegretyInjected = 0x02,

    /// <summary>
    ///     Test the event-injected (from any process) flag.
    /// </summary>
    Injected = 0x10,

    /// <summary>
    ///     Test the context code.
    /// </summary>
    AltDown = 0x20,

    /// <summary>
    ///     Test the transition-state flag.
    /// </summary>
    Up = 0x80
}