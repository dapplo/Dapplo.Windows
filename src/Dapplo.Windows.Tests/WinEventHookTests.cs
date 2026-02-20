// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Xunit;

namespace Dapplo.Windows.Tests;

public class WinEventHookTests
{
    private static readonly LogSource Log = new LogSource();

    public WinEventHookTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test typing in a notepad
    /// </summary>
    /// <returns></returns>
    [StaFact]
    private async Task TestWinEventHook()
    {
        // This buffers the observable
        var replaySubject = new ReplaySubject<IInteropWindow>();

        var winEventObservable = WinEventHook.WindowTitleChangeObservable()
            .Select(info => InteropWindowFactory.CreateFor(info.Handle).Fill())
            .Where(interopWindow => !string.IsNullOrEmpty(interopWindow?.Caption))
            .Subscribe(interopWindow =>
            {
                Log.Debug().WriteLine("Window title change: Process ID {0} - Title: {1}", interopWindow.Handle, interopWindow.Caption);
                replaySubject.OnNext(interopWindow);
            }, exception => Log.Error().WriteLine("An error occured", exception));
        await Task.Delay(100);
        // Start a process to test against
        using (var process = Process.Start("charmap.exe"))
        {
            try
            {
                // Make sure it's started
                Assert.NotNull(process);

                // Wait until the process started it's message pump (listening for input)
                if (!process.WaitForInputIdle(2000))
                {
                    Assert.Fail("Process wasn't ready for input.");
                    return;
                }
                User32Api.SetWindowText(process.MainWindowHandle, "TestWinEventHook - Test");

                // Find the belonging window
                var testWindow = await replaySubject.Where(info => info != null && info.ProcessId == process.Id).FirstAsync();
                Assert.Equal(process.Id, testWindow?.ProcessId);
            }
            finally
            {
                process?.Kill();
                winEventObservable.Dispose();
            }
        }
    }
}