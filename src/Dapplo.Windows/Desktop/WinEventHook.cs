﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     The WinEventHook can register handlers to become important windows events
///     This makes it possible to know a.o. when a window is created, moved, updated and closed.
///     Make sure you have a message pump running (WinProc), otherwise the behavior is sporadic / none.
/// </summary>
public static class WinEventHook
{
    private static readonly Dictionary<IntPtr, WinEventDelegate> Delegates = new Dictionary<IntPtr, WinEventDelegate>();

    /// <summary>
    ///     Create a WinEventHook as observable
    /// </summary>
    /// <param name="winEventStart">WinEvent "start" of which you are interested</param>
    /// <param name="winEventEnd">WinEvent "end" of which you are interested</param>
    /// <param name="process"></param>
    /// <param name="thread"></param>
    /// <returns>IObservable which processes WinEventInfo</returns>
    public static IObservable<WinEventInfo> Create(WinEvents winEventStart, WinEvents? winEventEnd = null, int process = 0, int thread = 0)
    {
        return Observable.Create<WinEventInfo>(observer =>
            {
                void WinEventHookDelegate(IntPtr eventHook, WinEvents winEvent, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, uint eventThread, uint eventTime)
                {
                    observer.OnNext(WinEventInfo.Create(eventHook, winEvent, hWnd, idObject, idChild, eventThread, eventTime));
                }

                WinEventDelegate winEventDelegate = WinEventHookDelegate;
                var hookPtr = SetWinEventHook(winEventStart, winEventEnd ?? winEventStart, IntPtr.Zero, winEventDelegate, process, thread, WinEventHookFlags.OutOfContext);
                if (hookPtr == IntPtr.Zero)
                {
                    observer.OnError(new Win32Exception("Can't hook."));
                    return Disposable.Empty;
                }
                // Store to keep a reference to it, otherwise it's GC'ed
                Delegates[hookPtr] = winEventDelegate;

                return Disposable.Create(() =>
                {
                    UnhookWinEvent(hookPtr);
                    Delegates.Remove(hookPtr);
                });
            })
            .Publish()
            .RefCount();
    }

    /// <summary>
    ///     Create an observable which only monitors title changes
    /// </summary>
    /// <returns>IObservable with WinEventInfo</returns>
    public static IObservable<WinEventInfo> WindowTitleChangeObservable()
    {
        return Create(WinEvents.EVENT_OBJECT_NAMECHANGE).Where(winEventInfo => winEventInfo.ObjectIdentifier == ObjectIdentifiers.Window);
    }

    /// <summary>
    ///     Create an observable which only monitors created (open) and destroyed (closed) windows
    /// </summary>
    /// <returns>IObservable with WinEventInfo</returns>
    public static IObservable<WinEventInfo> WindowCreateDestroyObservable()
    {
        return Create(WinEvents.EVENT_OBJECT_CREATE, WinEvents.EVENT_OBJECT_DESTROY).Where(winEventInfo => winEventInfo.ObjectIdentifier == ObjectIdentifiers.Window);
    }

    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwinevent">UnhookWinEvent</a>
    ///
    /// Three common errors cause this function to fail:  
    /// The hWinEventHook parameter is NULL or not valid.
    /// The event hook specified by hWinEventHook was already removed.
    /// UnhookWinEvent is called from a thread that is different from the original call to SetWinEventHook.
    /// </summary>
    /// <param name="hWinEventHook">IntPtr with the win event hook handle from SetWinEventHook</param>
    /// <returns>bool</returns>
    [DllImport(User32Api.User32, SetLastError = true)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    /// <summary>
    ///     Hook to win events, see <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook">SetWinEventHook</a>
    /// </summary>
    /// <param name="eventMin">
    ///     Specifies the event constant for the lowest event value in the range of events that are handled
    ///     by the hook function. This parameter can be set to EVENT_MIN to indicate the lowest possible event value.
    /// </param>
    /// <param name="eventMax">
    ///     Specifies the event constant for the highest event value in the range of events that are handled
    ///     by the hook function. This parameter can be set to EVENT_MAX to indicate the highest possible event value.
    /// </param>
    /// <param name="hmodWinEventProc">
    ///     Handle to the DLL that contains the hook function at lpfnWinEventProc, if the
    ///     WINEVENT_INCONTEXT flag is specified in the dwFlags parameter. If the hook function is not located in a DLL, or if
    ///     the WINEVENT_OUTOFCONTEXT flag is specified, this parameter is NULL.
    /// </param>
    /// <param name="eventProc">WinEventDelegate</param>
    /// <param name="idProcess">
    ///     Specifies the ID of the process from which the hook function receives events. Specify zero (0)
    ///     to receive events from all processes on the current desktop.
    /// </param>
    /// <param name="idThread">
    ///     Specifies the ID of the thread from which the hook function receives events. If this parameter
    ///     is zero, the hook function is associated with all existing threads on the current desktop.
    /// </param>
    /// <param name="winEventHookFlags">WinEventHookFlags</param>
    /// <returns>IntPtr with the hook id</returns>
    [DllImport(User32Api.User32)]
    private static extern IntPtr SetWinEventHook(WinEvents eventMin, WinEvents eventMax, IntPtr hmodWinEventProc, WinEventDelegate eventProc, int idProcess, int idThread, WinEventHookFlags winEventHookFlags);

    /// <summary>
    ///     The delegate called by SetWinEventHook when an event occurs
    /// </summary>
    /// <param name="hWinEventHook">IntPtr with the eventhook that this call belongs to</param>
    /// <param name="eventType">WinEvent</param>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="idObject">ObjectIdentifiers</param>
    /// <param name="idChild">int</param>
    /// <param name="eventThread">uint with EventThread</param>
    /// <param name="eventTime">uint with EventTime</param>
    private delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvents eventType, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, uint eventThread, uint eventTime);
}