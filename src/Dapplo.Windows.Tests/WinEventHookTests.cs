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

    private readonly ITestOutputHelper _testOutputHelper;

    public WinEventHookTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    ///     Test typing in a notepad
    /// </summary>
    /// <returns></returns>
    [StaFact]
    [Trait("Category", "Interactive")]
    public async Task TestWinEventHook()
    {
        // This buffers the observable
        var replaySubject = new ReplaySubject<IInteropWindow>();
        // The events arrive on the thread of the SharedMessageWindow.
        // Don't use the (global) Dapplo.Log default logger in the subscriber: when test classes run in parallel it can belong to another,
        // already finished, test and throw, which ends the subscription. Write to this test's output instead.
        var winEventObservable = WinEventHook.WindowTitleChangeObservable()
            .Select(info => InteropWindowFactory.CreateFor(info.Handle).Fill())
            .Where(interopWindow => !string.IsNullOrEmpty(interopWindow?.Caption))
            .Subscribe(interopWindow =>
            {
                _testOutputHelper.WriteLine($"Window title change: Handle {interopWindow.Handle} - Title: {interopWindow.Caption}");
                replaySubject.OnNext(interopWindow);
            }, exception => replaySubject.OnError(exception));
        await Task.Delay(100);
        // Start a process to test against
        using (var process = Process.Start("charmap.exe"))
        {
            try
            {
                // Make sure it's started
                Assert.NotNull(process);

                // Wait until the process started it's message pump (listening for input)
                Assert.True(process.WaitForInputIdle(5000), "Process wasn't ready for input.");
                // The MainWindowHandle can still be 0 directly after the start
                await TestWait.UntilAsync(() =>
                {
                    process.Refresh();
                    return process.MainWindowHandle != IntPtr.Zero;
                }, "The main window of charmap wasn't created");
                User32Api.SetWindowText(process.MainWindowHandle, "TestWinEventHook - Test");

                // Find the belonging window
                // Fail instead of hanging the test run when the event doesn't arrive
                var testWindow = await replaySubject.Where(info => info != null && info.ProcessId == process.Id).FirstAsync().Timeout(TimeSpan.FromSeconds(20));
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