// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using System;
using System.Reactive.Linq;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.SystemState.Enums;

namespace Dapplo.Windows.SystemState;

/// <summary>
/// Provides an observable stream of power broadcast events from the Windows message queue.
/// Listens for <see cref="WindowsMessages.WM_POWERBROADCAST"/> messages and exposes them as
/// an <see cref="IObservable{T}"/> of <see cref="PowerBroadcastEvent"/> values.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast">WM_POWERBROADCAST message</a>
/// and <a href="https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events">System Wake-Up Events</a>
/// </summary>
public static class PowerBroadcastListener
{
    private static readonly IObservable<PowerBroadcastEvent> _powerBroadcastEvents;

    static PowerBroadcastListener()
    {
        _powerBroadcastEvents = SharedMessageWindow.Messages
            .Where(m => m.Msg == WindowsMessages.WM_POWERBROADCAST)
            .Select(m => (PowerBroadcastEvent)(uint)m.WParam)
            .Publish()
            .RefCount();
    }

    /// <summary>
    /// Gets an observable sequence of power broadcast events.
    /// Subscribe to this to be notified of system power state changes such as
    /// suspend, resume, battery status changes, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// PowerBroadcastListener.PowerEvents
    ///     .Where(e => e == PowerBroadcastEvent.PBT_APMRESUMEAUTOMATIC)
    ///     .Subscribe(e => Console.WriteLine("System resumed automatically"));
    /// </code>
    /// </example>
    public static IObservable<PowerBroadcastEvent> PowerEvents => _powerBroadcastEvents;

    /// <summary>
    /// Gets an observable sequence that emits when the system is about to suspend.
    /// </summary>
    public static IObservable<PowerBroadcastEvent> Suspending =>
        _powerBroadcastEvents.Where(e => e == PowerBroadcastEvent.PBT_APMSUSPEND);

    /// <summary>
    /// Gets an observable sequence that emits when the system has resumed from suspension.
    /// This fires when user activity or an application is detected on the resumed system.
    /// </summary>
    public static IObservable<PowerBroadcastEvent> ResumedFromSuspend =>
        _powerBroadcastEvents.Where(e => e == PowerBroadcastEvent.PBT_APMRESUMESUSPEND);

    /// <summary>
    /// Gets an observable sequence that emits when the system has resumed automatically to handle an event.
    /// Applications that receive this event are not allowed to interact with the user.
    /// If the user is present, <see cref="ResumedFromSuspend"/> will follow.
    /// </summary>
    public static IObservable<PowerBroadcastEvent> ResumedAutomatically =>
        _powerBroadcastEvents.Where(e => e == PowerBroadcastEvent.PBT_APMRESUMEAUTOMATIC);

    /// <summary>
    /// Gets an observable sequence that emits when the system power status has changed
    /// (e.g., switch from battery to AC power, or battery level changed significantly).
    /// </summary>
    public static IObservable<PowerBroadcastEvent> PowerStatusChanged =>
        _powerBroadcastEvents.Where(e => e == PowerBroadcastEvent.PBT_APMPOWERSTATUSCHANGE);
}
#endif
