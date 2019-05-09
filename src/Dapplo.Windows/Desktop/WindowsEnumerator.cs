//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.User32;

#endregion

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
        /// <param name="parent">IInteropWindow with the hwnd of the parent, or null for all</param>
        /// <param name="wherePredicate">Func for the where</param>
        /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
        /// <returns>IEnumerable with IntPtr</returns>
        public static IEnumerable<IntPtr> EnumerateWindowHandles(IInteropWindow parent = null, Func<IntPtr, bool> wherePredicate = null, Func<IntPtr, int, bool> takeWhileFunc = null)
        {
            var result = new List<IntPtr>();

            bool EnumWindowsProc(IntPtr hwnd, IntPtr param)
            {
                // check if we should continue
                var interopWindow = InteropWindowFactory.CreateFor(hwnd);

                if (wherePredicate == null || wherePredicate(hwnd))
                {
                    result.Add(hwnd);
                }

                return takeWhileFunc == null || takeWhileFunc(interopWindow, result.Count);
            }

            User32Api.EnumChildWindows(parent?.Handle ?? IntPtr.Zero, EnumWindowsProc, IntPtr.Zero);
            return result;
        }

        /// <summary>
        ///     Enumerate the windows / child windows (this is NOT lazy)
        /// </summary>
        /// <param name="parent">IInteropWindow with the hwnd of the parent, or null for all</param>
        /// <param name="wherePredicate">Func for the where</param>
        /// <param name="takeWhileFunc">Func which can decide to stop enumerating, the second argument is the current count</param>
        /// <returns>IEnumerable with InteropWindow</returns>
        public static IEnumerable<IInteropWindow> EnumerateWindows(IInteropWindow parent = null, Func<IInteropWindow, bool> wherePredicate = null, Func<IInteropWindow, int, bool> takeWhileFunc = null)
        {
            var result = new List<IInteropWindow>();

            bool EnumWindowsProc(IntPtr hwnd, IntPtr param)
            {
                // check if we should continue
                var interopWindow = InteropWindowFactory.CreateFor(hwnd);

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