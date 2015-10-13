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

using Dapplo.Windows.Interop;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace Dapplo.Windows.App {
	/// <summary>
	/// Helper class to support with Windows Store apps
	/// </summary>
	public static class AppQuery {
		public const string ModernuiWindowsClass = "Windows.UI.Core.CoreWindow";
		public const string ModernuiApplauncherClass = "ImmersiveLauncher";
		public const string ModernuiGutterClass = "ImmersiveGutter";
		private static Guid CoClassGuidIAppVisibility = new Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514");

		// All currently known classes: "ImmersiveGutter", "Snapped Desktop", "ImmersiveBackgroundWindow","ImmersiveLauncher","Windows.UI.Core.CoreWindow","ApplicationManager_ImmersiveShellWindow","SearchPane","MetroGhostWindow","EdgeUiInputWndClass", "NativeHWNDHost", "Shell_CharmWindow"

		private static readonly IDisposableCom<IAppVisibility> _appVisibility;
		// For MonitorFromWindow
		public const int MONITOR_DEFAULTTONULL = 0;
		public const int MONITOR_DEFAULTTOPRIMARY = 1;
		public const int MONITOR_DEFAULTTONEAREST = 2;

		static AppQuery() {
			try {
				Type appVisibilityType = Type.GetTypeFromCLSID(new Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514"));
				_appVisibility = DisposableCom.Create((IAppVisibility)Activator.CreateInstance(appVisibilityType));
			} catch {
				_appVisibility = null;
			}
		}

		/// <summary>
		/// Return true if the app-launcher is visible
		/// </summary>
		public static bool IsLauncherVisible {
			get {
				if (_appVisibility != null) {
					return _appVisibility.ComObject.IsLauncherVisible;
				}
				return false;
			}
		}

		/// <summary>
		/// Get the hWnd for the AppLauncer
		/// </summary>
		public static IntPtr AppLauncher {
			get {
				// If there is no _appVisibility COM object, there can be no AppLauncher
				if (_appVisibility == null) {
					return IntPtr.Zero;
				}
				return User32.FindWindow(ModernuiApplauncherClass, null);
			}
		}

		/// <summary>
		/// Check if a Windows Store App (WinRT) is visible
		/// </summary>
		/// <param name="windowBounds"></param>
		/// <returns>true if an app, covering the supplied rect, is visisble</returns>
		public static bool AppVisible(Rect windowBounds) {
			if (_appVisibility == null) {
				return true;
			}

			foreach (var screen in User32.AllDisplays()) {
				if (screen.Bounds.Contains(windowBounds)) {
					if (windowBounds.Equals(screen.Bounds)) {
						// Fullscreen, it's "visible" when AppVisibilityOnMonitor says yes
						// Although it might be the other App, this is not "very" important
						RECT rect = new RECT(screen.Bounds);
						IntPtr monitor = User32.MonitorFromRect(ref rect, MONITOR_DEFAULTTONULL);
						if (monitor != IntPtr.Zero) {
							var monitorAppVisibility = _appVisibility.ComObject.GetAppVisibilityOnMonitor(monitor);
							if (monitorAppVisibility == MonitorAppVisibility.MAV_APP_VISIBLE) {
								return true;
							}
						}
					} else {
						// Is only partly on the screen, when this happens the app is allways visible!
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Retrieve all Windows Store apps
		/// </summary>
		public static IEnumerable<IntPtr> WindowsStoreApps {
			get {
				// if the appVisibility != null we have Windows 8.
				if (_appVisibility == null) {
					yield break;
				}
				IntPtr nextHandle = User32.FindWindow(ModernuiWindowsClass, null);
				while (nextHandle != IntPtr.Zero) {
					yield return nextHandle;
					nextHandle = User32.FindWindowEx(IntPtr.Zero, nextHandle, ModernuiWindowsClass, null);
				}
				// check for gutter
				IntPtr gutterHandle = User32.FindWindow(ModernuiGutterClass, null);
				if (gutterHandle != IntPtr.Zero) {
					yield return gutterHandle;
				}
			}
		}
	}
}
