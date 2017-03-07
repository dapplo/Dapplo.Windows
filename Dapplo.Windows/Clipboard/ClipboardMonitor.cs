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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
		private readonly IObservable<ClipboardUpdateEventArgs> _clipboardObservable;

		/// <summary>
		///     Private constructor to create the observable
		/// </summary>
		private ClipboardMonitor()
		{
			_clipboardObservable = Observable.Create<ClipboardUpdateEventArgs>(observer =>
			{
				// This handles the message
				HwndSourceHook winProcHandler = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
				{
					var windowsMessage = (WindowsMessages)msg;
					if (windowsMessage == WindowsMessages.WM_CLIPBOARDUPDATE)
					{
						try
						{
							OpenClipboard(hwnd);
							var clipboardEvent = new ClipboardUpdateEventArgs(EnumClipboardFormats());
							observer.OnNext(clipboardEvent);
						}
						finally
						{
							CloseClipboard();
						}
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
		/// Enumerate through all formats on the clipboard 
		/// </summary>
		/// <returns>IEnumerable</returns>
		private IEnumerable<string> EnumClipboardFormats()
		{
			uint clipboardId = 0;
			var clipboardFormatName = new StringBuilder(256);
			do
			{
				clipboardId = EnumClipboardFormats(clipboardId);
				if (clipboardId != 0)
				{
					if (Enum.IsDefined(typeof(StandardClipboardFormats), clipboardId))
					{
						var clipboardFormat = (StandardClipboardFormats) clipboardId;
						yield return clipboardFormat.ToString();
					}
					else
					{
						clipboardFormatName.Length = 0;
						GetClipboardFormatName(clipboardId, clipboardFormatName, clipboardFormatName.Capacity);
						yield return clipboardFormatName.ToString();
					}
				}
			} while (clipboardId != 0);
		}

		/// <summary>
		///     The actual clipboard hook observable
		/// </summary>
		public static IObservable<ClipboardUpdateEventArgs> ClipboardUpdateEvents => Singleton.Value._clipboardObservable;

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

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649038(v=vs.85).aspx">EnumClipboardFormats function</a>
		/// Enumerates the data formats currently available on the clipboard.
		/// Clipboard data formats are stored in an ordered list. To perform an enumeration of clipboard data formats, you make a series of calls to the EnumClipboardFormats function. For each call, the format parameter specifies an available clipboard format, and the function returns the next available clipboard format.
	    /// </summary>
		/// <param name="format">To start an enumeration of clipboard formats, set format to zero. When format is zero, the function retrieves the first available clipboard format. For subsequent calls during an enumeration, set format to the result of the previous EnumClipboardFormats call.</param>
		/// <returns>uint with clipboard format id</returns>
		[DllImport("user32")]
		private static extern uint EnumClipboardFormats(uint format);

		/// <summary>
		/// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
		/// Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
		/// </summary>
		/// <param name="hWndNewOwner">IntPtr with the hWnd of the new owner</param>
		/// <returns>true if the clipboard is opened</returns>
		[DllImport("user32", SetLastError = true)]
		private static extern bool OpenClipboard(IntPtr hWndNewOwner);

		/// <summary>
		/// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649048(v=vs.85).aspx"></a>
		/// Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
		/// </summary>
		/// <returns>true if the clipboard is closed</returns>
		[DllImport("user32", SetLastError = true)]
		private static extern bool CloseClipboard();

		/// <summary>
		/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx">GetClipboardFormatName function</a>
		/// Retrieves from the clipboard the name of the specified registered format.
		/// The function copies the name to the specified buffer.
		/// </summary>
		/// <param name="format">uint with the id of the format</param>
		/// <param name="lpszFormatName">Name of the format</param>
		/// <param name="cchMaxCount">Maximum size of the output</param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

		#endregion
	}
}