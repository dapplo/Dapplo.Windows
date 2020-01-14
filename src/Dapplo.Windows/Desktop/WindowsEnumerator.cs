// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     A managed EnumWindows wrapper, offering both as IObservable as an IEnumerable
    /// </summary>
    public static class WindowsEnumerator
    {
        /// <summary>
        ///     Enumerate the windows / child windows (this is NOT lazy, unless you add functions)
        /// </summary>
        /// <param name="parent">IInteropWindow with the hWnd of the parent, or null for all</param>
        /// <param name="wherePredicate">Func for the where</param>
        /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
        /// <returns>IEnumerable with IntPtr</returns>
        public static IEnumerable<IntPtr> EnumerateWindowHandles(IInteropWindow parent = null, Func<IntPtr, bool> wherePredicate = null, Func<IntPtr, int, bool> takeWhileFunc = null)
        {
            var result = new List<IntPtr>();

            bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
            {
                // check if we should continue
                var interopWindow = InteropWindowFactory.CreateFor(hWnd);

                if (wherePredicate == null || wherePredicate(hWnd))
                {
                    result.Add(hWnd);
                }

                return takeWhileFunc == null || takeWhileFunc(interopWindow, result.Count);
            }

            User32Api.EnumChildWindows(parent?.Handle ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
            return result;
        }

        /// <summary>
        ///     Enumerate the windows / child windows (this is NOT lazy)
        /// </summary>
        /// <param name="parent">IInteropWindow with the hWnd of the parent, or null for all</param>
        /// <param name="wherePredicate">Func for the where</param>
        /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
        /// <returns>IEnumerable with InteropWindow</returns>
        public static IEnumerable<IInteropWindow> EnumerateWindows(IInteropWindow parent = null, Func<IInteropWindow, bool> wherePredicate = null, Func<IInteropWindow, int, bool> takeWhileFunc = null)
        {
            var result = new List<IInteropWindow>();

            bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
            {
                // check if we should continue
                var interopWindow = InteropWindowFactory.CreateFor(hWnd);

                if (wherePredicate == null || wherePredicate(interopWindow))
                {
                    result.Add(interopWindow);
                }

                return takeWhileFunc == null || takeWhileFunc(interopWindow, result.Count);
            }

            User32Api.EnumChildWindows(parent?.Handle ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
            return result;
        }

        /// <summary>
        ///     Enumerate the windows / child windows via an Observable
        /// </summary>
        /// <param name="hWndParent">IntPtr with the hWnd of the parent, or null for all</param>
        /// <returns>IObservable with IInteropWindow</returns>
        public static IObservable<IInteropWindow> EnumerateWindowsAsync(IntPtr? hWndParent = null)
        {
            return Observable.Create<IInteropWindow>(observer =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() =>
                {
                    bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
                    {
                        // check if we should continue
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            return false;
                        }

                        var interopWindow = InteropWindowFactory.CreateFor(hWnd);
                        observer.OnNext(interopWindow);
                        return !cancellationTokenSource.IsCancellationRequested;
                    }

                    User32Api.EnumChildWindows(hWndParent ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
                    observer.OnCompleted();
                }, cancellationTokenSource.Token);
                return new CancellationDisposable(cancellationTokenSource);
            });
        }

        /// <summary>
        ///     Enumerate the windows and child handles (IntPtr) via an Observable
        /// </summary>
        /// <param name="hWndParent">IntPtr with the hWnd of the parent, or null for all</param>
        /// <returns>IObservable with IntPtr</returns>
        public static IObservable<IntPtr> EnumerateWindowHandlesAsync(IntPtr? hWndParent = null)
        {
            return Observable.Create<IntPtr>(observer =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() =>
                {
                    bool EnumWindowsProc(IntPtr hWnd, IntPtr param)
                    {
                        // check if we should continue
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            return false;
                        }

                        observer.OnNext(hWnd);
                        return !cancellationTokenSource.IsCancellationRequested;
                    }

                    User32Api.EnumChildWindows(hWndParent ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
                    observer.OnCompleted();
                }, cancellationTokenSource.Token);
                return new CancellationDisposable(cancellationTokenSource);
            });
        }
    }
}