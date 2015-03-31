/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Windows.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dapplo.Windows {
	/// <summary>
	/// The WinEventHook can register handlers to become important windows events
	/// This makes it possible to know a.o. when a window is created, moved, updated and closed.
	/// </summary>
	public class WindowsEventHook : IDisposable {
		private WinEventDelegate _winEventHandler;
		private GCHandle gcHandle;

		/// <summary>
		/// Used with Register hook
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		public delegate void WinEventHandler(WinEvent eventType, IntPtr hwnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		/// <summary>
		/// Create a WindowsEventHook object
		/// </summary>
		public WindowsEventHook() {
			_winEventHandler = WinEventDelegateHandler;
			gcHandle = GCHandle.Alloc(_winEventHandler);
		}

		#region native code
		[DllImport("user32", SetLastError = true)]
		private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr SetWinEventHook(WinEvent eventMin, WinEvent eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, int idProcess, int idThread, WinEventHookFlags dwFlags);

		/// <summary>
		/// Used with SetWinEventHook
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvent eventType, IntPtr hwnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
		#endregion

		private IDictionary<IntPtr, WinEventHandler> _winEventHandlers = new Dictionary<IntPtr, WinEventHandler>();

		/// <summary>
		/// Are hooks active?
		/// </summary>
		public bool IsHooked {
			get {
				return _winEventHandlers.Count > 0;
			}
		}

		/// <summary>
		/// Hook a WinEvent
		/// </summary>
		/// <param name="winEvent"></param>
		/// <param name="winEventHandler"></param>
		/// <returns>true if success</returns>
		public bool Hook(WinEvent winEvent, WinEventHandler winEventHandler) {
			return Hook(winEvent, winEvent, winEventHandler);
		}

		/// <summary>
		/// Hook a WinEvent
		/// </summary>
		/// <param name="winEventStart"></param>
		/// <param name="winEventEnd"></param>
		/// <param name="winEventHandler"></param>
		/// <returns></returns>
		public bool Hook(WinEvent winEventStart, WinEvent winEventEnd, WinEventHandler winEventHandler) {
			IntPtr hookPtr = SetWinEventHook(winEventStart, winEventEnd, IntPtr.Zero, _winEventHandler, 0, 0, WinEventHookFlags.WINEVENT_SKIPOWNPROCESS | WinEventHookFlags.WINEVENT_OUTOFCONTEXT);
			_winEventHandlers.Add(hookPtr, winEventHandler);
			return true;
		}

		/// <summary>
		/// Remove all hooks
		/// </summary>
		private void Unhook() {
			foreach (IntPtr hook in _winEventHandlers.Keys) {
				if (hook != IntPtr.Zero) {
					UnhookWinEvent(hook);
				}
			}
			_winEventHandlers.Clear();
			if (gcHandle != null) {
				gcHandle.Free();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				Unhook();
			}
		}

		/// <summary>
		/// Call the WinEventHandler for this event
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventDelegateHandler(IntPtr hWinEventHook, WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
			WinEventHandler handler;
			if (_winEventHandlers.TryGetValue(hWinEventHook, out handler)) {
				handler(eventType, hWnd, idObject, idChild, dwEventThread, dwmsEventTime);
			}
		}

	}
}
