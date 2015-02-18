using Dapplo.Windows.Native;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Windows.Desktop {
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
