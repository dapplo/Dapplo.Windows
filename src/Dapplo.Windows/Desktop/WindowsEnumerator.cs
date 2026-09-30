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
///     A managed EnumWindows wrapper, offering both as IObservable as an IEnumerable.
///     Note: with a parent, EnumChildWindows enumerates all the descendants of the parent (children, their children etc.), without a parent the top-level windows.
/// </summary>
public static class WindowsEnumerator
{
    /// <summary>
    ///     Enumerate the windows / child windows, this is NOT lazy: the result is completely retrieved before it's returned.
    /// </summary>
    /// <param name="parent">IInteropWindow with the hWnd of the parent, or null for all</param>
    /// <param name="wherePredicate">Func for the where</param>
    /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
    /// <returns>IReadOnlyList with IntPtr</returns>
    public static IReadOnlyList<IntPtr> EnumerateWindowHandles(IInteropWindow parent = null, Func<IntPtr, bool> wherePredicate = null, Func<IntPtr, int, bool> takeWhileFunc = null)
    {
        var result = new List<IntPtr>();
        ExceptionDispatchInfo callbackException = null;

        bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
        {
            // Exceptions must not propagate into the native callback, they are rethrown after the enumeration
            try
            {
                if (wherePredicate == null || wherePredicate(hWnd))
                {
                    result.Add(hWnd);
                }

                // check if we should continue
                return takeWhileFunc == null || takeWhileFunc(hWnd, result.Count);
            }
            catch (Exception ex)
            {
                callbackException = ExceptionDispatchInfo.Capture(ex);
                return false;
            }
        }

        User32Api.EnumChildWindows(parent?.Handle ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
        callbackException?.Throw();
        return result;
    }

    /// <summary>
    ///     Enumerate the windows / child windows, this is NOT lazy: the result is completely retrieved before it's returned.
    /// </summary>
    /// <param name="parent">IInteropWindow with the hWnd of the parent, or null for all</param>
    /// <param name="wherePredicate">Func for the where</param>
    /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
    /// <returns>IReadOnlyList with IInteropWindow</returns>
    public static IReadOnlyList<IInteropWindow> EnumerateWindows(IInteropWindow parent = null, Func<IInteropWindow, bool> wherePredicate = null, Func<IInteropWindow, int, bool> takeWhileFunc = null)
    {
        var result = new List<IInteropWindow>();
        ExceptionDispatchInfo callbackException = null;

        bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
        {
            // Exceptions must not propagate into the native callback, they are rethrown after the enumeration
            try
            {
                var interopWindow = InteropWindowFactory.CreateFor(hWnd);

                if (wherePredicate == null || wherePredicate(interopWindow))
                {
                    result.Add(interopWindow);
                }

                // check if we should continue
                return takeWhileFunc == null || takeWhileFunc(interopWindow, result.Count);
            }
            catch (Exception ex)
            {
                callbackException = ExceptionDispatchInfo.Capture(ex);
                return false;
            }
        }

        User32Api.EnumChildWindows(parent?.Handle ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
        callbackException?.Throw();
        return result;
    }

    /// <summary>
    ///     Enumerate the windows / child windows via an Observable.
    ///     The enumeration runs on a thread-pool thread, OnNext is called from the native enumeration callback.
    ///     If an observer throws, the enumeration stops and the exception is passed to OnError. OnCompleted is not called when the subscription was disposed.
    /// </summary>
    /// <param name="hWndParent">IntPtr with the hWnd of the parent, or null for all</param>
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
    /// <param name="hWndParent">IntPtr with the hWnd of the parent, or null for all</param>
    /// <returns>IObservable with IntPtr</returns>
    public static IObservable<IntPtr> EnumerateWindowHandlesAsync(IntPtr? hWndParent = null)
    {
        return EnumerateAsync<IntPtr>(hWndParent, hWnd => hWnd);
    }

    /// <summary>
    ///     The implementation of the observable enumeration
    /// </summary>
    /// <typeparam name="T">Type of the elements</typeparam>
    /// <param name="hWndParent">IntPtr with the hWnd of the parent, or null for all</param>
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

                bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
                {
                    // check if we should continue
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return false;
                    }

                    // Exceptions must never propagate into the native callback
                    try
                    {
                        observer.OnNext(factory(hWnd));
                    }
                    catch (Exception ex)
                    {
                        callbackException = ex;
                        return false;
                    }
                    return !cancellationTokenSource.IsCancellationRequested;
                }

                try
                {
                    User32Api.EnumChildWindows(hWndParent ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    callbackException ??= ex;
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
