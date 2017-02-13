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
	/// Extensions on the NativeWindowInfo
	/// </summary>
	public static class NativeWindowInfoExtensions
	{
		/// <summary>
		///     Fill ALL the information of the NativeWindowInfo
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		public static NativeWindowInfo Fill(this NativeWindowInfo nativeWindowInfo)
		{
			nativeWindowInfo.GetBounds();
			nativeWindowInfo.GetClientBounds();
			nativeWindowInfo.GetText();
			nativeWindowInfo.GetClassname();
			nativeWindowInfo.GetExtendedWindowStyle();
			nativeWindowInfo.GetWindowStyle();
			nativeWindowInfo.GetProcessId();
			nativeWindowInfo.GetParent();
			return nativeWindowInfo;
		}

		/// <summary>
		///     Get the Window bounds
		/// </summary>
		/// <returns>RECT</returns>
		public static RECT GetBounds(this NativeWindowInfo nativeWindowInfo)
		{
			RECT rectangle;
			User32.GetWindowRect(nativeWindowInfo.Handle, out rectangle);
			nativeWindowInfo.Bounds = rectangle;
			return rectangle;
		}

		/// <summary>
		///     Get the client bounds
		/// </summary>
		/// <returns>RECT</returns>
		public static RECT GetClientBounds(this NativeWindowInfo nativeWindowInfo)
		{
			RECT rectangle;
			User32.GetClientRect(nativeWindowInfo.Handle, out rectangle);
			nativeWindowInfo.Bounds = rectangle;
			return rectangle;
		}

		/// <summary>
		///     Get the Windows text
		/// </summary>
		public static string GetText(this NativeWindowInfo nativeWindowInfo)
		{
			var text = User32.GetText(nativeWindowInfo.Handle);
			nativeWindowInfo.Text = text;
			return text;
		}

		/// <summary>
		///     Get the parent
		/// </summary>
		public static IntPtr GetParent(this NativeWindowInfo nativeWindowInfo)
		{
			var parent = User32.GetParent(nativeWindowInfo.Handle);
			nativeWindowInfo.Parent = parent;
			return parent;
		}

		/// <summary>
		///     Get the Windows class name
		/// </summary>
		public static string GetClassname(this NativeWindowInfo nativeWindowInfo)
		{
			var className = User32.GetClassname(nativeWindowInfo.Handle);
			nativeWindowInfo.Classname = className;
			return className;
		}

		/// <summary>
		///     Get the Extended WindowStyle
		/// </summary>
		public static ExtendedWindowStyleFlags GetExtendedWindowStyle(this NativeWindowInfo nativeWindowInfo)
		{
			var extendedWindowStyleFlags = (ExtendedWindowStyleFlags)User32.GetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_EXSTYLE);
			nativeWindowInfo.ExtendedWindowStyle = extendedWindowStyleFlags;
			return extendedWindowStyleFlags;
		}

		/// <summary>
		///     Set the Extended WindowStyle
		/// </summary>
		public static void SetExtendedWindowStyle(this NativeWindowInfo nativeWindowInfo, ExtendedWindowStyleFlags extendedWindowStyleFlags)
		{
			nativeWindowInfo.ExtendedWindowStyle = extendedWindowStyleFlags;
			User32.SetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)extendedWindowStyleFlags));
		}

		/// <summary>
		///     Get the WindowStyle
		/// </summary>
		public static WindowStyleFlags GetWindowStyle(this NativeWindowInfo nativeWindowInfo)
		{
			var windowStyleFlags = (WindowStyleFlags)User32.GetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_STYLE);
			nativeWindowInfo.WindowStyle = windowStyleFlags;
			return windowStyleFlags;
		}

		/// <summary>
		///     Set the WindowStyle
		/// </summary>
		public static void SetWindowStyle(this NativeWindowInfo nativeWindowInfo, WindowStyleFlags windowStyleFlags)
		{
			nativeWindowInfo.WindowStyle = windowStyleFlags;
			User32.SetWindowLongWrapper(nativeWindowInfo.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint)windowStyleFlags));
		}

		/// <summary>
		/// Get the process which the specified window belongs to, the value is cached into the ProcessId of the WindowInfo
		/// </summary>
		/// <param name="nativeWindowInfo">NativeWindowInfo</param>
		/// <returns>int with process Id</returns>
		public static int GetProcessId(this NativeWindowInfo nativeWindowInfo)
		{
			int processId = nativeWindowInfo.ProcessId;
			if (processId == 0)
			{
				User32.GetWindowThreadProcessId(nativeWindowInfo.Handle, out processId);
				nativeWindowInfo.ProcessId = processId;
			}
			return processId;
		}

	}
}