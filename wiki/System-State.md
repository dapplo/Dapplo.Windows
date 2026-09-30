# System state

Package **Dapplo.Windows.SystemState**. Full version: [Power and system state](https://www.dapplo.net/Dapplo.Windows/articles/system-state.html).

## Preventing sleep

`PreventSleep` / `PreventSystemSleep` return a `SleepBlocker`; the system can sleep again when it's disposed. It isn't
bound to a thread, so it works around `await`.

<!-- sample: SystemStateSamples.PreventSleep -->
```csharp
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
```

## Sleep, shut down, log off, lock

<!-- sample: SystemStateSamples.SleepAndHibernate -->
```csharp
// Sleep (suspend to RAM), applications get PBT_APMSUSPEND first
bool ok = PowerManagementApi.Sleep();

// Hibernate (suspend to disk)
ok = PowerManagementApi.Hibernate();

// Sleep, and don't let wake timers wake the system
ok = PowerManagementApi.Sleep(disableWakeEvent: true);
```

`Shutdown` / `Restart` enable the shutdown privilege, log a planned shutdown, and only force hung applications unless
you pass `force: true`.

<!-- sample: SystemStateSamples.ShutdownRestartLogOff -->
```csharp
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
```

<!-- sample: SystemStateSamples.LockWorkStation -->
```csharp
// The same as Win+L. true means the lock was started, the screen might not be locked yet.
bool started = PowerManagementApi.LockWorkStation();
```

## Wake timers

`WaitableTimer` can wake the PC; the power option "Allow wake timers" must be enabled, no privilege is needed.

<!-- sample: SystemStateSamples.TimerOnce -->
```csharp
using var timer = new WaitableTimer();

// Fire once, in 30 seconds
timer.SetOnce(TimeSpan.FromSeconds(30));

// Block this thread until the timer fires, but wait at most 60 seconds
bool signaled = timer.Wait(TimeSpan.FromSeconds(60));
Console.WriteLine(signaled ? "Timer fired" : "Timed out");
```

<!-- sample: SystemStateSamples.TimerWakeSystem -->
```csharp
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
```

## Power events

Events arrive on the [[SharedMessageWindow]] thread while Windows waits; save state synchronously on suspend.

<!-- sample: SystemStateSamples.SuspendResume -->
```csharp
// Called on the SharedMessageWindow thread while Windows waits (about 2 seconds): save your state synchronously
var suspendSubscription = PowerBroadcastListener.Suspending
    .Subscribe(_ => SaveState());

// The user is back (opened the lid, pressed a key); this is the moment to reconnect
var resumeSubscription = PowerBroadcastListener.ResumedFromSuspend
    .Subscribe(_ => Reconnect());

// Stop listening
suspendSubscription.Dispose();
resumeSubscription.Dispose();
```

<!-- sample: SystemStateSamples.WakeTimerWithPowerEvents -->
```csharp
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
```
