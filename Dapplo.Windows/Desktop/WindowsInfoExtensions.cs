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
using System.Windows;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// </summary>
	public static class WindowsInfoExtensions
	{
		/// <summary>
		///     Fill the information of the WindowInfo
		/// </summary>
		/// <param name="windowInfo">WindowInfo</param>
		public static WindowInfo Fill(this WindowInfo windowInfo)
		{
			windowInfo.Text = User32.GetText(windowInfo.Handle);
			windowInfo.Classname = User32.GetClassname(windowInfo.Handle);
			Rect rectangle;
			User32.GetClientRect(windowInfo.Handle, out rectangle);
			windowInfo.Bounds = rectangle;
			return windowInfo;
		}

		/// <summary>
		///     Get the Extended WindowStyle
		/// </summary>
		public static ExtendedWindowStyleFlags GetExtendedWindowStyle(this WindowInfo windowInfo)
		{
			return (ExtendedWindowStyleFlags) User32.GetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_EXSTYLE);
		}

		/// <summary>
		///     Set the Extended WindowStyle
		/// </summary>
		public static void SetExtendedWindowStyle(this WindowInfo windowInfo, ExtendedWindowStyleFlags value)
		{
			User32.SetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint) value));
		}

		/// <summary>
		///     Get the WindowStyle
		/// </summary>
		public static WindowStyleFlags GetWindowStyle(this WindowInfo windowInfo)
		{
			return (WindowStyleFlags) User32.GetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_STYLE);
		}

		/// <summary>
		///     Set the WindowStyle
		/// </summary>
		public static void SetWindowStyle(this WindowInfo windowInfo, WindowStyleFlags value)
		{
			User32.SetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint) value));
		}

		/// <summary>
		/// Get the process which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
		/// </summary>
		/// <param name="windowInfo">WindowInfo</param>
		/// <returns>int with process Id</returns>
		public static int GetProcessId(this WindowInfo windowInfo)
		{
			int processId = windowInfo.ProcessId;
			if (processId == 0)
			{
				User32.GetWindowThreadProcessId(windowInfo.Handle, out processId);
				windowInfo.ProcessId = processId;
			}
			return processId;
		}

	}
}