using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Dapplo.Windows.App;
using Dapplo.Windows.Enums;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Query for native windows
	/// </summary>
	public static class WindowQuery
	{
		/// <summary>
		/// Window classes which can be ignored
		/// </summary>
		public static IEnumerable<string> IgnoreClasses { get; } =  new List<string>(new[] { "Progman", "Button", "Dwm" }); //"MS-SDIa"

		/// <summary>
		/// Check if the window is a top level
		/// </summary>
		/// <param name="window">WindowDetails</param>
		/// <returns>bool</returns>
		public static bool IsTopLevel(this WindowInfo window)
		{
			if (window.HasClassname && IgnoreClasses.Contains(window.Classname))
			{
				return false;
			}
			// Ignore windows without title
			if (window.Text.Length == 0)
			{
				return false;
			}
			// Windows without size
			if (window.Bounds.Size.IsEmpty)
			{
				return false;
			}
			if (window.HasParent)
			{
				return false;
			}
			var exWindowStyle = window.GetExtendedWindowStyle();
			if ((exWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
			{
				return false;
			}
			// Skip everything which is not rendered "normally"
			if (!window.IsWin8App() && (exWindowStyle & ExtendedWindowStyleFlags.WS_EX_NOREDIRECTIONBITMAP) != 0)
			{
				return false;
			}
			// Skip preview windows, like the one from Firefox
			if ((window.GetWindowStyle() & WindowStyleFlags.WS_VISIBLE) == 0)
			{
				return false;
			}
			return window.IsVisible || !window.IsMinimized;
		}

		/// <summary>
		/// Get all the top level windows
		/// </summary>
		/// <returns>List WindowDetails with all the top level windows</returns>
		public static IEnumerable<WindowInfo> GetTopLevelWindows(CancellationToken cancellationToken = default(CancellationToken))
		{
			foreach (var possibleTopLevelHwnd in AppQuery.WindowsStoreApps)
			{
				var possibleTopLevel = WindowInfo.CreateFor(possibleTopLevelHwnd);
				if (possibleTopLevel.IsTopLevel())
				{
					yield return possibleTopLevel;
				}
			}


			//foreach (var possibleTopLevel in await WindowsEnumerator.Enumerate().ToList().ToTask(cancellationToken))
			//{
			//	if (IsTopLevel(possibleTopLevel))
			//	{
			//		yield return possibleTopLevel;
			//	}
			//}
		}
	}
}
