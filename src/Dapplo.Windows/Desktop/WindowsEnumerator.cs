// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     A managed EnumWindows / EnumChildWindows wrapper, offering both as IObservable as an IEnumerable.
///     Without a parent EnumWindows enumerates the top-level windows, in Z-order from top to bottom.
///     With a parent EnumChildWindows enumerates all the descendants of the parent (children, their children etc.), depth-first in Z-order:
///     a child is followed by its own descendants before its next sibling. Use <see cref="InteropWindowQuery.GetTopWindows"/> for the direct children only.
///     Windows builds the list of handles when the enumeration starts, so the enumeration is a consistent snapshot: it can't loop or skip windows when the Z-order changes,
///     windows created during the enumeration are not included and windows destroyed during the enumeration may still be reported.
/// </summary>
public static class WindowsEnumerator
{
    /// <summary>
    ///     Call EnumWindows (hWndParent is IntPtr.Zero) or EnumChildWindows (with the parent) and pass every handle to the callback.
    ///     The delegate is kept alive during the native call, an exception of the callback doesn't unwind through the native code:
    ///     it stops the enumeration and is rethrown, with its original stack trace, after the native call returned.
    /// </summary>
    /// <param name="hWndParent">IntPtr with the parent, IntPtr.Zero for the top-level windows</param>
    /// <param name="callback">Func which is called for every handle, return false to stop the enumeration</param>
    internal static void EnumerateHandles(IntPtr hWndParent, Func<IntPtr, bool> callback)
    {
        ExceptionDispatchInfo callbackException = null;

        bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
        {
            // Exceptions must not propagate into the native callback, they are rethrown after the enumeration
            try
            {
                return callback(hWnd);
            }
            catch (Exception ex)
            {
                callbackException = ExceptionDispatchInfo.Capture(ex);
                return false;
            }
        }

        User32Api.EnumWindowsProc enumWindowsProc = EnumWindowsProc;
        // The return value is not used: EnumChildWindows returns false for a window without children, and both return false when the callback stopped the enumeration
        if (hWndParent == IntPtr.Zero)
        {
            User32Api.EnumWindows(enumWindowsProc, IntPtr.Zero);
        }
        else
        {
            User32Api.EnumChildWindows(hWndParent, enumWindowsProc, IntPtr.Zero);
        }
        // The native code calls the delegate until the call returns, make sure the garbage collector doesn't collect it before
        GC.KeepAlive(enumWindowsProc);
        callbackException?.Throw();
    }

    /// <summary>
    ///     Enumerate the windows / child windows, this is NOT lazy: the result is completely retrieved before it's returned.
    /// </summary>
    /// <param name="parent">IInteropWindow with the hWnd of the parent to get all its descendants, or null for the top-level windows</param>
    /// <param name="wherePredicate">Func for the where</param>
    /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
    /// <returns>IReadOnlyList with IntPtr</returns>
    public static IReadOnlyList<IntPtr> EnumerateWindowHandles(IInteropWindow parent = null, Func<IntPtr, bool> wherePredicate = null, Func<IntPtr, int, bool> takeWhileFunc = null)
    {
        var result = new List<IntPtr>();
        EnumerateHandles(parent?.Handle ?? IntPtr.Zero, hWnd =>
        {
            if (wherePredicate == null || wherePredicate(hWnd))
            {
                result.Add(hWnd);
            }

            // check if we should continue
            return takeWhileFunc == null || takeWhileFunc(hWnd, result.Count);
        });
        return result;
    }

    /// <summary>
    ///     Enumerate the windows / child windows, this is NOT lazy: the result is completely retrieved before it's returned.
    /// </summary>
    /// <param name="parent">IInteropWindow with the hWnd of the parent to get all its descendants, or null for the top-level windows</param>
    /// <param name="wherePredicate">Func for the where</param>
    /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
    /// <returns>IReadOnlyList with IInteropWindow</returns>
    public static IReadOnlyList<IInteropWindow> EnumerateWindows(IInteropWindow parent = null, Func<IInteropWindow, bool> wherePredicate = null, Func<IInteropWindow, int, bool> takeWhileFunc = null)
    {
        var result = new List<IInteropWindow>();
        EnumerateHandles(parent?.Handle ?? IntPtr.Zero, hWnd =>
        {
            var interopWindow = InteropWindowFactory.CreateFor(hWnd);

            if (wherePredicate == null || wherePredicate(interopWindow))
            {
                result.Add(interopWindow);
            }

            // check if we should continue
            return takeWhileFunc == null || takeWhileFunc(interopWindow, result.Count);
        });
        return result;
    }

    /// <summary>
    ///     Enumerate the windows / child windows via an Observable.
    ///     The enumeration runs on a thread-pool thread, OnNext is called from the native enumeration callback.
    ///     If an observer throws, the enumeration stops and the exception is passed to OnError. OnCompleted is not called when the subscription was disposed.
    /// </summary>
    /// <param name="hWndParent">IntPtr with the hWnd of the parent to get all its descendants, or null for the top-level windows</param>
    /// <returns>IObservable with IInteropWindow</returns>
    public static IObservable<IInteropWindow> EnumerateWindowsAsync(IntPtr? hWndParent = null)
    {
        return EnumerateAsync<IInteropWindow>(hWndParent, InteropWindowFactory.CreateFor);
    }

    /// <summary>
    ///     Enumerate the windows and child handles (IntPtr) via an Observable.
    ///     The enumeration runs on a thread-pool thread, OnNext is called from the native enumeration callback.
    ///     If an observer throws, the enumeration stops and the exception is passed to OnError. OnCompleted is not called when the subscription was disposed.
    /// </summary>
    /// <param name="hWndParent">IntPtr with the hWnd of the parent to get all its descendants, or null for the top-level windows</param>
    /// <returns>IObservable with IntPtr</returns>
    public static IObservable<IntPtr> EnumerateWindowHandlesAsync(IntPtr? hWndParent = null)
    {
        return EnumerateAsync<IntPtr>(hWndParent, hWnd => hWnd);
    }

    /// <summary>
    ///     The implementation of the observable enumeration
    /// </summary>
    /// <typeparam name="T">Type of the elements</typeparam>
    /// <param name="hWndParent">IntPtr with the hWnd of the parent to get all its descendants, or null for the top-level windows</param>
    /// <param name="factory">Func to create the element for a handle</param>
    /// <returns>IObservable of T</returns>
    private static IObservable<T> EnumerateAsync<T>(IntPtr? hWndParent, Func<IntPtr, T> factory)
    {
        return Observable.Create<T>(observer =>
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Exception callbackException = null;
                try
                {
                    // EnumerateHandles stops the enumeration when OnNext throws, and rethrows the exception after the native call
                    EnumerateHandles(hWndParent ?? IntPtr.Zero, hWnd =>
                    {
                        // check if we should continue
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            return false;
                        }
                        observer.OnNext(factory(hWnd));
                        return !cancellationTokenSource.IsCancellationRequested;
                    });
                }
                catch (Exception ex)
                {
                    callbackException = ex;
                }

                if (callbackException != null)
                {
                    try
                    {
                        observer.OnError(callbackException);
                    }
                    catch (Exception ex)
                    {
                        // A throwing subscriber might already be detached by Rx, so OnError might not reach anyone, trace it
                        Trace.TraceError("WindowsEnumerator: the enumeration failed: {0}", ex);
                    }
                    return;
                }

                // A disposed subscription doesn't expect any more notifications
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    observer.OnCompleted();
                }
            }, cancellationTokenSource.Token);
            return new CancellationDisposable(cancellationTokenSource);
        });
    }
}
