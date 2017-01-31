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
using System.Windows;
using System.Windows.Media;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;
using Microsoft.Win32;

#endregion

namespace Dapplo.Windows.Native
{
	/// <summary>
	///     Dwm Utils class
	/// </summary>
	public class Dwm
	{
		private const uint DWM_EC_DISABLECOMPOSITION = 0;
		private const uint DWM_EC_ENABLECOMPOSITION = 1;

		// Key to ColorizationColor for DWM
		private const string ColorizationColorKey = @"SOFTWARE\Microsoft\Windows\DWM";

		/// <summary>
		///     Return the AERO Color
		/// </summary>
		public static Color ColorizationColor
		{
			get
			{
				using (var key = Registry.CurrentUser.OpenSubKey(ColorizationColorKey, false))
				{
					if (key != null)
					{
						var dwordValue = key.GetValue("ColorizationColor");
						if (dwordValue != null)
						{
							// TODO: Convert
							//return Color.FromArgb(Int32)dwordValue);
						}
					}
				}
				return Colors.White;
			}
		}

		/// <summary>
		///     Helper method for an easy DWM check
		/// </summary>
		/// <returns>bool true if DWM is available AND active</returns>
		public static bool IsDwmEnabled
		{
			get
			{
				// According to: http://technet.microsoft.com/en-us/subscriptions/aa969538%28v=vs.85%29.aspx
				// And: http://msdn.microsoft.com/en-us/library/windows/desktop/aa969510%28v=vs.85%29.aspx
				// DMW is always enabled on Windows 8! So return true and save a check! ;-)
				if ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor == 2))
				{
					return true;
				}
				if (Environment.OSVersion.Version.Major >= 6)
				{
					bool dwmEnabled;
					DwmIsCompositionEnabled(out dwmEnabled);
					return dwmEnabled;
				}
				return false;
			}
		}

		public static void DisableComposition()
		{
			DwmEnableComposition(DWM_EC_DISABLECOMPOSITION);
		}

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DwmBlurBehind blurBehind);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern uint DwmEnableComposition(uint uCompositionAction);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT lpRect, int size);

		// Deprecated as of Windows 8 Release Preview
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out SIZE size);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmUnregisterThumbnail(IntPtr thumb);

		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DwmThumbnailProperties props);

		public static void EnableComposition()
		{
			DwmEnableComposition(DWM_EC_ENABLECOMPOSITION);
		}

		/// <summary>
		///     Helper method to get the window size for DWM Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		public static bool GetExtendedFrameBounds(IntPtr hWnd, out Rect rectangle)
		{
			RECT rect;
			var result = DwmGetWindowAttribute(hWnd, (int) DwmWindowAttributes.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
			if (result >= 0)
			{
				rectangle = rect.ToRect();
				return true;
			}
			rectangle = Rect.Empty;
			return false;
		}
	}
}