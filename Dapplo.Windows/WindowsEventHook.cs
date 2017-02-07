//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows
{
	/// <summary>
	///     The WinEventHook can register handlers to become important windows events
	///     This makes it possible to know a.o. when a window is created, moved, updated and closed.
	/// </summary>
	public class WindowsEventHook : IDisposable
	{
		/// <summary>
		///     Used with Register hook
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		public delegate void WinEventHandler(WinEvents eventType, IntPtr hwnd, ObjectIdentifiers idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		private readonly WinEventDelegate _winEventHandler;

		private readonly IDictionary<IntPtr, WinEventHandler> _winEventHandlers = new Dictionary<IntPtr, WinEventHandler>();
		private GCHandle _gcHandle;

		/// <summary>
		///     Create a WindowsEventHook object
		/// </summary>
		public WindowsEventHook()
		{
			_winEventHandler = WinEventDelegateHandler;
			_gcHandle = GCHandle.Alloc(_winEventHandler);
		}

		/// <summary>
		///     Are hooks active?
		/// </summary>
		public bool IsHooked => _winEventHandlers.Count > 0;

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Unhook();
		}

		/// <summary>
		///     Hook a WinEvent
		/// </summary>
		/// <param name="winEvent"></param>
		/// <param name="winEventHandler"></param>
		/// <returns>true if success</returns>
		public bool Hook(WinEvents winEvent, WinEventHandler winEventHandler)
		{
			return Hook(winEvent, winEvent, winEventHandler);
		}

		/// <summary>
		///     Hook a WinEvent
		/// </summary>
		/// <param name="winEventStart"></param>
		/// <param name="winEventEnd"></param>
		/// <param name="winEventHandler"></param>
		public bool Hook(WinEvents winEventStart, WinEvents winEventEnd, WinEventHandler winEventHandler)
		{
			var hookPtr = SetWinEventHook(winEventStart, winEventEnd, IntPtr.Zero, _winEventHandler, 0, 0, WinEventHookFlags.SkipOwnProcess | WinEventHookFlags.OutOfContext);
			if (hookPtr.ToInt64() != 0)
			{
				_winEventHandlers.Add(hookPtr, winEventHandler);
				return true;
			}
			return false;
		}

		/// <summary>
		///     Remove all hooks
		/// </summary>
		private void Unhook()
		{
			foreach (var hookPtr in _winEventHandlers.Keys)
			{
				if (hookPtr != IntPtr.Zero)
				{
					UnhookWinEvent(hookPtr);
				}
			}
			_winEventHandlers.Clear();
			_gcHandle.Free();
		}

		/// <summary>
		///     Call the WinEventHandler for this event
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventDelegateHandler(IntPtr hWinEventHook, WinEvents eventType, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			WinEventHandler handler;
			if (_winEventHandlers.TryGetValue(hWinEventHook, out handler))
			{
				handler(eventType, hWnd, idObject, idChild, dwEventThread, dwmsEventTime);
			}
		}

		#region native code

		[DllImport("user32", SetLastError = true)]
		private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr SetWinEventHook(WinEvents eventMin, WinEvents eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, int idProcess, int idThread, WinEventHookFlags dwFlags);

		/// <summary>
		///     Used with SetWinEventHook
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvents eventType, IntPtr hwnd, ObjectIdentifiers idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		#endregion
	}
}