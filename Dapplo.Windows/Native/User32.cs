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

using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace Dapplo.Windows.Native
{
	/// <summary>
	/// Native wrappers for the User32 DLL
	/// </summary>
	public class User32
	{
		private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

		#region Native imports
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr GetParent(IntPtr hWnd);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private extern static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private extern static int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private extern static IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr GetClassLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool IsZoomed(IntPtr hwnd);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
		[DllImport("user32", SetLastError = true)]
		public static extern int GetSystemMetrics(SystemMetric index);

		[DllImport("user32", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);
		#endregion

		/// <summary>
		/// Helper method to cast the GetLastWin32Error result to a Win32Error
		/// </summary>
		/// <returns></returns>
		private static Win32Error GetLastErrorCode()
		{
			return (Win32Error)Marshal.GetLastWin32Error();
		}

		private const uint MONITORINFOF_PRIMARY = 1;
		/// <summary>
		/// Returns the number of Displays using the Win32 functions
		/// </summary>
		/// <returns>collection of Display Info</returns>
		public static IList<DisplayInfo> AllDisplays()
		{
			var result = new List<DisplayInfo>();
			User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) {
				var monitorInfoEx = new MonitorInfoEx();
				monitorInfoEx.Init();
				bool success = User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
				if (success)
				{
					DisplayInfo displayInfo = new DisplayInfo();
					displayInfo.ScreenWidth = Math.Abs(monitorInfoEx.Monitor.Right - monitorInfoEx.Monitor.Left);
					displayInfo.ScreenHeight = Math.Abs(monitorInfoEx.Monitor.Bottom - monitorInfoEx.Monitor.Top);
					displayInfo.Bounds = monitorInfoEx.Monitor.ToRect();
					displayInfo.WorkingArea = monitorInfoEx.WorkArea.ToRect();
					displayInfo.IsPrimary = (monitorInfoEx.Flags | MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY;
					result.Add(displayInfo);
				}
				return true;
			}, IntPtr.Zero);
			return result;
		}

		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		public static bool GetWindowRect(IntPtr hWnd, out Rect rectangle)
		{
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result)
			{
				rectangle = windowInfo.rcWindow.ToRect();
			}
			else
			{
				rectangle = Rect.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		public static bool GetClientRect(IntPtr hWnd, out Rect rectangle)
		{
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result)
			{
				rectangle = windowInfo.rcClient.ToRect();
			}
			else
			{
				rectangle = Rect.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the Border size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="size"></param>
		/// <returns>bool true if it worked</returns>	
		public static bool GetBorderSize(IntPtr hWnd, out Size size)
		{
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result)
			{
				size = new Size((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			}
			else
			{
				size = Size.Empty;
			}
			return result;
		}

		/// <summary>
		/// Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="nIndex">int</param>
		/// <returns>IntPtr</returns>
		public static IntPtr GetClassLongWrapper(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size > 4)
			{
				return GetClassLongPtr(hWnd, nIndex);
			}
			return GetClassLong(hWnd, nIndex);
		}

		/// <summary>
		/// Retrieve the windows title, also called Text
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetText(IntPtr hWnd)
		{
			StringBuilder title = new StringBuilder(260, 260);
			int length = GetWindowText(hWnd, title, title.Capacity);
			if (length == 0)
			{
				Win32Error error = GetLastErrorCode();
				if (error != Win32Error.Success)
				{
					return null;
				}
			}
			return title.ToString();
		}

		/// <summary>
		/// Retrieve the windows classname
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetClassname(IntPtr hWnd)
		{
			StringBuilder classNameBuilder = new StringBuilder(260, 260);
			int hresult = GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			if (hresult == 0)
			{
				return null;
			}
			return classNameBuilder.ToString();
		}

		/// <summary>
		/// Get the icon for a hWnd
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns>System.Drawing.Icon</returns>
		public static System.Drawing.Icon GetIcon(IntPtr hWnd)
		{
			IntPtr iconSmall = IntPtr.Zero;
			IntPtr iconBig = new IntPtr(1);
			IntPtr iconSmall2 = new IntPtr(2);

			IntPtr iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, (int)ClassLongIndex.GCL_HICONSM);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, (int)ClassLongIndex.GCL_HICON);
			}

			if (iconHandle == IntPtr.Zero)
			{
				return null;
			}

			return System.Drawing.Icon.FromHandle(iconHandle);
		}

	}
}
