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
using System.Windows;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Information about a native window
	/// Note: This is a dumb container, and doesn't retrieve anything about the window itself
	/// </summary>
	public class NativeWindowInfo
	{
		/// <summary>
		/// Returns the bounds of this window
		/// </summary>
		public RECT? Bounds { get; set; }

		/// <summary>
		/// Returns the client bounds of this window
		/// </summary>
		public RECT? ClientBounds { get; set; }

		/// <summary>
		/// Returns the children of this window
		/// </summary>
		public IList<NativeWindowInfo> Children { get; set; }

		/// <summary>
		/// string with the name of the internal class for the window
		/// </summary>
		public string Classname { get; set; }

		/// <summary>
		/// Handle (ID) of the window
		/// </summary>
		public IntPtr Handle { get; set; }

		/// <summary>
		/// Does the window have a classname?
		/// </summary>
		public bool HasClassname => !string.IsNullOrEmpty(Classname);

		/// <summary>
		/// Does this window have parent?
		/// </summary>
		public bool HasParent => Parent.HasValue && Parent != IntPtr.Zero;

		/// <summary>
		/// The parent window to which this window belongs
		/// </summary>
		public IntPtr? Parent { get; set; }

		/// <summary>
		/// Return the text (title) of the window, if any
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Returns true if the window is visible
		/// </summary>
		public bool? IsVisible { get; set; }

		/// <summary>
		/// Returns true if the window is minimized
		/// </summary>
		public bool? IsMinimized { get; set; }

		/// <summary>
		/// Get the process ID this window belongs to
		/// </summary>
		public int? ProcessId { get; set; }

		/// <summary>
		/// ExtendedWindowStyleFlags for the Window
		/// </summary>
		public ExtendedWindowStyleFlags? ExtendedWindowStyle { get; set; }

		/// <summary>
		/// WindowStyleFlags for the Window
		/// </summary>
		public WindowStyleFlags? WindowStyle { get; set; }

		/// <summary>
		/// Create a NativeWindowInfo for the supplied handle
		/// </summary>
		/// <param name="handle">IntPtr</param>
		/// <returns>NativeWindowInfo</returns>
		public static NativeWindowInfo CreateFor(IntPtr handle)
		{
			return new NativeWindowInfo
			{
				Handle = handle
			};
		}
	}
}