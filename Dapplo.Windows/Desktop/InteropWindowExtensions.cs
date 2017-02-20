#region Dapplo 2017 - GNU Lesser General Public License

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
using System.Drawing;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using Dapplo.Windows.SafeHandles;
using System.Threading.Tasks;
using Dapplo.Windows.Extensions;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Extensions for the interopWindow, all get or set commands update the value in the InteropWindow that is used.
	/// </summary>
	public static class InteropWindowExtensions
	{
		/// <summary>
		///     Fill ALL the information of the InteropWindow
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="cacheFlags">InteropWindowCacheFlags to specify which information is retrieved and what not</param>
		public static IInteropWindow Fill(this IInteropWindow interopWindow, InteropWindowCacheFlags cacheFlags = InteropWindowCacheFlags.CacheAll)
		{
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Children) && cacheFlags.HasFlag(InteropWindowCacheFlags.ZOrderedChildren))
			{
				throw new ArgumentException("Can't have both Children & ZOrderedChildren", nameof(cacheFlags));
			}
			bool forceUpdate = cacheFlags.HasFlag(InteropWindowCacheFlags.ForceUpdate);

			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Bounds))
			{
				interopWindow.GetBounds(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.ClientBounds))
			{
				interopWindow.GetClientBounds(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Caption))
			{
				interopWindow.GetCaption(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Classname))
			{
				interopWindow.GetClassname(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.ExtendedStyle))
			{
				interopWindow.GetExtendedStyle(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Style))
			{
				interopWindow.GetStyle(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.ProcessId))
			{
				interopWindow.GetProcessId(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Parent))
			{
				interopWindow.GetParent(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Visible))
			{
				interopWindow.IsVisible(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Maximized))
			{
				interopWindow.IsMaximized(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Minimized))
			{
				interopWindow.IsMinimized(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.ScrollInfo))
			{
				interopWindow.GetWindowScroller(forceUpdate: forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Children))
			{
				interopWindow.GetChildren(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.ZOrderedChildren))
			{
				interopWindow.GetZOrderedChildren(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Placement))
			{
				interopWindow.GetPlacement(forceUpdate);
			}
			if (cacheFlags.HasFlag(InteropWindowCacheFlags.Text))
			{
				interopWindow.GetText(forceUpdate);
			}
			return interopWindow;
		}

		/// <summary>
		///     Get the Window bounds
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>RECT</returns>
		public static RECT GetBounds(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.Bounds.HasValue || forceUpdate)
			{
				RECT rectangle;
				User32.GetWindowRect(interopWindow.Handle, out rectangle);
				interopWindow.Bounds = rectangle;
			}
			return interopWindow.Bounds.Value;
		}

		/// <summary>
		///     Get the client bounds
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>RECT</returns>
		public static RECT GetClientBounds(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.ClientBounds.HasValue || forceUpdate)
			{
				RECT rectangle;
				User32.GetClientRect(interopWindow.Handle, out rectangle);
				interopWindow.ClientBounds = rectangle;
			}
			return interopWindow.ClientBounds.Value;
		}

		/// <summary>
		/// Get the Windows caption (title)
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>string with the caption</returns>
		public static string GetCaption(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (interopWindow.Caption == null || forceUpdate)
			{
				var caption = User32.GetText(interopWindow.Handle);
				interopWindow.Caption = caption;
			}
			return interopWindow.Caption;
		}

		/// <summary>
		/// Get text from the window
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>string with the text</returns>
		public static string GetText(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (interopWindow.Text == null || forceUpdate)
			{
				var text = User32.GetTextFromWindow(interopWindow.Handle);
				interopWindow.Text = text;
			}
			return interopWindow.Text;
		}

		/// <summary>
		/// Get the parent
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>IntPtr for the parent</returns>
		public static IntPtr GetParent(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.Parent.HasValue || forceUpdate)
			{
				var parent = User32.GetParent(interopWindow.Handle);
				interopWindow.Parent = parent;
			}
			return interopWindow.Parent.Value;
		}

		/// <summary>
		/// Get the Windows class name
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>string with the classname</returns>
		public static string GetClassname(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (interopWindow.Classname == null || forceUpdate)
			{
				var className = User32.GetClassname(interopWindow.Handle);
				interopWindow.Classname = className;
			}
			return interopWindow.Classname;
		}

		/// <summary>
		///     Get the WindowPlacement
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>WindowPlacement</returns>
		public static WindowPlacement GetPlacement(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.Placement.HasValue || forceUpdate)
			{
				WindowPlacement placement = WindowPlacement.Default;
				User32.GetWindowPlacement(interopWindow.Handle, ref placement);
				interopWindow.Placement = placement;
			}
			return interopWindow.Placement.Value;
		}

		/// <summary>
		///     Set the WindowPlacement
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="placement">WindowPlacement</param>
		public static void SetPlacement(this IInteropWindow interopWindow, WindowPlacement placement)
		{
			User32.SetWindowPlacement(interopWindow.Handle, ref placement);
			interopWindow.Placement = placement;
		}

		/// <summary>
		///     Get the Extended WindowStyle
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>ExtendedWindowStyleFlags</returns>
		public static ExtendedWindowStyleFlags GetExtendedStyle(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.ExtendedStyle.HasValue || forceUpdate)
			{
				var extendedWindowStyleFlags = (ExtendedWindowStyleFlags)User32.GetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_EXSTYLE);
				interopWindow.ExtendedStyle = extendedWindowStyleFlags;
			}
			return interopWindow.ExtendedStyle.Value;
		}

		/// <summary>
		///     Set the Extended WindowStyle
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="extendedWindowStyleFlags">ExtendedWindowStyleFlags</param>
		public static void SetExtendedStyle(this IInteropWindow interopWindow, ExtendedWindowStyleFlags extendedWindowStyleFlags)
		{
			interopWindow.ExtendedStyle = extendedWindowStyleFlags;
			User32.SetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)extendedWindowStyleFlags));
		}

		/// <summary>
		///     Get the WindowStyle
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>WindowStyleFlags</returns>
		public static WindowStyleFlags GetStyle(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.Style.HasValue || forceUpdate)
			{
				var windowStyleFlags = (WindowStyleFlags)User32.GetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_STYLE);
				interopWindow.Style = windowStyleFlags;
			}
			return interopWindow.Style.Value;
		}

		/// <summary>
		///     Set the WindowStyle
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="windowStyleFlags">WindowStyleFlags</param>
		public static void SetStyle(this IInteropWindow interopWindow, WindowStyleFlags windowStyleFlags)
		{
			interopWindow.Style = windowStyleFlags;
			User32.SetWindowLongWrapper(interopWindow.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint)windowStyleFlags));
		}

		/// <summary>
		/// Get the process which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>int with process Id</returns>
		public static int GetProcessId(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.ProcessId.HasValue || forceUpdate)
			{
				int processId;
				User32.GetWindowThreadProcessId(interopWindow.Handle, out processId);
				interopWindow.ProcessId = processId;
			}
			return interopWindow.ProcessId.Value;
		}

		/// <summary>
		/// Retrieve if the window is minimized (Iconic)
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>bool true if Iconic (minimized)</returns>
		public static bool IsMinimized(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.IsMinimized.HasValue || forceUpdate)
			{
				interopWindow.IsMinimized = User32.IsIconic(interopWindow.Handle);
			}
			return interopWindow.IsMinimized.Value;
		}

		/// <summary>
		/// Retrieve if the window is maximized (Iconic)
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>bool true if Iconic (minimized)</returns>
		public static bool IsMaximized(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.IsMaximized.HasValue || forceUpdate)
			{
				interopWindow.IsMaximized = User32.IsZoomed(interopWindow.Handle);
			}
			return interopWindow.IsMaximized.Value;
		}

		/// <summary>
		/// Minimize the Window
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		public static void Minimize(this IInteropWindow interopWindow)
		{
			User32.ShowWindow(interopWindow.Handle, ShowWindowCommands.Minimize);
			interopWindow.IsMinimized = true;
		}

		/// <summary>
		/// Restore (Un-Minimize/Maximize) the Window
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		public static void Restore(this IInteropWindow interopWindow)
		{
			User32.ShowWindow(interopWindow.Handle, ShowWindowCommands.Restore);
			interopWindow.IsMinimized = false;
			interopWindow.IsMaximized = false;
		}

		/// <summary>
		/// Maximize the window
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		public static void Maximized(this IInteropWindow interopWindow)
		{
			User32.ShowWindow(interopWindow.Handle, ShowWindowCommands.Maximize);
			interopWindow.IsMaximized = true;
			interopWindow.IsMinimized = false;
		}

		/// <summary>
		/// Retrieve if the window is Visible (IsWindowVisible, whatever that means)
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>bool true if minimizedIconic (minimized)</returns>
		public static bool IsVisible(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (!interopWindow.IsVisible.HasValue || forceUpdate)
			{
				interopWindow.IsVisible = User32.IsWindowVisible(interopWindow.Handle);
			}
			return interopWindow.IsVisible.Value;
		}

		/// <summary>
		/// Get the children of the specified interopWindow, this is not lazy!
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">True to force updating</param>
		/// <returns>IEnumerable with InteropWindow</returns>
		public static IEnumerable<IInteropWindow> GetChildren(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (interopWindow.HasChildren && !forceUpdate)
			{
				return interopWindow.Children;
			}
			var children = new List<IInteropWindow>();
			// Store it in the Children property
			interopWindow.Children = children;
			foreach (var child in WindowsEnumerator.EnumerateWindows(interopWindow))
			{
				children.Add(child);
			}
			return children;
		}

		/// <summary>
		/// Get the children of the specified interopWindow, from top to bottom. This is not lazy
		/// This might get different results than the GetChildren
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <param name="forceUpdate">True to force updating</param>
		/// <returns>IEnumerable with InteropWindow</returns>
		public static IEnumerable<IInteropWindow> GetZOrderedChildren(this IInteropWindow interopWindow, bool forceUpdate = false)
		{
			if (interopWindow.HasChildren && !forceUpdate)
			{
				return interopWindow.Children;
			}
			var children = new List<IInteropWindow>();
			// Store it in the Children property
			interopWindow.Children = children;
			foreach (var child in InteropWindowQuery.GetTopWindows(interopWindow))
			{
				children.Add(child);
			}
			return children;
		}

		/// <summary>
		/// Get the region for a window
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		public static Region GetRegion(this IInteropWindow interopWindow)
		{
			using (SafeRegionHandle region = Gdi32.CreateRectRgn(0, 0, 0, 0))
			{
				if (!region.IsInvalid)
				{
					var result = User32.GetWindowRgn(interopWindow.Handle, region);
					if (result != RegionResults.REGION_ERROR && result != RegionResults.REGION_NULLREGION)
					{
						return Region.FromHrgn(region.DangerousGetHandle());
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Set the window as foreground window
		/// </summary>
		/// <param name="interopWindow">The window to bring to the foreground</param>
		/// <param name="workaround">bool with true to use a trick to really bring the window to the foreground</param>
		public static async Task ToForegroundAsync(this IInteropWindow interopWindow, bool workaround = true)
		{
			// Nothing we can do if it's not visible!
			if (!interopWindow.IsVisible())
			{
				return;
			}
			if (interopWindow.IsMinimized())
			{
				interopWindow.Restore();
				while (interopWindow.IsMinimized())
				{
					await Task.Delay(50).ConfigureAwait(false);
				}
			}
			// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539(v=vs.85).aspx
			if (workaround)
			{
				// Simulate an "ALT" key press.
				InputGenerator.KeyPress(VirtualKeyCodes.MENU);
			}
			// Show window in forground.
			User32.BringWindowToTop(interopWindow.Handle);
			User32.SetForegroundWindow(interopWindow.Handle);
		}

		/// <summary>
		/// Get the Border size for GDI Windows
		/// </summary>
		/// <param name="interopWindow">InteropWindow</param>
		/// <returns>bool true if it worked</returns>	
		public static SIZE GetBorderSize(this IInteropWindow interopWindow)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			if (User32.GetWindowInfo(interopWindow.Handle, ref windowInfo))
			{
				return new SIZE((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			}
			return SIZE.Empty;
		}

		/// <summary>
		/// Extension method to create a WindowScroller
		/// </summary>
		/// <param name="interopWindow">IInteropWindow</param>
		/// <param name="scrollBar">ScrollBarTypes</param>
		/// <param name="forceUpdate">true to force a retry, even if the previous check failed</param>
		/// <returns>WindowScroller or null</returns>
		public static WindowScroller GetWindowScroller(this IInteropWindow interopWindow, ScrollBarTypes scrollBar = ScrollBarTypes.Vertical, bool forceUpdate = false)
		{
			if (!forceUpdate && interopWindow.CanScroll.HasValue && !interopWindow.CanScroll.Value)
			{
				return null;
			}
			var initialScrollInfo = new ScrollInfo(ScrollInfoMask.All);
			if (User32.GetScrollInfo(interopWindow.Handle, scrollBar, ref initialScrollInfo) && initialScrollInfo.nMin != initialScrollInfo.nMax)
			{
				var windowScroller = new WindowScroller
				{
					ScrollingArea = interopWindow,
					ScrollBar = scrollBar,
					InitialScrollInfo = initialScrollInfo,
					WheelDelta = (int)(120 * (initialScrollInfo.nPage / WindowScroller.ScrollWheelLinesFromRegistry))
				};
				windowScroller.FillScrollbarInfo();
				interopWindow.CanScroll = true;
				return windowScroller;
			}
			if (User32.GetScrollInfo(interopWindow.Handle, ScrollBarTypes.Control, ref initialScrollInfo) && initialScrollInfo.nMin != initialScrollInfo.nMax)
			{
				var windowScroller = new WindowScroller
				{
					ScrollingArea = interopWindow,
					ScrollBar = ScrollBarTypes.Control,
					InitialScrollInfo = initialScrollInfo,
					WheelDelta = (int)(120 * (initialScrollInfo.nPage / WindowScroller.ScrollWheelLinesFromRegistry))
				};
				interopWindow.CanScroll = true;
				windowScroller.FillScrollbarInfo();
				return windowScroller;
			}
			interopWindow.CanScroll = false;
			return null;
		}

		/// <summary>
		/// Returns if the IInteropWindow is docked to the left of the other IInteropWindow
		/// </summary>
		/// <param name="window1">IInteropWindow</param>
		/// <param name="window2">IInteropWindow</param>
		/// <param name="retrieveBoundsFunc">Function which returns the bounds for the IInteropWindow</param>
		/// <returns>bool true if docked</returns>
		public static bool IsDockedToLeft(this IInteropWindow window1, IInteropWindow window2, Func<IInteropWindow, RECT> retrieveBoundsFunc = null)
		{
			retrieveBoundsFunc = retrieveBoundsFunc ?? (window => window.GetBounds());
			return retrieveBoundsFunc(window1).IsDockedToLeft(retrieveBoundsFunc(window2));
		}

		/// <summary>
		/// Returns if the IInteropWindow is docked to the left of the other IInteropWindow
		/// </summary>
		/// <param name="window1">IInteropWindow</param>
		/// <param name="window2">IInteropWindow</param>
		/// <param name="retrieveBoundsFunc">Function which returns the bounds for the IInteropWindow</param>
		/// <returns>bool true if docked</returns>
		public static bool IsDockedToRight(this IInteropWindow window1, IInteropWindow window2, Func<IInteropWindow, RECT> retrieveBoundsFunc = null)
		{
			retrieveBoundsFunc = retrieveBoundsFunc ?? (window => window.GetBounds());
			return retrieveBoundsFunc(window1).IsDockedToRight(retrieveBoundsFunc(window2));
		}
	}
}