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
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.Structs;
using System;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Dapplo.Log.Loggers;
using Dapplo.Windows.Reactive;

#endregion

namespace Dapplo.Windows.Tests
{
	public class WinEventHookTests
	{
		private static readonly LogSource Log = new LogSource();

		public WinEventHookTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		/// <summary>
		/// Test typing in a notepad
		/// </summary>
		/// <returns></returns>
		[StaFact]
		private async Task TestWinEventHook()
		{
			// This takes care of having a WinProc handler, to make sure the messages arrive
			var winProcHandler = WinProcHandler.Instance;

			// This buffers the observable
			var replaySubject = new ReplaySubject<InteropWindow>();

			var winEventObservable =  WinEventHook.WindowTileChangeObservable()
				.Select(info => InteropWindow.CreateFor(info.Handle).Fill())
				.Where(info => !string.IsNullOrEmpty(info.Caption))
				.Subscribe(windowInfo =>
			{
				Log.Debug().WriteLine("Window title change: Process ID {0} - Title: {1}", windowInfo.Handle, windowInfo.Caption);
				replaySubject.OnNext(windowInfo);
			});

			// Start a process to test against
			using (var process = Process.Start("notepad.exe"))
			{
				try
				{
					// Make sure it's started
					Assert.NotNull(process);
					// Wait until the process started it's message pump (listening for input)
					process.WaitForInputIdle();

					// Find the belonging window
					var notepadWindow = await replaySubject.Where(info => info.ProcessId == process.Id).FirstAsync();
					Assert.Equal(process.Id, notepadWindow.ProcessId);
				}
				finally
				{
					process.Kill();
					winEventObservable.Dispose();
				}
			}
		}
	}
}