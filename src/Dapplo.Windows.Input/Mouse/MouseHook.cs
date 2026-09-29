// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Input.Mouse;

/// <summary>
///     A global (low-level) mouse hook, using System.Reactive
/// </summary>
/// <remarks>
/// The hook is installed when the first subscriber subscribes and removed when the last one unsubscribes.
/// It runs on a dedicated background thread with its own message loop, it doesn't matter which thread subscribes.
/// <para>
/// Subscribers of <see cref="MouseEvents"/> are called synchronously inside the hook callback, on the hook thread, and every mouse event of the whole system (including every move) waits for them.
/// Windows silently removes a low-level hook which takes longer than the LowLevelHooksTimeout (at most 1 second, often less), after that no events arrive anymore.
/// So only decide <see cref="MouseHookEventArgs.Handled"/> in there and keep it quick, move any other work away from the hook thread with ObserveOn,
/// or use <see cref="MouseEventsNonBlocking"/>. Anything which touches the UI needs to be marshalled to the UI thread.
/// </para>
/// <para>
/// Exceptions thrown by subscribers never leave the hook callback, they are published on <see cref="SubscriberErrors"/>.
/// When the hook cannot be installed, the subscriber gets OnError with a <see cref="System.ComponentModel.Win32Exception"/>.
/// </para>
/// </remarks>
public static class MouseHook
{
    private static readonly LowLevelHook<MouseHookEventArgs> Hook = new(HookTypes.WH_MOUSE_LL, "Dapplo.Windows.Input.MouseHook", CreateMouseEventArgs, eventArgs => eventArgs.Handled);

    /// <summary>
    ///     The mouse events, OnNext is called synchronously on the hook thread.
    ///     Setting <see cref="MouseHookEventArgs.Handled"/> to true in OnNext swallows the mouse event, keep the processing short.
    /// </summary>
    public static IObservable<MouseHookEventArgs> MouseEvents => Hook.Events;

    /// <summary>
    ///     The mouse events, delivered in order on a separate background thread so slow subscribers never delay the mouse input of the system.
    ///     Setting <see cref="MouseHookEventArgs.Handled"/> has no effect here, the event was already passed on.
    /// </summary>
    public static IObservable<MouseHookEventArgs> MouseEventsNonBlocking => Hook.NonBlockingEvents;

    /// <summary>
    ///     Exceptions thrown by subscribers of <see cref="MouseEvents"/> or <see cref="MouseEventsNonBlocking"/>, these are also written to System.Diagnostics.Trace.
    /// </summary>
    public static IObservable<Exception> SubscriberErrors => Hook.SubscriberErrors;

    /// <summary>
    ///     Create the MouseEventArgs from the parameters which where in the event
    /// </summary>
    /// <param name="wParam">IntPtr</param>
    /// <param name="lParam">IntPtr</param>
    /// <returns>MouseEventArgs</returns>
    private static MouseHookEventArgs CreateMouseEventArgs(IntPtr wParam, IntPtr lParam)
    {
        var mouseLowLevelHookStruct = Marshal.PtrToStructure<MouseLowLevelHookStruct>(lParam);

        return new MouseHookEventArgs
        {
            WindowsMessage = (WindowsMessages) wParam.ToInt64(),
            Point = mouseLowLevelHookStruct.pt,
            MouseData = mouseLowLevelHookStruct.MouseData,
            Flags = mouseLowLevelHookStruct.Flags,
            TimeStamp = mouseLowLevelHookStruct.TimeStamp
        };
    }
}
