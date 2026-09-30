// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     The WinEventHook can register handlers to become important windows events
///     This makes it possible to know a.o. when a window is created, moved, updated and closed.
/// </summary>
/// <remarks>
///     The hooks are installed and removed on the thread of the <see cref="SharedMessageWindow"/>, which runs a message loop.
///     This is needed as out-of-context events are delivered to the thread which installed the hook, and UnhookWinEvent must be called on that thread.
///     The events are therefore produced on the thread of the SharedMessageWindow: don't block in OnNext, use ObserveOn to process them elsewhere.
///     An exception thrown by a subscriber ends its subscription (and removes the hook when it was the last subscriber).
///     Every call of <see cref="Create"/> installs its own hook when subscribed, subscribers of the same returned observable share one hook.
/// </remarks>
public static class WinEventHook
{
    // Keeps a reference to the delegates, as long as the hook is installed, otherwise they are GC'ed while Windows still calls them
    private static readonly ConcurrentDictionary<IntPtr, WinEventDelegate> Delegates = new ConcurrentDictionary<IntPtr, WinEventDelegate>();

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
                void WinEventHookDelegate(IntPtr eventHook, WinEvents winEvent, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, int eventThread, uint eventTime)
                {
                    // Exceptions must never propagate into the native callback
                    try
                    {
                        observer.OnNext(WinEventInfo.Create(eventHook, winEvent, hWnd, idObject, idChild, eventThread, eventTime));
                    }
                    catch (Exception ex)
                    {
                        // A throwing subscriber is detached by Rx (which also removes the hook), OnError might not reach anyone, so trace it
                        System.Diagnostics.Trace.TraceError("WinEventHook: a subscriber threw an exception, the subscription ends: {0}", ex);
                        try
                        {
                            observer.OnError(ex);
                        }
                        catch
                        {
                            // Nothing more we can do, the exception must not reach Windows
                        }
                    }
                }

                WinEventDelegate winEventDelegate = WinEventHookDelegate;
                var hookPtr = IntPtr.Zero;
                var error = 0;
                // Install the hook on the thread of the SharedMessageWindow, which pumps messages
                SharedMessageWindow.Invoke(_ =>
                {
                    hookPtr = SetWinEventHook(winEventStart, winEventEnd ?? winEventStart, IntPtr.Zero, winEventDelegate, process, thread, WinEventHookFlags.OutOfContext);
                    if (hookPtr == IntPtr.Zero)
                    {
                        error = Marshal.GetLastWin32Error();
                        return;
                    }
                    // Store to keep a reference to it, otherwise it's GC'ed
                    Delegates[hookPtr] = winEventDelegate;
                });
                if (hookPtr == IntPtr.Zero)
                {
                    observer.OnError(new Win32Exception(error, "SetWinEventHook failed"));
                    return Disposable.Empty;
                }

                return Disposable.Create(() =>
                {
                    // UnhookWinEvent must be called on the thread which called SetWinEventHook
                    try
                    {
                        SharedMessageWindow.Invoke(_ =>
                        {
                            // If unhooking fails, the delegate is kept alive, as Windows might still call it
                            if (UnhookWinEvent(hookPtr))
                            {
                                Delegates.TryRemove(hookPtr, out var _);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // Dispose must not throw
                        System.Diagnostics.Trace.TraceError("WinEventHook: removing the hook failed: {0}", ex);
                    }
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
    [return: MarshalAs(UnmanagedType.Bool)]
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
    [DllImport(User32Api.User32, SetLastError = true)]
    private static extern IntPtr SetWinEventHook(WinEvents eventMin, WinEvents eventMax, IntPtr hmodWinEventProc, WinEventDelegate eventProc, int idProcess, int idThread, WinEventHookFlags winEventHookFlags);

    /// <summary>
    ///     The delegate called by SetWinEventHook when an event occurs
    /// </summary>
    /// <param name="hWinEventHook">IntPtr with the eventhook that this call belongs to</param>
    /// <param name="eventType">WinEvent</param>
    /// <param name="hWnd">IntPtr</param>
    /// <param name="idObject">ObjectIdentifiers</param>
    /// <param name="idChild">int</param>
    /// <param name="eventThread">int with the thread ID (a DWORD, natively)</param>
    /// <param name="eventTime">uint with EventTime</param>
    private delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvents eventType, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, int eventThread, uint eventTime);
}