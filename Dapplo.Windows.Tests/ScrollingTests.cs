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
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Tests
{
	public class ScrollingTests
	{
		private static readonly LogSource Log = new LogSource();

		public ScrollingTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[StaFact]
		private async Task TestScrolling()
		{
			using (var process = Process.Start("notepad.exe", "C:\\Windows\\setupact.log"))
			{
				Assert.NotNull(process);
				process.WaitForInputIdle();
				var notepadWindow = await WindowsEnumerator.EnumerateWindowsAsync().Where(info => info.Fill().Text.Contains("setupact.log")).FirstOrDefaultAsync();
				Assert.NotNull(notepadWindow);
				SCROLLINFO scrollInfo = new SCROLLINFO();
				scrollInfo.cbSize = (uint)Marshal.SizeOf(scrollInfo);
				scrollInfo.fMask = (uint)ScrollInfoMask.All;

				var scrollingWindow = await WindowsEnumerator.EnumerateWindowsAsync(notepadWindow.Handle).Where(childWindow => User32.GetScrollInfo(childWindow.Handle, ScrollBarDirection.Vertical, ref scrollInfo)).FirstOrDefaultAsync();
				Assert.NotNull(scrollingWindow);

				scrollingWindow.Fill();

				do
				{
					if (!User32.GetScrollInfo(scrollingWindow.Handle, ScrollBarDirection.Vertical, ref scrollInfo))
					{
						var error = Win32.GetLastErrorCode();
						Log.Error().WriteLine("Error calling GetScrollInfo : {0}", Win32.GetMessage(error, 0x0C09));
						break;
					}
					if (scrollInfo.nMax == Math.Max(scrollInfo.nPos, scrollInfo.nTrackPos) + scrollInfo.nPage - 1)
					{
						// End reached
						break;
					}
					scrollInfo.nPos = scrollInfo.nPos + (int)scrollInfo.nPage;
					if (!User32.SetScrollInfo(scrollingWindow.Handle, ScrollBarDirection.Vertical, ref scrollInfo, true))
					{
						var error = Win32.GetLastErrorCode();
						Log.Error().WriteLine("Error calling SetScrollInfo : {0}", Win32.GetMessage(error, 0x0C09));
						break;
					}
					User32.SendMessage(scrollingWindow.Handle, WindowsMessages.WM_VSCROLL, new IntPtr(4 + 0x10000 * scrollInfo.nPos), IntPtr.Zero);
					await Task.Delay(100);
				} while (true);


				process.Kill();
			}
		}

	}
}