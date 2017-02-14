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
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Extensions for the NativeWindowInfo, all get or set commands update the value in the NativeWindowInfo that is used.
	/// </summary>
	public static class NativeWindowInfoExtensions
	{
		/// <summary>
		///     Fill ALL the information of the NativeWindowInfo
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">true to force updating the values</param>
		public static NativeWindowInfo Fill(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			nativeWindowInfo.GetBounds(forceUpdate);
			nativeWindowInfo.GetClientBounds(forceUpdate);
			nativeWindowInfo.GetText(forceUpdate);
			nativeWindowInfo.GetClassname(forceUpdate);
			nativeWindowInfo.GetExtendedStyle(forceUpdate);
			nativeWindowInfo.GetStyle(forceUpdate);
			nativeWindowInfo.GetProcessId(forceUpdate);
			nativeWindowInfo.GetParent(forceUpdate);
			nativeWindowInfo.GetPlacement(forceUpdate);
			return nativeWindowInfo;
		}

		/// <summary>
		///     Get the Window bounds
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>RECT</returns>
		public static RECT GetBounds(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.Bounds.HasValue || forceUpdate)
			{
				RECT rectangle;
				User32.GetWindowRect(nativeWindowInfo.Handle, out rectangle);
				nativeWindowInfo.Bounds = rectangle;
			}
			return nativeWindowInfo.Bounds.Value;
		}

		/// <summary>
		///     Get the client bounds
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>RECT</returns>
		public static RECT GetClientBounds(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.ClientBounds.HasValue || forceUpdate)
			{
				RECT rectangle;
				User32.GetClientRect(nativeWindowInfo.Handle, out rectangle);
				nativeWindowInfo.ClientBounds = rectangle;
			}
			return nativeWindowInfo.ClientBounds.Value;
		}

		/// <summary>
		/// Get the Windows text (title)
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>string with the text</returns>
		public static string GetText(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (nativeWindowInfo.Text == null || forceUpdate)
			{
				var text = User32.GetText(nativeWindowInfo.Handle);
				nativeWindowInfo.Text = text;
			}
			return nativeWindowInfo.Text;
		}

		/// <summary>
		/// Get the parent
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>IntPtr for the parent</returns>
		public static IntPtr GetParent(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.Parent.HasValue || forceUpdate)
			{
				var parent = User32.GetParent(nativeWindowInfo.Handle);
				nativeWindowInfo.Parent = parent;
			}
			return nativeWindowInfo.Parent.Value;
		}

		/// <summary>
		/// Get the Windows class name
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>string with the classname</returns>
		public static string GetClassname(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (nativeWindowInfo.Classname == null || forceUpdate)
			{
				var className = User32.GetClassname(nativeWindowInfo.Handle);
				nativeWindowInfo.Classname = className;
			}
			return nativeWindowInfo.Classname;
		}

		/// <summary>
		///     Get the WindowPlacement
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>WindowPlacement</returns>
		public static WindowPlacement GetPlacement(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.Placement.HasValue || forceUpdate)
			{
				WindowPlacement placement = WindowPlacement.Default;
				User32.GetWindowPlacement(nativeWindowInfo.Handle, ref placement);
				nativeWindowInfo.Placement = placement;
			}
			return nativeWindowInfo.Placement.Value;
		}

		/// <summary>
		///     Set the WindowPlacement
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="placement">WindowPlacement</param>
		public static void SetPlacement(this NativeWindowInfo nativeWindowInfo, WindowPlacement placement)
		{
			User32.SetWindowPlacement(nativeWindowInfo.Handle, ref placement);
			nativeWindowInfo.Placement = placement;
		}

		/// <summary>
		///     Get the Extended WindowStyle
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>ExtendedWindowStyleFlags</returns>
		public static ExtendedWindowStyleFlags GetExtendedStyle(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.ExtendedStyle.HasValue || forceUpdate)
			{
				var extendedWindowStyleFlags = (ExtendedWindowStyleFlags)User32.GetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_EXSTYLE);
				nativeWindowInfo.ExtendedStyle = extendedWindowStyleFlags;
			}
			return nativeWindowInfo.ExtendedStyle.Value;
		}

		/// <summary>
		///     Set the Extended WindowStyle
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="extendedWindowStyleFlags">ExtendedWindowStyleFlags</param>
		public static void SetExtendedStyle(this NativeWindowInfo nativeWindowInfo, ExtendedWindowStyleFlags extendedWindowStyleFlags)
		{
			nativeWindowInfo.ExtendedStyle = extendedWindowStyleFlags;
			User32.SetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)extendedWindowStyleFlags));
		}

		/// <summary>
		///     Get the WindowStyle
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>WindowStyleFlags</returns>
		public static WindowStyleFlags GetStyle(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.Style.HasValue || forceUpdate)
			{
				var windowStyleFlags = (WindowStyleFlags)User32.GetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_STYLE);
				nativeWindowInfo.Style = windowStyleFlags;
			}
			return nativeWindowInfo.Style.Value;
		}

		/// <summary>
		///     Set the WindowStyle
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="windowStyleFlags">WindowStyleFlags</param>
		public static void SetStyle(this NativeWindowInfo nativeWindowInfo, WindowStyleFlags windowStyleFlags)
		{
			nativeWindowInfo.Style = windowStyleFlags;
			User32.SetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint)windowStyleFlags));
		}

		/// <summary>
		/// Get the process which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>int with process Id</returns>
		public static int GetProcessId(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.ProcessId.HasValue || forceUpdate)
			{
				int processId;
				User32.GetWindowThreadProcessId(nativeWindowInfo.Handle, out processId);
				nativeWindowInfo.ProcessId = processId;
			}
			return nativeWindowInfo.ProcessId.Value;
		}

		/// <summary>
		/// Retrieve if the window is minimized (Iconic)
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>bool true if Iconic (minimized)</returns>
		public static bool IsMinimized(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.IsMinimized.HasValue || forceUpdate)
			{
				nativeWindowInfo.IsMinimized = User32.IsIconic(nativeWindowInfo.Handle);
			}
			return nativeWindowInfo.IsMinimized.Value;
		}

		/// <summary>
		/// Minimize the Window
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		public static void Minimize(this NativeWindowInfo nativeWindowInfo)
		{
			User32.ShowWindow(nativeWindowInfo.Handle, ShowWindowCommands.Minimize);
			nativeWindowInfo.IsMinimized = true;
		}

		/// <summary>
		/// Restore (Un-Minimize/Maximize) the Window
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		public static void Restore(this NativeWindowInfo nativeWindowInfo)
		{
			User32.ShowWindow(nativeWindowInfo.Handle, ShowWindowCommands.Restore);
			nativeWindowInfo.IsMinimized = false;
			nativeWindowInfo.IsMaximized = false;
		}


		/// <summary>
		/// Maximize the window
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		public static void Maximized(this NativeWindowInfo nativeWindowInfo)
		{
			User32.ShowWindow(nativeWindowInfo.Handle, ShowWindowCommands.Maximize);
			nativeWindowInfo.IsMaximized = true;
			nativeWindowInfo.IsMinimized = false;
		}

		/// <summary>
		/// Retrieve if the window is Visible (IsWindowVisible, whatever that means)
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <param name="forceUpdate">set to true to make sure the value is updated</param>
		/// <returns>bool true if minimizedIconic (minimized)</returns>
		public static bool IsVisible(this NativeWindowInfo nativeWindowInfo, bool forceUpdate = false)
		{
			if (!nativeWindowInfo.IsVisible.HasValue || forceUpdate)
			{
				nativeWindowInfo.IsVisible = User32.IsWindowVisible(nativeWindowInfo.Handle);
			}
			return nativeWindowInfo.IsVisible.Value;
		}

	}
}