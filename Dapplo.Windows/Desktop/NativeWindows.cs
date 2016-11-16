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
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
	public class NativeWindows
	{
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetClassLong(IntPtr hWnd, int nIndex);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsZoomed(IntPtr hwnd);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);

		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);
	}
}