// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     The event-injected flags. An application can use the following values to test the flags.
///     Testing LLMHF_INJECTED (bit 0) will tell you whether the event was injected.
///     If it was, then testing LLMHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not
///     the event was injected from a process running at lower integrity level.
/// </summary>
[Flags]
public enum ExtendedMouseFlags : uint
{
    /// <summary>
    ///     Test the event-injected (from any process) flag.
    /// </summary>
    Injected = 0x01,

    /// <summary>
    ///     Test the event-injected (from a process running at lower integrity level) flag.
    /// </summary>
    LowerIntegretyInjected = 0x02
}