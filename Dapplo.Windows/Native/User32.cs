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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Native wrappers for the User32 DLL
	/// </summary>
	public static class User32
	{
		/// <summary>
		///     Delegate description for the windows enumeration
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="lParam"></param>
		public delegate int EnumWindowsProc(IntPtr hwnd, int lParam);

		// ReSharper disable once InconsistentNaming
		private const uint MONITORINFOF_PRIMARY = 1;
		private static readonly LogSource Log = new LogSource();
		private static bool _canCallGetPhysicalCursorPos = true;

		/// <summary>
		///     Returns the number of Displays using the Win32 functions
		/// </summary>
		/// <returns>collection of Display Info</returns>
		public static IList<DisplayInfo> AllDisplays()
		{
			var result = new List<DisplayInfo>();
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
			{
				var monitorInfoEx = new MonitorInfoEx();
				monitorInfoEx.Init();
				var success = GetMonitorInfo(hMonitor, ref monitorInfoEx);
				if (success)
				{
					var displayInfo = new DisplayInfo
					{
						ScreenWidth = Math.Abs(monitorInfoEx.Monitor.Right - monitorInfoEx.Monitor.Left),
						ScreenHeight = Math.Abs(monitorInfoEx.Monitor.Bottom - monitorInfoEx.Monitor.Top),
						Bounds = monitorInfoEx.Monitor.ToRect(),
						BoundsRectangle = monitorInfoEx.Monitor.ToRectangle(),
						WorkingArea = monitorInfoEx.WorkArea.ToRect(),
						WorkingAreaRectangle = monitorInfoEx.WorkArea.ToRectangle(),
						IsPrimary = (monitorInfoEx.Flags | MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY
					};
					result.Add(displayInfo);
				}
				return true;
			}, IntPtr.Zero);
			return result;
		}

		/// <summary>
		///     Helper method to create a Win32 exception with the windows message in it
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <returns>Exception</returns>
		public static Exception CreateWin32Exception(string method)
		{
			var exceptionToThrow = new Win32Exception();
			exceptionToThrow.Data.Add("Method", method);
			return exceptionToThrow;
		}

		/// <summary>
		///     Helper method to get the Border size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="size"></param>
		/// <returns>bool true if it worked</returns>
		public static bool GetBorderSize(IntPtr hWnd, out Size size)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			if (result)
			{
				size = new Size((int) windowInfo.cxWindowBorders, (int) windowInfo.cyWindowBorders);
			}
			else
			{
				size = Size.Empty;
			}
			return result;
		}

		/// <summary>
		///     Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
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
		///     Retrieve the windows classname
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetClassname(IntPtr hWnd)
		{
			var classNameBuilder = new StringBuilder(260, 260);
			var hresult = GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			if (hresult == 0)
			{
				return null;
			}
			return classNameBuilder.ToString();
		}

		/// <summary>
		///     Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetClientRect(IntPtr hWnd, out Rect rectangle)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			rectangle = result ? windowInfo.rcClient.ToRect() : Rect.Empty;
			return result;
		}

		/// <summary>
		///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
		/// </summary>
		/// <returns>
		///     Point with cursor location, relative to the origin of the monitor setup
		///     (i.e. negative coordinates arepossible in multiscreen setups)
		/// </returns>
		public static Point GetCursorLocation()
		{
			if ((Environment.OSVersion.Version.Major >= 6) && _canCallGetPhysicalCursorPos)
			{
				try
				{
					POINT cursorLocation;
					if (GetPhysicalCursorPos(out cursorLocation))
					{
						return new Point(cursorLocation.X, cursorLocation.Y);
					}
					var error = Win32.GetLastErrorCode();
					Log.Error().WriteLine("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Exception retrieving PhysicalCursorPos, no longer calling this. Cause :");
					_canCallGetPhysicalCursorPos = false;
				}
			}
			return new Point(Cursor.Position.X, Cursor.Position.Y);
		}

		/// <summary>
		///     Return the count of GDI objects.
		/// </summary>
		/// <returns>Return the count of GDI objects.</returns>
		public static uint GetGuiResourcesGdiCount()
		{
			using (var currentProcess = Process.GetCurrentProcess())
			{
				return GetGuiResources(currentProcess.Handle, 0);
			}
		}

		/// <summary>
		///     Return the count of USER objects.
		/// </summary>
		/// <returns>Return the count of USER objects.</returns>
		public static uint GetGuiResourcesUserCount()
		{
			using (var currentProcess = Process.GetCurrentProcess())
			{
				return GetGuiResources(currentProcess.Handle, 1);
			}
		}

		/// <summary>
		///     Get the icon for a hWnd
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetIcon(IntPtr hWnd)
		{
			var iconSmall = IntPtr.Zero;
			var iconBig = new IntPtr(1);
			var iconSmall2 = new IntPtr(2);

			var iconHandle = SendMessage(hWnd, (int) WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, (int) WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, (int) ClassLongIndex.GCL_HICONSM);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, (int) WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, (int) ClassLongIndex.GCL_HICON);
			}

			if (iconHandle == IntPtr.Zero)
			{
				return null;
			}

			return Icon.FromHandle(iconHandle);
		}

		/// <summary>
		///     Helper method to cast the GetLastWin32Error result to a Win32Error
		/// </summary>
		/// <returns></returns>
		private static Win32Error GetLastErrorCode()
		{
			return (Win32Error) Marshal.GetLastWin32Error();
		}

		/// <summary>
		///     Retrieve the windows title, also called Text
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetText(IntPtr hWnd)
		{
			var title = new StringBuilder(260, 260);
			var length = GetWindowText(hWnd, title, title.Capacity);
			if (length == 0)
			{
				var error = GetLastErrorCode();
				if (error != Win32Error.Success)
				{
					return null;
				}
			}
			return title.ToString();
		}

		/// <summary>
		///     Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="nIndex"></param>
		/// <returns></returns>
		public static long GetWindowLongWrapper(IntPtr hwnd, WindowLongIndex nIndex)
		{
			if (IntPtr.Size == 8)
			{
				return GetWindowLongPtr(hwnd, (int) nIndex).ToInt64();
			}
			return GetWindowLong(hwnd, (int) nIndex);
		}

		/// <summary>
		///     Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetWindowRect(IntPtr hWnd, out Rect rectangle)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			rectangle = result ? windowInfo.rcWindow.ToRect() : Rect.Empty;
			return result;
		}

		/// <summary>
		///     Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="nIndex"></param>
		/// <param name="styleFlags"></param>
		public static void SetWindowLongWrapper(IntPtr hwnd, WindowLongIndex nIndex, IntPtr styleFlags)
		{
			if (IntPtr.Size == 8)
			{
				SetWindowLongPtr(hwnd, (int) nIndex, styleFlags);
			}
			else
			{
				SetWindowLong(hwnd, (int) nIndex, styleFlags.ToInt32());
			}
		}

		private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

		#region Native imports

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern int SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommand uCmd);

		[DllImport("user32", SetLastError = true)]
		public static extern int ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern uint GetSysColor(int nIndex);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsZoomed(IntPtr hwnd);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr GetClassLong(IntPtr hWnd, int nIndex);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetClassLongPtr")]
		public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, SysCommands sysCommand, IntPtr lParam);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLong")]
		private static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

		[DllImport("user32", SetLastError = true)]
		private static extern int SetWindowLong(IntPtr hWnd, int index, int styleFlags);

		[DllImport("user32", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr styleFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		[DllImport("user32", SetLastError = true)]
		public static extern int EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32", SetLastError = true)]
		public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowScrollBar(IntPtr hwnd, ScrollBarDirection scrollBar, [MarshalAs(UnmanagedType.Bool)] bool show);

		[DllImport("user32", SetLastError = true)]
		public static extern int SetScrollPos(IntPtr hWnd, Orientation nBar, int nPos, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

		[DllImport("user32", SetLastError = true)]
		public static extern RegionResult GetWindowRgn(IntPtr hWnd, SafeHandle hRgn);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, WindowPos uFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetTopWindow(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetClipboardOwner();

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		/// uiFlags: 0 - Count of GDI objects
		/// uiFlags: 1 - Count of USER objects
		/// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
		/// - Win32 USER objects:
		/// - 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
		/// - Other USER objects (windows, menus)
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetPhysicalCursorPos(out POINT cursorLocation);

		[DllImport("user32", SetLastError = true)]
		public static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref POINT lpPoints, [MarshalAs(UnmanagedType.U4)] int cPoints);

		[DllImport("user32", SetLastError = true)]
		public static extern int GetSystemMetrics(SystemMetric index);

		/// <summary>
		///     The following is used for Icon handling
		/// </summary>
		/// <param name="hIcon"></param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern SafeIconHandle CopyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorInfo(out CURSORINFO cursorInfo);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetIconInfo(SafeIconHandle iconHandle, out IconInfo iconInfo);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetCapture(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReleaseCapture();

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

		[DllImport("user32", SetLastError = true)]
		internal static extern IntPtr OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, DesktopAccessRight dwDesiredAccess);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetThreadDesktop(IntPtr hDesktop);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CloseDesktop(IntPtr hDesktop);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

		#endregion
	}
}