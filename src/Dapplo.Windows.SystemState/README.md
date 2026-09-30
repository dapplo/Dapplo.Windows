# Dapplo.Windows.SystemState

Power management for .NET on Windows: keep the system awake while your application works, put it to sleep or shut it
down, wake it up with a timer, and react to suspend / resume. Targets `net480` and `net10.0-windows`.

This package is part of [Dapplo.Windows](https://github.com/dapplo/Dapplo.Windows). Full documentation:
[Power and system state](https://www.dapplo.net/Dapplo.Windows/articles/system-state.html), changes:
[changelog](https://github.com/dapplo/Dapplo.Windows/blob/master/CHANGELOG.md).

## Preventing sleep

`PreventSleep` keeps the system and the display on, `PreventSystemSleep` only the system, until the returned
`SleepBlocker` is disposed. It's a power request, not bound to a thread, so it works around `await`.

Namespace: `Dapplo.Windows.SystemState`.

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

`Shutdown` and `Restart` enable the shutdown privilege and log a planned shutdown. Without `force` hung applications
are ended after a timeout; `force: true` closes everything without asking.

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

`WaitableTimer` can wake the PC from sleep or hibernation (the power option "Allow wake timers" must be enabled; no
privilege is needed).

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

<!-- sample: SystemStateSamples.TimerPeriodic -->
```csharp
using var timer = new WaitableTimer();

// First after 1 second, then every 5000 milliseconds
timer.SetPeriodic(initialDelay: TimeSpan.FromSeconds(1), period: 5000);

for (var i = 0; i < 5; i++)
{
    timer.Wait(TimeSpan.FromSeconds(10));
    Console.WriteLine($"Tick {i + 1}");
}

timer.Cancel();
```

## Power events

`PowerBroadcastListener` reports `WM_POWERBROADCAST` (through the SharedMessageWindow of Dapplo.Windows.Messages).
Events arrive on a background thread while Windows waits: save state synchronously when suspending.

Namespaces: `System.Reactive.Linq`, `Dapplo.Windows.SystemState.Enums`.

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

<!-- sample: SystemStateSamples.AllPowerEvents -->
```csharp
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
```

| `PowerBroadcastEvent` | When |
|---|---|
| `PBT_APMSUSPEND` | the system is about to suspend |
| `PBT_APMRESUMEAUTOMATIC` | after every wake-up, the user might not be there |
| `PBT_APMRESUMESUSPEND` | after a wake-up by the user |
| `PBT_APMPOWERSTATUSCHANGE` | AC / battery switch, battery level |
