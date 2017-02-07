#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Native;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	///     EnumWindows wrapper for .NET
	/// </summary>
	public static class WindowsEnumerator
	{
		/// <summary>
		///     Enumerate the windows / child windows via an Observable
		/// </summary>
		/// <param name="hWndParent">IntPtr with the hwnd of the parent, or null for all</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>IObservable with WinWindowInfo</returns>
		public static IObservable<WindowInfo> EnumerateWindowsAsync(IntPtr? hWndParent = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Observable.Create<WindowInfo>(observer =>
			{
				var continueWithEnumeration = true;
				Task.Run(() =>
				{
					User32.EnumChildWindows(hWndParent ?? IntPtr.Zero, (hwnd, param) =>
					{
						// check if we should continue
						if (cancellationToken.IsCancellationRequested || !continueWithEnumeration)
						{
							return false;
						}
						var windowInfo = WindowInfo.CreateFor(hwnd);
						observer.OnNext(windowInfo);
						return continueWithEnumeration;
					}, IntPtr.Zero);
					observer.OnCompleted();
				}, cancellationToken);
				return () =>
				{
					// Stop enumerating
					continueWithEnumeration = false;
				};
			});
		}

		/// <summary>
		///     Enumerate the windows / child windows (this is NOT lazy)
		/// </summary>
		/// <param name="hWndParent">IntPtr with the hwnd of the parent, or null for all</param>
		/// <param name="wherePredicate">Func for the where</param>
		/// <param name="takeWhileFunc">Func which can decide to stop enumerating</param>
		/// <returns>IEnumerable with WindowInfo</returns>
		public static IEnumerable<WindowInfo> EnumerateWindows(IntPtr? hWndParent = null, Func<WindowInfo, bool> wherePredicate = null, Func<WindowInfo,bool> takeWhileFunc = null)
		{
			var result = new List<WindowInfo>();
			User32.EnumChildWindows(hWndParent ?? IntPtr.Zero, (hwnd, param) =>
			{
				// check if we should continue
				var windowInfo = WindowInfo.CreateFor(hwnd);

				if (wherePredicate?.Invoke(windowInfo) != false)
				{
					result.Add(windowInfo);
				}
				return takeWhileFunc?.Invoke(windowInfo) != false;
			}, IntPtr.Zero);
			return result;
		}
	}
}