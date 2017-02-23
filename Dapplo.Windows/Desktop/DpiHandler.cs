using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// This handles DPI changes
	/// see u.a. <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
	/// </summary>
	public class DpiHandler : IDisposable
	{
		private const double UserDefaultScreenDpi = 96d;

		/// <summary>
		/// This is that which handles the windows messages, and needs to be disposed
		/// </summary>
		internal IDisposable MessageHandler { get; set; }

		/// <summary>
		/// Retrieve the current DPI for the window
		/// </summary>
		public double Dpi { get; private set; }

		/// <summary>
		/// The action to be called, for when a change occurs.
		/// This is already set with a default handler, depending on the type of Window which this handler handles.
		/// </summary>
		/// <param name="value">Action with double for the DPI and an doubke with the factor for the scaling (1.0 = 100% = 96 dpi)</param>
		public Action<double, double> OnDpiChangedAction { get; set; }

		/// <summary>
		/// Message handler of the Per_Monitor_DPI_Aware window.
		/// The handles the WM_DPICHANGED message and adjusts window size, graphics and text based on the DPI of the monitor.
		/// The window message provides the new window size (lparam) and new DPI (wparam)
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn312083(v=vs.85).aspx">WM_DPICHANGED message</a>
		/// </summary>
		/// <param name="hwnd">IntPtr with the hWnd</param>
		/// <param name="msg">The Windows message</param>
		/// <param name="wParam">IntPtr</param>
		/// <param name="lParam">IntPtr</param>
		/// <param name="handled">ref bool</param>
		/// <returns>IntPtr</returns>
		internal IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			var windowsMessage = (WindowsMessages) msg;
			switch (windowsMessage)
			{
				// Handle the WM_CREATE, this is where we can get the DPI via system calls
				case WindowsMessages.WM_CREATE:
					if (Environment.OSVersion.IsWindows81OrLater())
					{
						var hMonitor = User32.MonitorFromWindow(hwnd, MonitorFromFlags.DefaultToNearest);
						uint dpiX;
						uint dpiY;
						if (GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out dpiX, out dpiY))
						{
							if (IsEqual(Dpi, dpiX))
							{
								Dpi = dpiX;
								var scalePercentage = Dpi / UserDefaultScreenDpi;
								OnDpiChangedAction?.Invoke(Dpi, scalePercentage);
							}
						}
					}
					break;
				// Handle the DPI change message, this is where it's supplied
				case WindowsMessages.WM_DPICHANGED:
					// Retrieve the adviced location
					RECT lprNewRect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
					// Move the window to it's location, and resize
					User32.SetWindowPos(hwnd,
						IntPtr.Zero,
						lprNewRect.Left,
						lprNewRect.Top,
						lprNewRect.Width,
						lprNewRect.Height,
						WindowPos.SWP_NOZORDER | WindowPos.SWP_NOOWNERZORDER | WindowPos.SWP_NOACTIVATE);
					// Check if the DPI was changed, if so call the action (if any)
					var newDpi = wParam.ToInt32() & 0xFFFF;
					if (IsEqual(Dpi,newDpi))
					{
						Dpi = newDpi;
						var scalePercentage = Dpi / UserDefaultScreenDpi;
						OnDpiChangedAction?.Invoke(Dpi, scalePercentage);
					}
					// specify that the message was handled
					handled = true;
					break;
			}

			return IntPtr.Zero;
		}

		/// <summary>
		/// Compare helper for doubles
		/// </summary>
		/// <param name="d1">double</param>
		/// <param name="d2">double</param>
		/// <param name="precisionFactor">uint</param>
		/// <returns>bool</returns>
		private static bool IsEqual(double d1, double d2, uint precisionFactor = 2)
		{
			return Math.Abs(d1 - d2) < precisionFactor * double.Epsilon;
		}

		#region DllImports
		[DllImport("shcore")]
		private static extern int SetProcessDpiAwareness(DpiAwareness value);

		[DllImport("shcore")]
		private static extern int GetProcessDpiAwareness(IntPtr processHandle, out DpiAwareness value);

		[DllImport("shcore")]
		private static extern int GetDpiForWindow(IntPtr hWnd);

		[DllImport("shcore")]
		private static extern int GetDpiForSystem();

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx">GetDpiForMonitor function</a>
		/// Queries the dots per inch (dpi) of a display.
		/// </summary>
		/// <param name="hMonitor">IntPtr</param>
		/// <param name="dpiType">MonitorDpiType</param>
		/// <param name="dpiX">out int for the horizontal dpi</param>
		/// <param name="dpiY">out int for the vertical dpi</param>
		/// <returns>true if all okay</returns>
		[DllImport("shcore")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

		[DllImport("shcore")]
		private static extern DpiAwarenessContext GetThreadDpiAwarenessContext();

		[DllImport("shcore")]
		private static extern DpiAwarenessContext SetThreadDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

		[DllImport("shcore")]
		private static extern DpiAwareness GetAwarenessFromDpiAwarenessContext(DpiAwarenessContext dpiAwarenessContext);

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748621(v=vs.85).aspx">EnableNonClientDpiScaling function</a>
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <returns>bool</returns>
		[DllImport("shcore")]
		private static extern bool EnableNonClientDpiScaling(IntPtr hWnd);

		/// <inheritdoc />
		public void Dispose()
		{
			MessageHandler?.Dispose();
			MessageHandler = null;
		}

		#endregion
	}
}
