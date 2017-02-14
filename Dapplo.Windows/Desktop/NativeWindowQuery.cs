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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dapplo.Windows.App;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	///     Query for native windows
	/// </summary>
	public static class NativeWindowQuery
	{
		/// <summary>
		///     Window classes which can be ignored
		/// </summary>
		public static IEnumerable<string> IgnoreClasses { get; } = new List<string>(new[] {"Progman", "Button", "Dwm"}); //"MS-SDIa"

		/// <summary>
		///     Check if the window is a top level window.
		///     This method will retrieve all information, and fill it to the NativeWindowInfo, it needs to make the decision.
		/// </summary>
		/// <param name="nativeWindow">WindowDetails</param>
		/// <returns>bool</returns>
		public static bool IsTopLevel(this NativeWindowInfo nativeWindow)
		{
			if (IgnoreClasses.Contains(nativeWindow.GetClassname()))
			{
				return false;
			}
			// Ignore windows without title
			if (nativeWindow.GetText().Length == 0)
			{
				return false;
			}
			// Windows without size
			if (nativeWindow.GetBounds().IsEmpty)
			{
				return false;
			}
			if (nativeWindow.GetParent() != IntPtr.Zero)
			{
				return false;
			}
			var exWindowStyle = nativeWindow.GetExtendedStyle();
			if ((exWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
			{
				return false;
			}
			// Skip everything which is not rendered "normally"
			if (!nativeWindow.IsWin8App() && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
			{
				return false;
			}
			// Skip preview windows, like the one from Firefox
			if ((nativeWindow.GetStyle() & WindowStyleFlags.WS_VISIBLE) == 0)
			{
				return false;
			}
			return !nativeWindow.IsMinimized();
		}

		/// <summary>
		///     Iterate the Top level windows, from top to bottom
		/// </summary>
		/// <returns>IEnumerable with all the top level windows</returns>
		public static IEnumerable<NativeWindowInfo> GetTopLevelWindows()
		{

			IntPtr windowPtr = User32.GetTopWindow(IntPtr.Zero);

			do
			{
				var possibleTopLevel = NativeWindowInfo.CreateFor(windowPtr);

				if (possibleTopLevel.IsTopLevel())
				{
					yield return possibleTopLevel;
				}
				windowPtr = User32.GetWindow(windowPtr, GetWindowCommands.GW_HWNDNEXT);

			} while (windowPtr != IntPtr.Zero);
		}

		/// <summary>
		///     Get the currently active window
		/// </summary>
		/// <returns>NativeWindowInfo</returns>
		public static NativeWindowInfo GetActiveWindow()
		{
			return NativeWindowInfo.CreateFor(User32.GetForegroundWindow());
		}

		/// <summary>
		/// Gets the Desktop window
		/// </summary>
		/// <returns>NativeWindowInfo for the desktop window</returns>
		public static NativeWindowInfo GetDesktopWindow()
		{
			return NativeWindowInfo.CreateFor(User32.GetDesktopWindow());
		}

		/// <summary>
		/// Find windows belonging to the same process (thread) as the supplied window.
		/// </summary>
		/// <param name="windowToLinkTo">NativeWindowInfo</param>
		/// <returns>IEnumerable with NativeWindowInfo</returns>
		public static IEnumerable<NativeWindowInfo> GetLinkedWindows(this NativeWindowInfo windowToLinkTo)
		{
			int processIdSelectedWindow = windowToLinkTo.GetProcessId();

			using (var process = Process.GetProcessById(processIdSelectedWindow))
			{
				foreach (ProcessThread thread in process.Threads)
				{
					var handles = new List<IntPtr>();
					try
					{
						User32.EnumThreadWindows(thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
					}
					finally
					{
						thread?.Dispose();
					}
					foreach (var handle in handles)
					{
						yield return NativeWindowInfo.CreateFor(handle);
					}
				}
			}
		}
	}
}