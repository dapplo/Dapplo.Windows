using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.App {
	// This is used for Windows 8 to see if the App Launcher is active
	// See https://msdn.microsoft.com/en-us/library/windows/desktop/jj554119.aspx
	[ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAppVisibility {
		MonitorAppVisibility GetAppVisibilityOnMonitor(IntPtr hMonitor);
		bool IsLauncherVisible {
			get;
		}
	}
}
