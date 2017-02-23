//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;

#endregion

namespace Dapplo.Windows.Clipboard
{
	/// <summary>
	///     A monitor for clipboard changes
	/// </summary>
	public class ClipboardMonitor
	{
		/// <summary>
		///     The singleton of the KeyboardHook
		/// </summary>
		private static readonly Lazy<ClipboardMonitor> Singleton = new Lazy<ClipboardMonitor>(() => new ClipboardMonitor());

		/// <summary>
		///     Used to store the observable
		/// </summary>
		private readonly IObservable<EventArgs> _clipboardObservable;

		/// <summary>
		///     Private constructor to create the observable
		/// </summary>
		private ClipboardMonitor()
		{
			_clipboardObservable = Observable.Create<EventArgs>(observer =>
			{
				// This handles the message
				HwndSourceHook winProcHandler = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
				{
					var windowsMessage = (WindowsMessages)msg;
					if (windowsMessage == WindowsMessages.WM_CLIPBOARDUPDATE)
					{
						observer.OnNext(EventArgs.Empty);
					}
					return IntPtr.Zero;
				};
				WinProcHandler.Instance.AddHook(winProcHandler);
				if (!AddClipboardFormatListener(WinProcHandler.Instance.Handle))
				{
					var error = Win32.GetLastErrorCode();
					observer.OnError(new Exception(Win32.GetMessage(error)));
				}
				return Disposable.Create(() =>
				{
					RemoveClipboardFormatListener(WinProcHandler.Instance.Handle);
					WinProcHandler.Instance.RemoveHook(winProcHandler);
				});
			}).Publish().RefCount();
		}

		/// <summary>
		///     The actual clipboard hook observable
		/// </summary>
		public static IObservable<EventArgs> ClipboardUpdateEvents => Singleton.Value._clipboardObservable;

		#region Native methods

		/// <summary>
		/// Add a window as a clipboard format listener
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649033(v=vs.85).aspx">AddClipboardFormatListener function</a>
		/// </summary>
		/// <param name="hWnd">IntPtr for the window to handle the messages</param>
		/// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AddClipboardFormatListener(IntPtr hWnd);

		/// <summary>
		/// Remove a window as a clipboard format listener
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649050(v=vs.85).aspx">RemoveClipboardFormatListener function</a>
		/// </summary>
		/// <param name="hWnd">IntPtr for the window to handle the messages</param>
		/// <returns>true if it worked, false if not; call GetLastError to see what was the problem</returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

		#endregion
	}
}