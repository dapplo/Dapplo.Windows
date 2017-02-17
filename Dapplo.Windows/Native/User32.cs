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
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Structs;

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
		public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

		// ReSharper disable once InconsistentNaming
		private static readonly LogSource Log = new LogSource();
		private static bool _canCallGetPhysicalCursorPos = true;

		/// <summary>
		///     Returns the number of Displays using the Win32 functions
		/// </summary>
		/// <returns>collection of Display Info</returns>
		public static IList<DisplayInfo> AllDisplays()
		{
			var result = new List<DisplayInfo>();
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr data) =>
			{
				var monitorInfoEx = new MonitorInfoEx();
				monitorInfoEx.Init();
				var success = GetMonitorInfo(monitor, ref monitorInfoEx);
				if (!success)
				{
					return true;
				}
				var displayInfo = new DisplayInfo
				{
					ScreenWidth = Math.Abs(monitorInfoEx.Monitor.Right - monitorInfoEx.Monitor.Left),
					ScreenHeight = Math.Abs(monitorInfoEx.Monitor.Bottom - monitorInfoEx.Monitor.Top),
					Bounds = monitorInfoEx.Monitor,
					BoundsRectangle = monitorInfoEx.Monitor,
					WorkingArea = monitorInfoEx.WorkArea,
					WorkingAreaRectangle = monitorInfoEx.WorkArea,
					IsPrimary = (monitorInfoEx.Flags | MonitorInfoFlags.Primary) == MonitorInfoFlags.Primary
				};
				result.Add(displayInfo);
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
		/// <param name="hWnd">IntPtr</param>
		/// <param name="size">SIZE</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetBorderSize(IntPtr hWnd, out SIZE size)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			if (result)
			{
				size = new SIZE((int) windowInfo.cxWindowBorders, (int) windowInfo.cyWindowBorders);
			}
			else
			{
				size = SIZE.Empty;
			}
			return result;
		}

		/// <summary>
		///     Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="index">ClassLongIndex</param>
		/// <returns>IntPtr</returns>
		public static IntPtr GetClassLongWrapper(IntPtr hWnd, ClassLongIndex index)
		{
			if (IntPtr.Size > 4)
			{
				return GetClassLongPtr(hWnd, index);
			}
			return GetClassLong(hWnd, index);
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
		/// <param name="rectangle">out RECT</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetClientRect(IntPtr hWnd, out RECT rectangle)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			rectangle = result ? windowInfo.rcClient : RECT.Empty;
			return result;
		}

		/// <summary>
		///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
		/// </summary>
		/// <returns>
		///     POINT with cursor location, relative to the origin of the monitor setup
		///     (i.e. negative coordinates arepossible in multiscreen setups)
		/// </returns>
		public static POINT GetCursorLocation()
		{
			if ((Environment.OSVersion.Version.Major >= 6) && _canCallGetPhysicalCursorPos)
			{
				try
				{
					POINT cursorLocation;
					if (GetPhysicalCursorPos(out cursorLocation))
					{
						return cursorLocation;
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
			return new POINT(Cursor.Position.X, Cursor.Position.Y);
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
		/// <param name="hWnd">IntPtr</param>
		/// <param name="useLargeIcons">true to try to get a big icon first</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetIcon(IntPtr hWnd, bool useLargeIcons = false)
		{
			var iconSmall = IntPtr.Zero;
			var iconBig = new IntPtr(1);
			var iconSmall2 = new IntPtr(2);

			IntPtr iconHandle;
			if (useLargeIcons)
			{
				iconHandle = SendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
				if (iconHandle == IntPtr.Zero)
				{
					iconHandle = GetClassLongWrapper(hWnd, ClassLongIndex.GCL_HICON);
				}
			}
			else
			{
				iconHandle = SendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, ClassLongIndex.GCL_HICONSM);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = SendMessage(hWnd, WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = GetClassLongWrapper(hWnd, ClassLongIndex.GCL_HICON);
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
		///     Retrieve the windows caption, also called Text
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetText(IntPtr hWnd)
		{
			var caption = new StringBuilder(260, 260);
			GetWindowText(hWnd, caption, caption.Capacity);
			return caption.ToString();
		}

		/// <summary>
		/// Get the text of a control, this is not the caption
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <returns>string with the text</returns>
		public static string GetTextFromWindow(IntPtr hWnd)
		{
			// Get the size of the string required to hold the window's text. 
			var size = SendMessage(hWnd, WindowsMessages.WM_GETTEXTLENGTH, 0, 0).ToInt32();

			// If the return is 0, there is no text. 
			if (size <= 0)
			{
				return null;
			}
			var text = new StringBuilder(size + 1);

			SendMessage(hWnd, WindowsMessages.WM_GETTEXT, text.Capacity, text);
			return text.ToString();
		}

		/// <summary>
		///     Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd">IntPtr</param>
		/// <param name="index">WindowLongIndex</param>
		/// <returns></returns>
		public static long GetWindowLongWrapper(IntPtr hwnd, WindowLongIndex index)
		{
			if (IntPtr.Size == 8)
			{
				return GetWindowLongPtr(hwnd, index).ToInt64();
			}
			return GetWindowLong(hwnd, index);
		}

		/// <summary>
		///     Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="rectangle">out RECT</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetWindowRect(IntPtr hWnd, out RECT rectangle)
		{
			var windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			var result = GetWindowInfo(hWnd, ref windowInfo);
			rectangle = result ? windowInfo.rcWindow : RECT.Empty;
			return result;
		}

		/// <summary>
		///     Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hwnd">IntPtr</param>
		/// <param name="index">WindowLongIndex</param>
		/// <param name="styleFlags"></param>
		public static void SetWindowLongWrapper(IntPtr hwnd, WindowLongIndex index, IntPtr styleFlags)
		{
			if (IntPtr.Size == 8)
			{
				SetWindowLongPtr(hwnd, index, styleFlags);
			}
			else
			{
				SetWindowLong(hwnd, index, styleFlags.ToInt32());
			}
		}

		private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

		/// <summary>
		/// Wrapper to simplify sending of inputs
		/// </summary>
		/// <param name="inputs">Input array</param>
		/// <returns>inputs send</returns>
		public static uint SendInput(Input[] inputs)
		{
			return SendInput((uint)inputs.Length, inputs, Input.Size);
		}


		#region Native imports

		/// <summary>
		/// Synthesizes keystrokes, mouse motions, and button clicks.
		/// The function returns the number of events that it successfully inserted into the keyboard or mouse input stream.
		/// If the function returns zero, the input was already blocked by another thread.
		/// To get extended error information, call GetLastError.
		/// </summary>
		[DllImport("user32", SetLastError = true)]
		public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] Input[] inputs, int cbSize);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

		/// <summary>
		/// Retrieves a handle to the specified window's parent or owner.
		///To retrieve a handle to a specified ancestor, use the GetAncestor function.
		/// </summary>
		/// <param name="hWnd">A handle to the window whose parent window handle is to be retrieved.</param>
		/// <returns>
		/// IntPtr handle to the parent window or IntPtr.Zero if none
		/// If the window is a child window, the return value is a handle to the parent window. If the window is a top-level window with the WS_POPUP style, the return value is a handle to the owner window.
		/// </returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetParent(IntPtr hWnd);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633541(v=vs.85).aspx">SetParent function</a>
		/// Changes the parent window of the specified child window.
		/// </summary>
		/// <param name="hWndChild">IntPtr</param>
		/// <param name="hWndNewParent">IntPtr</param>
		/// <returns>
		/// If the function succeeds, the return value is a handle to the previous parent window.
		/// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		/// <summary>
		/// Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window.
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515(v=vs.85).aspx">GetWindow function</a>
		/// </summary>
		/// <param name="hWnd">IntPtr A handle to a window. The window handle retrieved is relative to this window, based on the value of the uCmd parameter.</param>
		/// <param name="getWindowCommand">GetWindowCommands The relationship between the specified window and the window whose handle is to be retrieved. See GetWindowCommands</param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommands getWindowCommand);

		[DllImport("user32", SetLastError = true)]
		public static extern int ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);

		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern uint GetSysColor(int nIndex);

		/// <summary>
		/// Bring the specified window to the front
		/// </summary>
		/// <param name="hWnd">IntPtr specifying the hWnd</param>
		/// <returns>true if the call was successfull</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		/// <summary>
		/// Get the hWnd of the Desktop window
		/// </summary>
		/// <returns>IntPtr</returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();

		/// <summary>
		/// Set the current foreground window
		/// </summary>
		/// <param name="hWnd">IntPtr with the handle to the window</param>
		/// <returns>bool</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		/// <summary>
		/// Sets the keyboard focus to the specified window. The window must be attached to the calling thread's message queue.
		/// </summary>
		/// <param name="hWnd">A handle to the window that will receive the keyboard input. If this parameter is NULL, keystrokes are ignored.</param>
		/// <returns>IntPtr
		/// If the function succeeds, the return value is the handle to the window that previously had the keyboard focus.
		/// If the hWnd parameter is invalid or the window is not attached to the calling thread's message queue, the return value is NULL.
		/// To get extended error information, call GetLastError.
		/// </returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetFocus(IntPtr hWnd);

		/// <summary>
		/// Get the WindowPlacement for the specified window
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="windowPlacement">WindowPlacement</param>
		/// <returns>true if success</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement windowPlacement);

		/// <summary>
		/// Set the WindowPlacement for the specified window
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="windowPlacement">WindowPlacement</param>
		/// <returns>true if the call was sucessfull</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement windowPlacement);

		/// <summary>
		/// Return true if the specified window is minimized
		/// </summary>
		/// <param name="hWnd">IntPtr for the hWnd</param>
		/// <returns>true if minimized</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsIconic(IntPtr hWnd);

		/// <summary>
		/// Return true if the specified window is maximized
		/// </summary>
		/// <param name="hwnd">IntPtr for the hWnd</param>
		/// <returns>true if maximized</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsZoomed(IntPtr hwnd);

		/// <summary>
		/// Get the classname of the specified window
		/// </summary>
		/// <param name="hWnd">IntPtr with the hWnd</param>
		/// <param name="className">StringBuilder to place the classname into</param>
		/// <param name="nMaxCount">max size for the string builder length</param>
		/// <returns>nr of characters returned</returns>
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder className, int nMaxCount);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr GetClassLong(IntPtr hWnd, ClassLongIndex index);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetClassLongPtr")]
		public static extern IntPtr GetClassLongPtr(IntPtr hWnd, ClassLongIndex index);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd162869(v=vs.85).aspx">PrintWindow function</a>
		/// </summary>
		/// <param name="hwnd">IntPtr</param>
		/// <param name="hDc">IntPtr</param>
		/// <param name="printWindowFlags">PrintWindowFlags</param>
		/// <returns>bool</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDc, PrintWindowFlags printWindowFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, SysCommands sysCommand, IntPtr lParam);

		/// <summary>
		/// Used for WM_VSCROLL and WM_HSCROLL
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="windowsMessage">WindowsMessages</param>
		/// <param name="scrollBarCommand">ScrollBarCommands</param>
		/// <param name="lParam"></param>
		/// <returns>0</returns>
		[DllImport("user32", SetLastError = true)]
		public static extern int SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, ScrollBarCommands scrollBarCommand, int lParam);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, IntPtr lParam);

		/// <summary>
		/// Used for calls where the arguments are int
		/// </summary>
		/// <param name="hWnd">IntPtr for the Window handle</param>
		/// <param name="windowsMessage">WindowsMessages</param>
		/// <param name="wParam">int</param>
		/// <param name="lParam">int</param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, int wParam, int lParam);

		/// <summary>
		/// Used for WM_GETTEXT
		/// </summary>
		/// <param name="hWnd">IntPtr for the Window handle</param>
		/// <param name="windowsMessage"></param>
		/// <param name="wParam">int with the capacity of the string builder</param>
		/// <param name="lParam">StringBuilder</param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, int wParam, StringBuilder lParam);

		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLong")]
		private static extern int GetWindowLong(IntPtr hwnd, WindowLongIndex index);

		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, WindowLongIndex nIndex);

		[DllImport("user32", SetLastError = true)]
		private static extern int SetWindowLong(IntPtr hWnd, WindowLongIndex index, int styleFlags);

		[DllImport("user32", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndex index, IntPtr styleFlags);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

		/// <summary>
		/// The MonitorFromRect function retrieves a handle to the display monitor that has the largest area of intersection with a specified rectangle.
		/// see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
		/// </summary>
		/// <param name="rect">A RECT structure that specifies the rectangle of interest in virtual-screen coordinates.</param>
		/// <param name="monitorFromRectFlags">MonitorFromRectFlags</param>
		/// <returns>HMONITOR handle</returns>
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromRect([In] ref RECT rect, MonitorFromRectFlags monitorFromRectFlags);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
		/// </summary>
		/// <param name="enumFunc">EnumWindowsProc</param>
		/// <param name="param">An application-defined value to be passed to the callback function.</param>
		/// <returns>true if success</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr param);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633495(v=vs.85).aspx">EnumThreadWindows function</a>
		/// Enumerates all nonchild windows associated with a thread by passing the handle to each window, in turn, to an application-defined callback function.
		/// EnumThreadWindows continues until the last window is enumerated or the callback function returns FALSE.
		/// To enumerate child windows of a particular window, use the EnumChildWindows function.
		/// </summary>
		/// <param name="threadId">The identifier of the thread whose windows are to be enumerated.</param>
		/// <param name="enumFunc">EnumWindowsProc</param>
		/// <param name="param">An application-defined value to be passed to the callback function.</param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern bool EnumThreadWindows(int threadId, EnumWindowsProc enumFunc, IntPtr param);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
		/// </summary>
		/// <param name="hWndParent">IntPtr with hwnd of parent window, if this is IntPtr.Zero this function behaves as EnumWindows</param>
		/// <param name="enumFunc">EnumWindowsProc</param>
		/// <param name="param">An application-defined value to be passed to the callback function.</param>
		/// <returns>true if success</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc enumFunc, IntPtr param);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787583(v=vs.85).aspx">GetScrollInfo</a> for more information.
		/// </summary>
		/// <param name="hwnd">IntPtr with the window handle</param>
		/// <param name="scrollBar">ScrollBarTypes</param>
		/// <param name="scrollInfo">ScrollInfo ref</param>
		/// <returns>bool if it worked</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetScrollInfo(IntPtr hwnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787595(v=vs.85).aspx">SetScrollInfo</a> for more information.
		/// </summary>
		/// <param name="hwnd">IntPtr with the window handle</param>
		/// <param name="scrollBar">ScrollBarTypes</param>
		/// <param name="scrollInfo">ScrollInfo ref</param>
		/// <param name="redraw">bool to specify if a redraw should be made</param>
		/// <returns>bool if it worked</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetScrollInfo(IntPtr hwnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo, bool redraw);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787601(v=vs.85).aspx">ShowScrollBar function</a> for more information.
		/// </summary>
		/// <param name="hwnd">IntPtr</param>
		/// <param name="scrollBar">ScrollBarTypes</param>
		/// <param name="show">true to show, false to hide</param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowScrollBar(IntPtr hwnd, ScrollBarTypes scrollBar, [MarshalAs(UnmanagedType.Bool)] bool show);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787581(v=vs.85).aspx">GetScrollBarInfo function</a> for more information.
		/// </summary>
		/// <param name="hwnd">Handle to a window associated with the scroll bar whose information is to be retrieved. If the idObject parameter is OBJID_CLIENT, hwnd is a handle to a scroll bar control. Otherwise, hwnd is a handle to a window created with WS_VSCROLL and/or WS_HSCROLL style.</param>
		/// <param name="idObject">Specifies the scroll bar object. Can be ObjectIdentifiers.Client, ObjectIdentifiers.HorizontalScrollbar, ObjectIdentifiers.VerticalScrollbar</param>
		/// <param name="scrollBarInfo">ScrollBarInfo ref</param>
		/// <returns>bool</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetScrollBarInfo(IntPtr hwnd, ObjectIdentifiers idObject, ref ScrollBarInfo scrollBarInfo);

		[DllImport("user32", SetLastError = true)]
		public static extern RegionResults GetWindowRgn(IntPtr hWnd, SafeHandle hRgn);

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
		///     The following is used for Icon handling, and copies a hicon to a new
		/// </summary>
		/// <param name="hIcon">IntPtr</param>
		/// <returns>SafeIconHandle</returns>
		[DllImport("user32", SetLastError = true)]
		public static extern SafeIconHandle CopyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
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