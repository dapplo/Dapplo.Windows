/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
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

using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dapplo.Windows
{
	/// <summary>
	/// The WinEventHook registers itself to all for us important Windows events.
	/// This makes it possible to know a.o. when a window is created, moved, updated and closed.
	/// The information in the WindowInfo objects is than updated accordingly, so when capturing everything is already available.
	/// !!!! WORK IN PROGRESS !!!!
	/// </summary>
	public class WindowsMonitor : IDisposable {
		private readonly IDictionary<IntPtr, WindowInfo> _windowsCache = new ConcurrentDictionary<IntPtr, WindowInfo>();
		private readonly WindowsEventHook _hook = new WindowsEventHook();

		void IDisposable.Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose the hook
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				_hook.Dispose();
			}
		}

		/// <summary>
		/// WinEventDelegate for the creation and destruction
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventHandler(WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
			if (hWnd == IntPtr.Zero || (idObject != EventObjects.OBJID_WINDOW && idObject != EventObjects.OBJID_CLIENT)) {
				//if (idObject != EventObjects.OBJID_CARET && idObject != EventObjects.OBJID_CURSOR) {
				//	LOG.InfoFormat("Unhandled eventType: {0}, hWnd {1}, idObject {2}, idChild {3}, dwEventThread {4}, dwmsEventTime {5}", eventType, hWnd, idObject, idChild, dwEventThread, dwmsEventTime);
				//}
				return;
			}
			WindowInfo currentWindowInfo;
			bool isPreviouslyCreated = _windowsCache.TryGetValue(hWnd, out currentWindowInfo);
			// Check if we know this window, if not we might want to get the details on it
			if (!isPreviouslyCreated) {
				if (eventType == WinEvent.EVENT_OBJECT_DESTROY) {
					// Not a peeps, destroy of not know window doesn't interrest us!
					return;
				}
				currentWindowInfo = WindowInfo.CreateFor(hWnd);
				// Skip OleMainThreadWndClass Windows, they are not interessting
				if (currentWindowInfo.HasClassname && currentWindowInfo.Classname == "OleMainThreadWndClass") {
					// Not a peeps, this window is not interresting and only disturbs the log
					return;
				}
				_windowsCache.Add(currentWindowInfo.Handle, currentWindowInfo);
			}

			// currentWindowInfo can't be null!

			// If we didn't know of this window yet, assume it is "created"
			if (!isPreviouslyCreated) {
				UpdateParentChainFor(eventType, currentWindowInfo);
				if (!currentWindowInfo.HasParent) {
					// Doesn't have a parent, it's a top window! Only add as first if it's new or has focus
					AddTopWindow(currentWindowInfo, eventType == WinEvent.EVENT_OBJECT_CREATE || eventType == WinEvent.EVENT_OBJECT_FOCUS);
					// Doesn't have a parent, it's a top window!
				}
			}

			// Handle specify events
			switch (eventType) {
				case WinEvent.EVENT_OBJECT_NAMECHANGE:
					// Force update of Text, will be automatically updated on the next get access to the property
					currentWindowInfo.Text = null;
					break;
				case WinEvent.EVENT_OBJECT_CREATE:
					// Nothing to do, we already handled all the logic
					break;
				case WinEvent.EVENT_OBJECT_DESTROY:
					if (!currentWindowInfo.HasParent) {
						// Top window!
						//RemoveTopWindow(currentWindowInfo);
					}
					// Remove from cache
					_windowsCache.Remove(currentWindowInfo.Handle);
					break;
				case WinEvent.EVENT_OBJECT_FOCUS:
					// Move the top-window with the focus to the foreground
					if (!currentWindowInfo.HasParent) {
						MoveTopWindowToFront(currentWindowInfo);
					} else {
						var topLevelWindow = currentWindowInfo;
						while (topLevelWindow != null && topLevelWindow.HasParent) {
							if (!_windowsCache.TryGetValue(topLevelWindow.Parent, out topLevelWindow)) {
								topLevelWindow = null;
							}
						}
						if (topLevelWindow != null) {
							MoveTopWindowToFront(topLevelWindow);
						}
					}
					break;
				case WinEvent.EVENT_OBJECT_LOCATIONCHANGE:
				case WinEvent.EVENT_SYSTEM_MOVESIZESTART:
				case WinEvent.EVENT_SYSTEM_MOVESIZEEND:
					// Reset location, this means at the next request the information is retrieved again.
					//System.Drawing.Rectangle prevBounds = currentWindowInfo.Bounds;
					currentWindowInfo.Bounds = System.Windows.Rect.Empty;
					//LOG.InfoFormat("Move/resize: from {2} to {3} - '{0}' / class '{1}'", currentWindowInfo.Text, currentWindowInfo.Classname, prevBounds, currentWindowInfo.Bounds);
					break;
			}
		}

		/// <summary>
		/// Add all missing parents for the supplied windowInfo, returns the first parent
		/// In this method, currently, the top-level parent is added to the chain...
		/// This means it looks like this window has focus, even if it doesn't.
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="currentWindowInfo">Window to make the parent chain for</param>
		private void UpdateParentChainFor(WinEvent eventType, WindowInfo currentWindowInfo) {
			WindowInfo prevWindow = currentWindowInfo;
			WindowInfo parentWindow;
			// Parent not available, create chain
			while (prevWindow.HasParent && !_windowsCache.TryGetValue(prevWindow.Parent, out parentWindow)) {
				parentWindow = WindowInfo.CreateFor(prevWindow.Parent);
				// check top level window
				if (!parentWindow.HasParent) {
					AddTopWindow(parentWindow, false);
				} else {
					_windowsCache.Add(parentWindow.Handle, parentWindow);
				}
				if (!parentWindow.Children.Contains(prevWindow)) {
					parentWindow.Children.Add(prevWindow);
				}

				// go up in parent chain
				prevWindow = parentWindow;
			}
			// Set the direct parent window, needed for the log statement
			_windowsCache.TryGetValue(currentWindowInfo.Parent, out parentWindow);

			// Update children if needed
			if (parentWindow != null) {
				switch (eventType) {
					case WinEvent.EVENT_OBJECT_DESTROY:
						if (parentWindow.Children.Contains(currentWindowInfo)) {
							parentWindow.Children.Remove(currentWindowInfo);
						}
						break;
					default:
						if (!parentWindow.Children.Contains(currentWindowInfo)) {
							// Parent already there, so we just add the new window as child
							parentWindow.Children.Add(currentWindowInfo);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Remove a Top window from the windows list
		/// </summary>
		/// <param name="windowInfo"></param>
		private void RemoveTopWindow(WindowInfo windowInfo) {
			//_windowsCache.Remove(windowInfo.);
		}

		/// <summary>
		/// Add a Top-Window from the windows list
		/// </summary>
		/// <param name="windowInfo"></param>
		/// <param name="focus"></param>
		private void AddTopWindow(WindowInfo windowInfo, bool focus) {
			if (focus) {
				//_windowsCache.Insert(0, windowInfo);
			} else {
				//_windowsCache.Add(windowInfo);
			}
		}

		private void MoveTopWindowToFront(WindowInfo windowInfo) {
			//int index = _windowsCache.IndexOf(windowInfo);
			//if (index > 0) {
				//__windowsCache.Move(index, 0);
				//LOG.DebugFormat("Focus: '{0}' - '{1}' / class '{2}'", windowInfo.Handle, windowInfo.Text, windowInfo.Classname);
			//}
		}

	}
}
