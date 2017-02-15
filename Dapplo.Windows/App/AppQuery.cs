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
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Interop;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.App
{
	/// <summary>
	///     Helper class to support with Windows Store apps
	/// </summary>
	public static class AppQuery
	{
		private const string W8AppWindowClass = "Windows.UI.Core.CoreWindow"; //Used for Windows 8(.1)
		private const string W10AppWindowClass = "ApplicationFrameWindow"; // Windows 10 uses ApplicationFrameWindow
		private const string ApplauncherClass = "ImmersiveLauncher";
		private const string GutterClass = "ImmersiveGutter";

		// the window class for the App window, this depends on the windows version and will be set in the static initializer
		private static readonly string AppWindowsClass;

		// For MonitorFromWindow
		private static readonly Guid CoClassGuidIAppVisibility = new Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514");

		// All currently known classes: "ImmersiveGutter", "Snapped Desktop", "ImmersiveBackgroundWindow","ImmersiveLauncher","Windows.UI.Core.CoreWindow","ApplicationManager_ImmersiveShellWindow","SearchPane","MetroGhostWindow","EdgeUiInputWndClass", "NativeHWNDHost", "Shell_CharmWindow"

		private static readonly IDisposableCom<IAppVisibility> AppVisibility;

		static AppQuery()
		{
			// No need to check for the IAppVisibility
			if (!Environment.OSVersion.IsWindows8OrLater())
			{
				AppVisibility = null;
				return;
			}
			AppWindowsClass = Environment.OSVersion.IsWindows8() ? W8AppWindowClass : W10AppWindowClass;
			try
			{
				var appVisibilityType = Type.GetTypeFromCLSID(CoClassGuidIAppVisibility);
				AppVisibility = DisposableCom.Create((IAppVisibility) Activator.CreateInstance(appVisibilityType));
			}
			catch
			{
				AppVisibility = null;
			}
		}

		/// <summary>
		///     Get the hWnd for the AppLauncer
		/// </summary>
		public static IntPtr AppLauncher
		{
			get
			{
				// If there is no _appVisibility COM object, there can be no AppLauncher
				if (AppVisibility == null)
				{
					return IntPtr.Zero;
				}
				return User32.FindWindow(ApplauncherClass, null);
			}
		}

		/// <summary>
		///     Return true if the app-launcher is visible
		/// </summary>
		public static bool IsLauncherVisible => AppVisibility?.ComObject.IsLauncherVisible == true;

		/// <summary>
		///     Retrieve handles of all Windows store apps
		/// </summary>
		public static IEnumerable<InteropWindow> WindowsStoreApps
		{
			get
			{
				// if the appVisibility != null we have Windows 8 or later
				if (AppVisibility == null)
				{
					yield break;
				}
				var nextHandle = User32.FindWindow(AppWindowsClass, null);
				while (nextHandle != IntPtr.Zero)
				{
					yield return InteropWindow.CreateFor(nextHandle);
					nextHandle = User32.FindWindowEx(IntPtr.Zero, nextHandle, AppWindowsClass, null);
				}
				// check for gutter
				var gutterHandle = User32.FindWindow(GutterClass, null);
				if (gutterHandle != IntPtr.Zero)
				{
					yield return InteropWindow.CreateFor(gutterHandle);
				}
			}
		}


		/// <summary>
		/// This checks if the window is an App (Win8 or Win10)
		/// </summary>
		public static bool IsApp(this InteropWindow interopWindow)
		{
			return AppWindowsClass.Equals(interopWindow.GetClassname());
		}

		/// <summary>
		/// This checks if the window is a Windows 8 App
		/// For Windows 10 most normal code works, as it's hosted inside "ApplicationFrameWindow"
		/// </summary>
		public static bool IsWin8App(this InteropWindow interopWindow)
		{
			return W8AppWindowClass.Equals(interopWindow.GetClassname());
		}

		/// <summary>
		/// This checks if the window is a Windows 10 App
		/// For Windows 10 apps are hosted inside "ApplicationFrameWindow"
		/// </summary>
		public static bool IsWin10App(this InteropWindow interopWindow)
		{
			return W10AppWindowClass.Equals(interopWindow.GetClassname());
		}

		/// <summary>
		/// Check if the window is the metro gutter (sizeable separator)
		/// </summary>
		public static bool IsGutter(this InteropWindow interopWindow)
		{
			return GutterClass.Equals(interopWindow.GetClassname());
		}

		/// <summary>
		/// Test if this window is for the App-Launcher
		/// </summary>
		public static bool IsAppLauncher(this InteropWindow interopWindow)
		{
			return ApplauncherClass.Equals(interopWindow.GetClassname());
		}

		/// <summary>
		/// Check if this window is the window of a metro app
		/// </summary>
		public static bool IsMetroApp(this InteropWindow interopWindow)
		{
			return interopWindow.IsAppLauncher() || interopWindow.IsWin8App();
		}

		/// <summary>
		/// Get the AppLauncher
		/// </summary>
		/// <returns>InteropWindow</returns>
		public static InteropWindow GetAppLauncher()
		{
			// Works only if Windows 8 (or higher)
			if (IsLauncherVisible)
			{
				return null;
			}
			IntPtr appLauncher = User32.FindWindow(ApplauncherClass, null);
			if (appLauncher != IntPtr.Zero)
			{
				return InteropWindow.CreateFor(appLauncher);
			}
			return null;
		}

		/// <summary>
		///     Check if a Windows Store App (WinRT) is visible
		/// </summary>
		/// <param name="windowBounds"></param>
		/// <returns>true if an app, covering the supplied rect, is visisble</returns>
		public static bool AppVisible(RECT windowBounds)
		{
			if (AppVisibility == null)
			{
				return true;
			}

			foreach (var screen in User32.AllDisplays())
			{
				if (screen.Bounds.Contains(windowBounds))
				{
					if (windowBounds.Equals(screen.Bounds))
					{
						// Fullscreen, it's "visible" when AppVisibilityOnMonitor says yes
						// Although it might be the other App, this is not "very" important
						RECT rect = screen.Bounds;
						var monitor = User32.MonitorFromRect(ref rect, MonitorFromRectFlags.DefaultToNearest);
						if (monitor != IntPtr.Zero)
						{
							var monitorAppVisibility = AppVisibility.ComObject.GetAppVisibilityOnMonitor(monitor);
							if (monitorAppVisibility == MonitorAppVisibility.MAV_APP_VISIBLE)
							{
								return true;
							}
						}
					}
					else
					{
						// Is only partly on the screen, when this happens the app is allways visible!
						return true;
					}
				}
			}
			return false;
		}
	}
}