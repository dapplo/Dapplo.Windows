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

using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;

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

		/// <summary>
		/// Test scrolling a window
		/// </summary>
		/// <returns></returns>
		[StaFact]
		private async Task TestScrollingAsync()
		{
			// Start a process to test against
			using (var process = Process.Start("notepad.exe", "C:\\Windows\\setupact.log"))
			{
				// Make sure it's started
				Assert.NotNull(process);
				// Wait until the process started it's message pump (listening for input)
				process.WaitForInputIdle();

				// Find the belonging window
				// TODO: Do this by process ID
				var notepadWindow = await WindowsEnumerator.EnumerateWindowsAsync().Where(info => info.Fill().Text.Contains("setupact.log")).FirstOrDefaultAsync();
				Assert.NotNull(notepadWindow);

				// Create a WindowScroller
				var scroller = await WindowScroller.CreateAsync(notepadWindow);
				Assert.NotNull(scroller);

				// Move the window to the start
				scroller.Start();
				// A delay to make the window move
				await Task.Delay(100);

				// Check if it did move to the start
				Assert.True(scroller.IsAtStart);

				// Loop
				do
				{
					// Next "page"
					scroller.Next();
					// Wait a bit, so the 
					await Task.Delay(100);
					// Loop as long as we are not at the end yet
				} while (!scroller.IsAtEnd);

				// Kill the process
				process.Kill();
			}
		}
	}
}