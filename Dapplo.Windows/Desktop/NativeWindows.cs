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

using Dapplo.Windows.Structs;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Windows.Desktop
{
	public class NativeWindows {
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetParent(IntPtr hWnd);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		private extern static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		private extern static int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
		[DllImport("user32", SetLastError = true)]
		private extern static IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetClassLong(IntPtr hWnd, int nIndex);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool IsZoomed(IntPtr hwnd);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
	}
}
