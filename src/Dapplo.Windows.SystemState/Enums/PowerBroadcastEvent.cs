// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.SystemState.Enums;

/// <summary>
/// Power broadcast events (wParam values for WM_POWERBROADCAST).
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast">WM_POWERBROADCAST message</a>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum PowerBroadcastEvent : uint
{
    /// <summary>
    /// The system is about to suspend.
    /// Applications should save any data they need before the system suspends.
    /// </summary>
    PBT_APMSUSPEND = 0x0004,

    /// <summary>
    /// The system has resumed operation after being suspended.
    /// This event is broadcast when user activity or an application is detected on the resumed system,
    /// or if the user presses the power button.
    /// </summary>
    PBT_APMRESUMESUSPEND = 0x0007,

    /// <summary>
    /// Battery power is low.
    /// </summary>
    PBT_APMBATTERYLOW = 0x0009,

    /// <summary>
    /// A change in the power status of the computer is detected, such as a switch from battery power to AC.
    /// The system also broadcasts this event when remaining battery power slips below the threshold
    /// specified by the user or if the battery power changes by a specified percentage.
    /// </summary>
    PBT_APMPOWERSTATUSCHANGE = 0x000A,

    /// <summary>
    /// The system has resumed operation after a critical suspension caused by a failing battery.
    /// </summary>
    PBT_APMRESUMEDCRITICAL = 0x0006,

    /// <summary>
    /// The system has resumed operation automatically to handle an event.
    /// Applications that receive the PBT_APMRESUMEAUTOMATIC event are not allowed to interact with the user.
    /// If the user is present, another PBT_APMRESUMESUSPEND event will follow.
    /// See <a href="https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events">System Wake-Up Events</a>
    /// </summary>
    PBT_APMRESUMEAUTOMATIC = 0x0012,

    /// <summary>
    /// A power setting change event has been received.
    /// The lParam parameter points to a POWERBROADCAST_SETTING structure.
    /// </summary>
    PBT_POWERSETTINGCHANGE = 0x8013,
}
