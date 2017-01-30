using Dapplo.Windows.Native;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public static class WindowsEnumerator
	{
		/// <summary>
		/// Enumerate the windows / child windows via an Observable
		/// </summary>
		/// <param name="hWndParent">IntPtr with the hwnd of the parent, or null for all</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>IObservable with WinWindowInfo</returns>
		public static IObservable<WindowInfo> EnumerateWindowsAsync(IntPtr? hWndParent = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Observable.Create<WindowInfo>(observer =>
			{
				bool continueWithEnumeration = true;
				Task.Run(() =>
				{
					User32.EnumChildWindows(hWndParent ?? IntPtr.Zero, (hwnd, param) =>
					{
						// check if we should continue
						if (cancellationToken.IsCancellationRequested || !continueWithEnumeration)
						{
							return 0;
						}
						var windowInfo = WindowInfo.CreateFor(hwnd);
						observer.OnNext(windowInfo);
						return continueWithEnumeration ? 1 : 0;
					}, IntPtr.Zero);
					observer.OnCompleted();
				}, cancellationToken);
				return () =>
				{
					// Stop enumerating
					continueWithEnumeration = false;
				};
			});

		}
	}
}
