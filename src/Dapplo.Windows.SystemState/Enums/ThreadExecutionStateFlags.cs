// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.SystemState.Enums;

/// <summary>
/// Flags for the SetThreadExecutionState function, which enables an application to inform
/// the system that it is in use, thereby preventing the system from entering sleep or turning off the display.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate">SetThreadExecutionState function</a>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[Flags]
public enum ThreadExecutionStateFlags : uint
{
    /// <summary>
    /// Enables away mode. This value must be specified with <see cref="ES_CONTINUOUS"/>.
    /// Away mode should be used only by media-recording and media-distribution applications that must perform critical background processing on desktop computers while the computer appears to be sleeping.
    /// </summary>
    ES_AWAYMODE_REQUIRED = 0x00000040,

    /// <summary>
    /// Informs the system that the state being set should remain in effect until the next call
    /// that includes <see cref="ES_CONTINUOUS"/> and one of the other state flags is cleared.
    /// </summary>
    ES_CONTINUOUS = 0x80000000,

    /// <summary>
    /// Forces the display to be on by resetting the display idle timer.
    /// </summary>
    ES_DISPLAY_REQUIRED = 0x00000002,

    /// <summary>
    /// Forces the system to be in the working state by resetting the system idle timer.
    /// </summary>
    ES_SYSTEM_REQUIRED = 0x00000001,

    /// <summary>
    /// This value is not supported. If <see cref="ES_USER_PRESENT"/> is combined with other esFlags values,
    /// the call will fail and none of the specified states will be set.
    /// </summary>
    [Obsolete("ES_USER_PRESENT is not supported.")]
    ES_USER_PRESENT = 0x00000004,
}
