// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.SystemState;
using Dapplo.Windows.SystemState.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the SystemState package: PowerManagementApi, SystemStateApi, and WaitableTimer.
/// </summary>
public class SystemStateTests
{
    private static readonly LogSource Log = new LogSource();

    public SystemStateTests(ITestOutputHelper testOutputHelper)
    {
        LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
    }

    /// <summary>
    /// Test that a SleepBlocker for the system can be created and disposed, also on another thread.
    /// </summary>
    [Fact]
    public async Task Test_PreventSystemSleep_DisposeOnOtherThread()
    {
        var sleepBlocker = SystemStateApi.PreventSystemSleep("Dapplo.Windows.Tests");
        Assert.True(sleepBlocker.IsActive);
        Assert.False(sleepBlocker.KeepsDisplayOn);
        Assert.Equal("Dapplo.Windows.Tests", sleepBlocker.Reason);

        // A power request is not bound to a thread
        await Task.Run(() => sleepBlocker.Dispose(), TestContext.Current.CancellationToken);
        Assert.False(sleepBlocker.IsActive);
        // Disposing twice is harmless
        sleepBlocker.Dispose();
    }

    /// <summary>
    /// Test that a SleepBlocker which also keeps the display on can be created and disposed.
    /// </summary>
    [Fact]
    public void Test_PreventSleep_KeepsDisplayOn()
    {
        using var sleepBlocker = SystemStateApi.PreventSleep();
        Assert.True(sleepBlocker.IsActive);
        Assert.True(sleepBlocker.KeepsDisplayOn);
        Assert.False(string.IsNullOrEmpty(sleepBlocker.Reason));
    }

    /// <summary>
    /// Test that a WaitableTimer can be created and disposed.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_CreateAndDispose()
    {
        using var timer = new WaitableTimer();
        Assert.True(timer.IsValid, "WaitableTimer handle should be valid.");
    }

    /// <summary>
    /// Test that a named WaitableTimer can be created.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_CreateNamedTimer()
    {
        var timerName = "DapploTestTimer_" + Guid.NewGuid().ToString("N");
        using var timer = new WaitableTimer(timerName);
        Assert.True(timer.IsValid, "Named WaitableTimer handle should be valid.");
    }

    /// <summary>
    /// Test that a WaitableTimer fires after a short delay.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_FiresAfterDelay()
    {
        using var timer = new WaitableTimer();
        var delay = TimeSpan.FromMilliseconds(100);
        var wasSet = timer.SetOnce(delay, wakeSystem: false);
        Assert.True(wasSet, "SetOnce should succeed.");

        // Wait for the timer to fire (with a generous timeout)
        var signaled = timer.Wait(TimeSpan.FromSeconds(5));
        Assert.True(signaled, "WaitableTimer should fire within 5 seconds.");
    }

    /// <summary>
    /// Test that a WaitableTimer can be cancelled.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_Cancel()
    {
        using var timer = new WaitableTimer();
        var wasSet = timer.SetOnce(TimeSpan.FromHours(1), wakeSystem: false);
        Assert.True(wasSet, "SetOnce should succeed.");

        var wasCancelled = timer.Cancel();
        Assert.True(wasCancelled, "Cancel should succeed.");
    }

    /// <summary>
    /// Test that WaitableTimer throws ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_ThrowsAfterDispose()
    {
        var timer = new WaitableTimer();
        timer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => timer.SetOnce(TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Test that PowerBroadcastEvent enum has expected values.
    /// </summary>
    [Fact]
    public void Test_PowerBroadcastEvent_EnumValues()
    {
        Assert.Equal(0x0004u, (uint)PowerBroadcastEvent.PBT_APMSUSPEND);
        Assert.Equal(0x0007u, (uint)PowerBroadcastEvent.PBT_APMRESUMESUSPEND);
        Assert.Equal(0x000Au, (uint)PowerBroadcastEvent.PBT_APMPOWERSTATUSCHANGE);
        Assert.Equal(0x0012u, (uint)PowerBroadcastEvent.PBT_APMRESUMEAUTOMATIC);
    }

    /// <summary>
    /// Test that ThreadExecutionStateFlags enum has expected values.
    /// </summary>
    [Fact]
    public void Test_ThreadExecutionStateFlags_EnumValues()
    {
        Assert.Equal(0x00000001u, (uint)ThreadExecutionStateFlags.ES_SYSTEM_REQUIRED);
        Assert.Equal(0x00000002u, (uint)ThreadExecutionStateFlags.ES_DISPLAY_REQUIRED);
        Assert.Equal(0x80000000u, (uint)ThreadExecutionStateFlags.ES_CONTINUOUS);
    }

    /// <summary>
    /// Test that ExitWindowsFlags enum has expected values.
    /// </summary>
    [Fact]
    public void Test_ExitWindowsFlags_EnumValues()
    {
        Assert.Equal(0x00000000u, (uint)ExitWindowsFlags.EWX_LOGOFF);
        Assert.Equal(0x00000001u, (uint)ExitWindowsFlags.EWX_SHUTDOWN);
        Assert.Equal(0x00000002u, (uint)ExitWindowsFlags.EWX_REBOOT);
        Assert.Equal(0x00000008u, (uint)ExitWindowsFlags.EWX_POWEROFF);
    }

    /// <summary>
    /// Test that SetWaitableTimer can be used for a future UTC time.
    /// </summary>
    [Fact]
    public void Test_WaitableTimer_SetAtFutureTime()
    {
        using var timer = new WaitableTimer();
        var futureTime = DateTimeOffset.UtcNow.AddMilliseconds(100);
        var wasSet = timer.SetAt(futureTime, wakeSystem: false);
        Assert.True(wasSet, "SetAt should succeed for a future time.");

        var signaled = timer.Wait(TimeSpan.FromSeconds(5));
        Assert.True(signaled, "WaitableTimer should fire within 5 seconds.");
    }
}
