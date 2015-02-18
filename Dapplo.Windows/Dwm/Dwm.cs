using Dapplo.Windows.Native;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Dapplo.Windows.Dwm {
	/// <summary>
	/// Dwm Utils class
	/// </summary>
	public class Dwm {
		private const uint DWM_EC_DISABLECOMPOSITION = 0;
		private const uint DWM_EC_ENABLECOMPOSITION = 1;

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmUnregisterThumbnail(IntPtr thumb);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out SIZE size);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

		// Deprecated as of Windows 8 Release Preview
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmIsCompositionEnabled(out bool enabled);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT lpRect, int size);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern uint DwmEnableComposition(uint uCompositionAction);

		public static void EnableComposition() {
			DwmEnableComposition(DWM_EC_ENABLECOMPOSITION);
		}
		public static void DisableComposition() {
			DwmEnableComposition(DWM_EC_DISABLECOMPOSITION);
		}

		// Key to ColorizationColor for DWM
		private const string ColorizationColorKey = @"SOFTWARE\Microsoft\Windows\DWM";

		/// <summary>
		/// Helper method for an easy DWM check
		/// </summary>
		/// <returns>bool true if DWM is available AND active</returns>
		public static bool IsDwmEnabled() {
			// According to: http://technet.microsoft.com/en-us/subscriptions/aa969538%28v=vs.85%29.aspx
			// And: http://msdn.microsoft.com/en-us/library/windows/desktop/aa969510%28v=vs.85%29.aspx
			// DMW is always enabled on Windows 8! So return true and save a check! ;-)
			if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 2) {
				return true;
			}
			if (Environment.OSVersion.Version.Major >= 6) {
				bool dwmEnabled;
				DwmIsCompositionEnabled(out dwmEnabled);
				return dwmEnabled;
			}
			return false;
		}

		/// <summary>
		/// Return the AERO Color
		/// </summary>
		public static Color ColorizationColor {
			get {
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ColorizationColorKey, false)) {
					if (key != null) {
						object dwordValue = key.GetValue("ColorizationColor");
						if (dwordValue != null) {
							// TODO: Convert
							//return Color.FromArgb(Int32)dwordValue);
						}
					}
				}
				return Colors.White;
			}
		}

		/// <summary>
		/// Helper method to get the window size for DWM Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetExtendedFrameBounds(IntPtr hWnd, out Rect rectangle) {
			RECT rect;
			int result = DwmGetWindowAttribute(hWnd, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
			if (result >= 0) {
				rectangle = rect.ToRect();
				return true;
			}
			rectangle = Rect.Empty;
			return false;
		}
	}
}
