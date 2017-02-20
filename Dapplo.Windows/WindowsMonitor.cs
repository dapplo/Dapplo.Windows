//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows
{
	/// <summary>
	///     The WinEventHook registers itself to all for us important Windows events.
	///     This makes it possible to know a.o. when a window is created, moved, updated and closed.
	///     The information in the InteropWindow objects is than updated accordingly, so when capturing everything is already
	///     available.
	///     !!!! WORK IN PROGRESS !!!!
	/// </summary>
	public class WindowsMonitor : IDisposable
	{
		private readonly WindowsEventHook _hook = new WindowsEventHook();
		private readonly IDictionary<IntPtr, InteropWindow> _windowsCache = new ConcurrentDictionary<IntPtr, InteropWindow>();

		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Add a Top-Window from the windows list
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="focus"></param>
		private void AddTopWindow(InteropWindow interopWindow, bool focus)
		{
			if (focus)
			{
				//_windowsCache.Insert(0, nativeWindow);
			}
		}

		/// <summary>
		///     Dispose the hook
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_hook.Dispose();
			}
		}

		private void MoveTopWindowToFront(InteropWindow interopWindow)
		{
			//int index = _windowsCache.IndexOf(interopWindow);
			//if (index > 0) {
			//__windowsCache.Move(index, 0);
			//LOG.DebugFormat("Focus: '{0}' - '{1}' / class '{2}'", interopWindow.Handle, interopWindow.Text, interopWindow.Classname);
			//}
		}

		/// <summary>
		///     Remove a Top window from the windows list
		/// </summary>
		/// <param name="interopWindow"></param>
		private void RemoveTopWindow(InteropWindow interopWindow)
		{
			//_windowsCache.Remove(interopWindow.);
		}

		/// <summary>
		///     Add all missing parents for the supplied interopWindow, returns the first parent
		///     In this method, currently, the top-level parent is added to the chain...
		///     This means it looks like this window has focus, even if it doesn't.
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="currentNativeWindow">Window to make the parent chain for</param>
		private void UpdateParentChainFor(WinEvents eventType, InteropWindow currentInteropWindow)
		{
			var prevWindow = currentNativeWindow;
			InteropWindow parentWindow;
			// Parent not available, create chain
			while (prevWindow.HasParent && !_windowsCache.TryGetValue(prevWindow.Parent, out parentWindow))
			{
				parentWindow = interopWindow.CreateFor(prevWindow.Parent);
				// check top level window
				if (!parentWindow.HasParent)
				{
					AddTopWindow(parentWindow, false);
				}
				else
				{
					_windowsCache.Add(parentWindow.Handle, parentWindow);
				}
				if (!parentWindow.Children.Contains(prevWindow))
				{
					parentWindow.Children.Add(prevWindow);
				}

				// go up in parent chain
				prevWindow = parentWindow;
			}
			// Set the direct parent window, needed for the log statement
			_windowsCache.TryGetValue(currentinteropWindow.Parent, out parentWindow);

			// Update children if needed
			if (parentWindow != null)
			{
				switch (eventType)
				{
					case WinEvents.EVENT_OBJECT_DESTROY:
						if (parentWindow.Children.Contains(currentNativeWindow))
						{
							parentWindow.Children.Remove(currentNativeWindow);
						}
						break;
					default:
						if (!parentWindow.Children.Contains(currentNativeWindow))
						{
							// Parent already there, so we just add the new window as child
							parentWindow.Children.Add(currentNativeWindow);
						}
						break;
				}
			}
		}

		/// <summary>
		///     WinEventDelegate for the creation and destruction
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventHandler(WinEvents eventType, IntPtr hWnd, ObjectIdentifiers idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if ((hWnd == IntPtr.Zero) || ((idObject != ObjectIdentifiers.Window) && (idObject != ObjectIdentifiers.Client)))
			{
				//if (idObject != EventObjects.OBJID_CARET && idObject != EventObjects.OBJID_CURSOR) {
				//	LOG.InfoFormat("Unhandled eventType: {0}, hWnd {1}, idObject {2}, idChild {3}, dwEventThread {4}, dwmsEventTime {5}", eventType, hWnd, idObject, idChild, dwEventThread, dwmsEventTime);
				//}
				return;
			}
			NativeWindow currentNativeWindow;
			var isPreviouslyCreated = _windowsCache.TryGetValue(hWnd, out currentNativeWindow);
			// Check if we know this window, if not we might want to get the details on it
			if (!isPreviouslyCreated)
			{
				if (eventType == WinEvents.EVENT_OBJECT_DESTROY)
				{
					// Not a peeps, destroy of not know window doesn't interrest us!
					return;
				}
				currentNativeWindow = interopWindow.CreateFor(hWnd);
				// Skip OleMainThreadWndClass Windows, they are not interessting
				if (currentinteropWindow.HasClassname && (currentinteropWindow.Classname == "OleMainThreadWndClass"))
				{
					// Not a peeps, this window is not interresting and only disturbs the log
					return;
				}
				_windowsCache.Add(currentinteropWindow.Handle, currentNativeWindow);
			}

			// currentNativeWindow can't be null!

			// If we didn't know of this window yet, assume it is "created"
			if (!isPreviouslyCreated)
			{
				UpdateParentChainFor(eventType, currentNativeWindow);
				if (!currentinteropWindow.HasParent)
				{
					// Doesn't have a parent, it's a top window! Only add as first if it's new or has focus
					AddTopWindow(currentinteropWindow, (eventType == WinEvents.EVENT_OBJECT_CREATE) || (eventType == WinEvents.EVENT_OBJECT_FOCUS));
					// Doesn't have a parent, it's a top window!
				}
			}

			// Handle specify events
			switch (eventType)
			{
				case WinEvents.EVENT_OBJECT_NAMECHANGE:
					// Force update of Text, will be automatically updated on the next get access to the property
					currentinteropWindow.Text = null;
					break;
				case WinEvents.EVENT_OBJECT_CREATE:
					// Nothing to do, we already handled all the logic
					break;
				case WinEvents.EVENT_OBJECT_DESTROY:
					if (!currentinteropWindow.HasParent)
					{
						// Top window!
						//RemoveTopWindow(currentNativeWindow);
					}
					// Remove from cache
					_windowsCache.Remove(currentinteropWindow.Handle);
					break;
				case WinEvents.EVENT_OBJECT_FOCUS:
					// Move the top-window with the focus to the foreground
					if (!currentinteropWindow.HasParent)
					{
						MoveTopWindowToFront(currentNativeWindow);
					}
					else
					{
						var topLevelWindow = currentNativeWindow;
						while ((topLevelWindow != null) && topLevelWindow.HasParent)
						{
							if (!_windowsCache.TryGetValue(topLevelWindow.Parent, out topLevelWindow))
							{
								topLevelWindow = null;
							}
						}
						if (topLevelWindow != null)
						{
							MoveTopWindowToFront(topLevelWindow);
						}
					}
					break;
				case WinEvents.EVENT_OBJECT_LOCATIONCHANGE:
				case WinEvents.EVENT_SYSTEM_MOVESIZESTART:
				case WinEvents.EVENT_SYSTEM_MOVESIZEEND:
					// Reset location, this means at the next request the information is retrieved again.
					//System.Drawing.Rectangle prevBounds = currentinteropWindow.Bounds;
					currentinteropWindow.Bounds = Rect.Empty;
					//LOG.InfoFormat("Move/resize: from {2} to {3} - '{0}' / class '{1}'", currentinteropWindow.Text, currentinteropWindow.Classname, prevBounds, currentinteropWindow.Bounds);
					break;
			}
		}
	}
}