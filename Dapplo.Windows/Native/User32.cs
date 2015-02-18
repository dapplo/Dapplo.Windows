using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dapplo.Windows.Native {
	/// <summary>
	/// Native wrappers for the User32 DLL
	/// </summary>
	public class User32 {
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
		#endregion

		/// <summary>
		/// Helper method to cast the GetLastWin32Error result to a Win32Error
		/// </summary>
		/// <returns></returns>
		private static Win32Error GetLastErrorCode() {
			return (Win32Error)Marshal.GetLastWin32Error();
		}

		/// <summary>
		/// Returns the number of Displays using the Win32 functions
		/// </summary>
		/// <returns>collection of Display Info</returns>
		public static List<DisplayInfo> GetDisplays() {
			List<DisplayInfo> displayInfoList = new List<DisplayInfo>();

			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
				delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) {
					MonitorInfoEx monitorInfoEx = new MonitorInfoEx();
					monitorInfoEx.Init();
					bool success = GetMonitorInfo(hMonitor, ref monitorInfoEx);
					if (success) {
						DisplayInfo displayInfo = new DisplayInfo();
						displayInfo.Bounds = monitorInfoEx.Monitor.ToRect();
						displayInfo.WorkArea = monitorInfoEx.WorkArea.ToRect();
						displayInfo.DeviceName = monitorInfoEx.DeviceName;
						displayInfo.Flags = (MonitorInfoFlags)monitorInfoEx.Flags;
						displayInfoList.Add(displayInfo);
					}
					return true;
				}, IntPtr.Zero);
			return displayInfoList;
		}

		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		public static bool GetWindowRect(IntPtr hWnd, out Rect rectangle) {
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result) {
				rectangle = windowInfo.rcWindow.ToRect();
			} else {
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
		public static bool GetClientRect(IntPtr hWnd, out Rect rectangle) {
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result) {
				rectangle = windowInfo.rcClient.ToRect();
			} else {
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
		public static bool GetBorderSize(IntPtr hWnd, out Size size) {
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = GetWindowInfo(hWnd, ref windowInfo);
			if (result) {
				size = new Size((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			} else {
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
		public static IntPtr GetClassLongWrapper(IntPtr hWnd, int nIndex) {
			if (IntPtr.Size > 4) {
				return GetClassLongPtr(hWnd, nIndex);
			}
			return GetClassLong(hWnd, nIndex);
		}

		/// <summary>
		/// Retrieve the windows title, also called Text
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		public static string GetText(IntPtr hWnd) {
			StringBuilder title = new StringBuilder(260, 260);
			int length = GetWindowText(hWnd, title, title.Capacity);
			if (length == 0) {
				Win32Error error = GetLastErrorCode();
				if (error != Win32Error.Success) {
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
		public static string GetClassname(IntPtr hWnd) {
			StringBuilder classNameBuilder = new StringBuilder(260, 260);
			int hresult = GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			if (hresult == 0) {
				return null;
			}
			return classNameBuilder.ToString();
		}

		/// <summary>
		/// Get the icon for a hWnd
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns>System.Drawing.Icon</returns>
		public static System.Drawing.Icon GetIcon(IntPtr hWnd) {
			IntPtr iconSmall = IntPtr.Zero;
			IntPtr iconBig = new IntPtr(1);
			IntPtr iconSmall2 = new IntPtr(2);

			IntPtr iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconSmall2, IntPtr.Zero);
			if (iconHandle == IntPtr.Zero) {
				iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconSmall, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero) {
				iconHandle = GetClassLongWrapper(hWnd, (int)ClassLongIndex.GCL_HICONSM);
			}
			if (iconHandle == IntPtr.Zero) {
				iconHandle = SendMessage(hWnd, (int)WindowsMessages.WM_GETICON, iconBig, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero) {
				iconHandle = GetClassLongWrapper(hWnd, (int)ClassLongIndex.GCL_HICON);
			}

			if (iconHandle == IntPtr.Zero) {
				return null;
			}

			return System.Drawing.Icon.FromHandle(iconHandle);
		}

	}
}
