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
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Dapplo.Windows.Native
{
	/// <summary>
	/// Native wrappers for the User32 DLL
	/// </summary>
	public class User32
	{
		private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
		public delegate int EnumWindowsProc(IntPtr hwnd, int lParam);
		private static bool _CanCallGetPhysicalCursorPos = true;

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

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLong")]
		public static extern int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
		public static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

		[DllImport("user32", SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hWnd, int index, int styleFlags);

		[DllImport("user32", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr styleFlags);

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
		public static extern bool ShowScrollBar(IntPtr hwnd, ScrollBarDirection scrollBar, bool show);

		[DllImport("user32", SetLastError = true)]
		public static extern int SetScrollPos(IntPtr hWnd, Orientation nBar, int nPos, bool bRedraw);

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
		public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetClipboardOwner();

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

		[DllImport("user32", SetLastError = true)]
		public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		/// uiFlags: 0 - Count of GDI objects
		/// uiFlags: 1 - Count of USER objects
		/// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
		/// - Win32 USER objects:
		///	- 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
		/// - Other USER objects (windows, menus)
		///
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);

		[DllImport("user32", SetLastError = true)]
		private static extern bool GetPhysicalCursorPos(out POINT cursorLocation);

		[DllImport("user32", SetLastError = true)]
		public static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref POINT lpPoints, [MarshalAs(UnmanagedType.U4)] int cPoints);

		[DllImport("user32", SetLastError = true)]
		public static extern int GetSystemMetrics(SystemMetric index);

		/// <summary>
		/// The following is used for Icon handling
		/// </summary>
		/// <param name="hIcon"></param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern SafeIconHandle CopyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetCursorInfo(out CursorInfo cursorInfo);

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
		internal static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, DesktopAccessRight dwDesiredAccess);

		[DllImport("user32", SetLastError = true)]
		internal static extern bool SetThreadDesktop(IntPtr hDesktop);

		[DllImport("user32", SetLastError = true)]
		internal static extern bool CloseDesktop(IntPtr hDesktop);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

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
					displayInfo.BoundsRectangle = monitorInfoEx.Monitor.ToRectangle();
					displayInfo.WorkingArea = monitorInfoEx.WorkArea.ToRect();
					displayInfo.WorkingAreaRectangle = monitorInfoEx.WorkArea.ToRectangle();
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

		/// <summary>
		/// Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
		/// </summary>
		/// <returns>Point with cursor location, relative to the origin of the monitor setup
		/// (i.e. negative coordinates arepossible in multiscreen setups)</returns>
		public static Point GetCursorLocation()
		{
			if (Environment.OSVersion.Version.Major >= 6 && _CanCallGetPhysicalCursorPos)
			{
				POINT cursorLocation;
				try
				{
					if (GetPhysicalCursorPos(out cursorLocation))
					{
						return new Point(cursorLocation.X, cursorLocation.Y);
					}
					else
					{
						Win32Error error = Win32.GetLastErrorCode();
						//LOG.ErrorFormat("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
					}
				}
				catch (Exception ex)
				{
					//LOG.Error("Exception retrieving PhysicalCursorPos, no longer calling this. Cause :", ex);
					_CanCallGetPhysicalCursorPos = false;
				}
			}
			return new Point(Cursor.Position.X, Cursor.Position.Y);
		}

		/// <summary>
		/// Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="nIndex"></param>
		/// <returns></returns>
		public static long GetWindowLongWrapper(IntPtr hwnd, int nIndex)
		{
			if (IntPtr.Size == 8)
			{
				return GetWindowLongPtr(hwnd, nIndex).ToInt64();
			}
			else
			{
				return GetWindowLong(hwnd, nIndex);
			}
		}

		/// <summary>
		/// Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd"></param>
		/// <param name="nIndex"></param>
		/// <param name="styleFlags"></param>
		public static void SetWindowLongWrapper(IntPtr hwnd, int nIndex, IntPtr styleFlags)
		{
			if (IntPtr.Size == 8)
			{
				SetWindowLongPtr(hwnd, nIndex, styleFlags);
			}
			else
			{
				SetWindowLong(hwnd, nIndex, styleFlags.ToInt32());
			}
		}

		public static uint GetGuiResourcesGDICount()
		{
			using (Process currentProcess = Process.GetCurrentProcess())
			{
				return GetGuiResources(currentProcess.Handle, 0);
			}
		}

		public static uint GetGuiResourcesUserCount()
		{
			using (Process currentProcess = Process.GetCurrentProcess())
			{
				return GetGuiResources(currentProcess.Handle, 1);
			}
		}

		/// <summary>
		/// Helper method to create a Win32 exception with the windows message in it
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <returns>Exception</returns>
		public static Exception CreateWin32Exception(string method)
		{
			Win32Exception exceptionToThrow = new Win32Exception();
			exceptionToThrow.Data.Add("Method", method);
			return exceptionToThrow;
		}
	}
}
