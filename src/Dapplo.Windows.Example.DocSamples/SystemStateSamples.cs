// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Windows.SystemState;
using Dapplo.Windows.SystemState.Enums;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/system-state.md, src/Dapplo.Windows.SystemState/README.md, wiki/System-State.md
/// </summary>
public static class SystemStateSamples
{
    public static void SleepAndHibernate()
    {
        #region SleepAndHibernate
        // Sleep (suspend to RAM), applications get PBT_APMSUSPEND first
        bool ok = PowerManagementApi.Sleep();

        // Hibernate (suspend to disk)
        ok = PowerManagementApi.Hibernate();

        // Sleep, and don't let wake timers wake the system
        ok = PowerManagementApi.Sleep(disableWakeEvent: true);
        #endregion
    }

    public static void ShutdownRestartLogOff()
    {
        #region ShutdownRestartLogOff
        // Log off the current user
        PowerManagementApi.LogOff();

        // Power off; applications which don't respond are terminated after a timeout
        if (!PowerManagementApi.Shutdown())
        {
            // e.g. 1314 (ERROR_PRIVILEGE_NOT_HELD) when the user may not shut down
            Console.WriteLine($"Shutdown failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
        }

        // Restart
        PowerManagementApi.Restart();

        // Close all applications without asking them, they can lose data
        PowerManagementApi.Shutdown(force: true);
        #endregion
    }

    public static void ExitWindowsExDirect()
    {
        #region ExitWindowsExDirect
        // ExitWindowsEx needs the shutdown privilege, which is disabled by default
        if (PowerManagementApi.EnableShutdownPrivilege())
        {
            PowerManagementApi.ExitWindowsEx(ExitWindowsFlags.EWX_REBOOT | ExitWindowsFlags.EWX_RESTARTAPPS | ExitWindowsFlags.EWX_FORCEIFHUNG,
                PowerManagementApi.ShutdownReasonPlannedOther);
        }
        #endregion
    }

    public static void LockWorkStation()
    {
        #region LockWorkStation
        // The same as Win+L. true means the lock was started, the screen might not be locked yet.
        bool started = PowerManagementApi.LockWorkStation();
        #endregion
    }

    public static async Task PreventSleep()
    {
        #region PreventSleep
        // Keep the system and the display on until the blocker is disposed
        using (SystemStateApi.PreventSleep("Recording the screen"))
        {
            await RecordAsync();
        }

        // Keep only the system awake, the display may turn off
        using (SystemStateApi.PreventSystemSleep("Uploading files"))
        {
            await UploadAsync();
        }
        #endregion
    }

    public static void PreventSleepLongRunning()
    {
        #region PreventSleepLongRunning
        // A SleepBlocker is not bound to a thread, you can keep it in a field and dispose it anywhere
        SleepBlocker blocker = SystemStateApi.PreventSystemSleep("Download in progress");
        Console.WriteLine($"Blocking sleep: {blocker.Reason}, display kept on: {blocker.KeepsDisplayOn}");
        // ... later, e.g. in a completion callback on another thread
        blocker.Dispose();
        #endregion
    }

    public static void TimerOnce()
    {
        #region TimerOnce
        using var timer = new WaitableTimer();

        // Fire once, in 30 seconds
        timer.SetOnce(TimeSpan.FromSeconds(30));

        // Block this thread until the timer fires, but wait at most 60 seconds
        bool signaled = timer.Wait(TimeSpan.FromSeconds(60));
        Console.WriteLine(signaled ? "Timer fired" : "Timed out");
        #endregion
    }

    public static void TimerAt()
    {
        #region TimerAt
        using var timer = new WaitableTimer();

        // Fire at a point in time, the offset of the DateTimeOffset is taken into account
        timer.SetAt(DateTimeOffset.Now.AddHours(2));
        timer.Wait(TimeSpan.FromHours(3));
        #endregion
    }

    public static void TimerPeriodic()
    {
        #region TimerPeriodic
        using var timer = new WaitableTimer();

        // First after 1 second, then every 5000 milliseconds
        timer.SetPeriodic(initialDelay: TimeSpan.FromSeconds(1), period: 5000);

        for (var i = 0; i < 5; i++)
        {
            timer.Wait(TimeSpan.FromSeconds(10));
            Console.WriteLine($"Tick {i + 1}");
        }

        timer.Cancel();
        #endregion
    }

    public static void TimerWakeSystem()
    {
        #region TimerWakeSystem
        using var timer = new WaitableTimer();

        // Wake the PC from sleep or hibernation in 2 hours.
        // No privilege is needed, but the "Allow wake timers" power setting must be enabled.
        if (!timer.SetOnce(TimeSpan.FromHours(2), wakeSystem: true))
        {
            Console.WriteLine("Could not set the timer");
            return;
        }

        // After the wake-up Windows broadcasts PBT_APMRESUMEAUTOMATIC, and the wait returns
        timer.Wait(TimeSpan.FromHours(3));
        #endregion
    }

    public static void TimerWaitHandle()
    {
        #region TimerWaitHandle
        var timer = new WaitableTimer();
        timer.SetOnce(TimeSpan.FromMinutes(5));

        // Don't block a thread: let the thread pool call you when the timer fires
        System.Threading.ThreadPool.RegisterWaitForSingleObject(timer.WaitHandle, (state, timedOut) =>
        {
            Console.WriteLine("Five minutes are over");
            timer.Dispose();
        }, null, System.Threading.Timeout.Infinite, executeOnlyOnce: true);
        #endregion
    }

    public static void TimerNamed()
    {
        #region TimerNamed
        // Process A creates the timer ...
        using var timerA = new WaitableTimer("MyApp_WakeTimer");
        timerA.SetOnce(TimeSpan.FromMinutes(30), wakeSystem: true);

        // ... process B opens the same timer by its name and waits for it
        using var timerB = new WaitableTimer("MyApp_WakeTimer");
        bool fired = timerB.Wait(TimeSpan.FromHours(1));
        #endregion
    }

    public static void SuspendResume()
    {
        #region SuspendResume
        // Called on the SharedMessageWindow thread while Windows waits (about 2 seconds): save your state synchronously
        var suspendSubscription = PowerBroadcastListener.Suspending
            .Subscribe(_ => SaveState());

        // The user is back (opened the lid, pressed a key); this is the moment to reconnect
        var resumeSubscription = PowerBroadcastListener.ResumedFromSuspend
            .Subscribe(_ => Reconnect());

        // Stop listening
        suspendSubscription.Dispose();
        resumeSubscription.Dispose();
        #endregion
    }

    public static void ResumedAutomatically()
    {
        #region ResumedAutomatically
        // Fires after every wake-up, e.g. by a wake timer. Don't show UI here, maybe nobody is in front of the PC.
        // When a user is present, ResumedFromSuspend follows.
        var subscription = PowerBroadcastListener.ResumedAutomatically
            .Subscribe(_ => RunScheduledWork());
        #endregion
    }

    public static void AllPowerEvents()
    {
        #region AllPowerEvents
        var subscription = PowerBroadcastListener.PowerEvents
            .Subscribe(powerEvent =>
            {
                switch (powerEvent)
                {
                    case PowerBroadcastEvent.PBT_APMSUSPEND:
                        Console.WriteLine("Suspending");
                        break;
                    case PowerBroadcastEvent.PBT_APMRESUMEAUTOMATIC:
                        Console.WriteLine("Resumed");
                        break;
                    case PowerBroadcastEvent.PBT_APMRESUMESUSPEND:
                        Console.WriteLine("Resumed, the user is present");
                        break;
                    case PowerBroadcastEvent.PBT_APMPOWERSTATUSCHANGE:
                        Console.WriteLine("Power source or battery level changed");
                        break;
                }
            });
        #endregion
    }

    public static void WakeTimerWithPowerEvents()
    {
        #region WakeTimerWithPowerEvents
        var timer = new WaitableTimer();

        // Do the nightly work when the PC woke up for it ...
        var subscription = PowerBroadcastListener.ResumedAutomatically
            .Take(1)
            .Subscribe(_ =>
            {
                // ... on another thread, the SharedMessageWindow thread must not be blocked
                Task.Run(() =>
                {
                    // ... and keep the PC awake until the work is done
                    using (SystemStateApi.PreventSystemSleep("Nightly backup"))
                    {
                        RunScheduledWork();
                    }
                    timer.Dispose();
                });
            });

        // Wake up tomorrow at 03:00 local time
        timer.SetAt(new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(3)), wakeSystem: true);
        #endregion
    }

    private static Task RecordAsync() => Task.CompletedTask;
    private static Task UploadAsync() => Task.CompletedTask;
    private static void SaveState() { }
    private static void Reconnect() { }
    private static void RunScheduledWork() { }
}
