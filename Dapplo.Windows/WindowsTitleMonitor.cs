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
using System.Reflection;

namespace Dapplo.Windows {

	/// <summary>
	/// Event arguments for the TitleChangeEvent
	/// </summary>
	public class TitleChangeEventArgs : EventArgs {
		public IntPtr HWnd {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
	}

	/// <summary>
	/// Delegate for the title change event
	/// </summary>
	/// <param name="eventArgs"></param>
	public delegate void TitleChangeEventDelegate(TitleChangeEventArgs eventArgs);

	/// <summary>
	/// Monitor all title changes
	/// </summary>
	public class WindowsTitleMonitor : IDisposable {
		private WindowsEventHook _hook;
		private object lockObject = new object();
		private event TitleChangeEventDelegate _titleChangeEvent;

		/// <summary>
		/// Add / remove event handler to the title monitor
		/// </summary>
		public event TitleChangeEventDelegate TitleChangeEvent {
			add {
				lock (lockObject) {
					if (_hook == null) {
						_hook = new WindowsEventHook();
						_hook.Hook(WinEvent.EVENT_OBJECT_NAMECHANGE, WinEventHandler);
					}
					_titleChangeEvent += value;
				}
			}
			remove {
				lock (lockObject) {
					_titleChangeEvent -= value;
					if (_titleChangeEvent == null || _titleChangeEvent.GetInvocationList().Length == 0) {
						if (_hook != null) {
							_hook.Dispose();
							_hook = null;
						}
					}
				}
			}
		}

		/// <summary>
		/// WinEventDelegate for the creation & destruction
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventHandler(WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
			if (hWnd == IntPtr.Zero || idObject != EventObjects.OBJID_WINDOW) {
				return;
			}
			if (eventType == WinEvent.EVENT_OBJECT_NAMECHANGE) {
				string newTitle = User32.GetText(hWnd);
				if (_titleChangeEvent != null) {
					_titleChangeEvent(new TitleChangeEventArgs { HWnd = hWnd, Title = newTitle });
				}
			}
		}

		#region Dispose
		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose all managed resources
		/// </summary>
		/// <param name="disposing">when true is passed all managed resources are disposed.</param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				foreach (TitleChangeEventDelegate eventDelegate in _titleChangeEvent.GetInvocationList()) {
					_titleChangeEvent -= eventDelegate;
				}
				// free managed resources
				if (_hook != null) {
					_hook.Dispose();
					_hook = null;
				}
			}
		}
		#endregion
	}
}
