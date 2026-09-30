# Power and system state

**Dapplo.Windows.SystemState** keeps the system awake while your application works, puts it to sleep or shuts it
down, wakes it up with a timer, and reports power events.

```powershell
dotnet add package Dapplo.Windows.SystemState
```

Namespaces used on this page: `Dapplo.Windows.SystemState`, `Dapplo.Windows.SystemState.Enums`,
`System.Reactive.Linq`.

## Preventing sleep

`SystemStateApi.PreventSleep(reason)` keeps the system *and* the display on, `PreventSystemSleep(reason)` only the
system. Both return a `SleepBlocker`; the system can sleep again when it's disposed.

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

A `SleepBlocker` is a power request (`PowerCreateRequest` / `PowerSetRequest`). Unlike `SetThreadExecutionState` it
isn't bound to a thread, so you can create it before an `await` and dispose it after, or keep it in a field. Several
blockers can be active at the same time. The reason is shown by `powercfg /requests`.

<!-- sample: SystemStateSamples.PreventSleepLongRunning -->
```csharp
// A SleepBlocker is not bound to a thread, you can keep it in a field and dispose it anywhere
SleepBlocker blocker = SystemStateApi.PreventSystemSleep("Download in progress");
Console.WriteLine($"Blocking sleep: {blocker.Reason}, display kept on: {blocker.KeepsDisplayOn}");
// ... later, e.g. in a completion callback on another thread
blocker.Dispose();
```

`SystemStateApi.SetThreadExecutionState` is still there, but a state set with `ES_CONTINUOUS` belongs to the calling
thread: only that thread can reset it, and it ends when the thread ends. Don't use it from thread-pool threads or async
code.

## Sleep and hibernate

<!-- sample: SystemStateSamples.SleepAndHibernate -->
```csharp
// Sleep (suspend to RAM), applications get PBT_APMSUSPEND first
bool ok = PowerManagementApi.Sleep();

// Hibernate (suspend to disk)
ok = PowerManagementApi.Hibernate();

// Sleep, and don't let wake timers wake the system
ok = PowerManagementApi.Sleep(disableWakeEvent: true);
```

Applications are told with `PBT_APMSUSPEND` before the system suspends. `SetSuspendState` is also available with all
its parameters.

## Shut down, restart, log off, lock

`Shutdown()` and `Restart()` enable the shutdown privilege of the process first (interactive users have it, but it's
disabled by default), and log a *planned* shutdown in the event log. Without `force`, applications which don't respond
are ended after a timeout (`EWX_FORCEIFHUNG`); `force: true` closes all applications without asking (`EWX_FORCE`),
which can lose data. They return `false` on failure, `Marshal.GetLastWin32Error()` has the reason.

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

For other combinations call `ExitWindowsEx` yourself, after `EnableShutdownPrivilege()`:

<!-- sample: SystemStateSamples.ExitWindowsExDirect -->
```csharp
// ExitWindowsEx needs the shutdown privilege, which is disabled by default
if (PowerManagementApi.EnableShutdownPrivilege())
{
    PowerManagementApi.ExitWindowsEx(ExitWindowsFlags.EWX_REBOOT | ExitWindowsFlags.EWX_RESTARTAPPS | ExitWindowsFlags.EWX_FORCEIFHUNG,
        PowerManagementApi.ShutdownReasonPlannedOther);
}
```

<!-- sample: SystemStateSamples.LockWorkStation -->
```csharp
// The same as Win+L. true means the lock was started, the screen might not be locked yet.
bool started = PowerManagementApi.LockWorkStation();
```

## Waitable timers

`WaitableTimer` wraps a Windows waitable timer. Unlike a .NET timer it can wake the system from sleep or hibernation.
The handle is a `SafeWaitHandle`, so it's released even when you forget `Dispose`.

<!-- sample: SystemStateSamples.TimerOnce -->
```csharp
using var timer = new WaitableTimer();

// Fire once, in 30 seconds
timer.SetOnce(TimeSpan.FromSeconds(30));

// Block this thread until the timer fires, but wait at most 60 seconds
bool signaled = timer.Wait(TimeSpan.FromSeconds(60));
Console.WriteLine(signaled ? "Timer fired" : "Timed out");
```

<!-- sample: SystemStateSamples.TimerAt -->
```csharp
using var timer = new WaitableTimer();

// Fire at a point in time, the offset of the DateTimeOffset is taken into account
timer.SetAt(DateTimeOffset.Now.AddHours(2));
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

### Waking the system

With `wakeSystem: true` the timer wakes the PC. No privilege is needed, but the power option "Allow wake timers" must
be enabled (it's often disabled on battery). After the wake-up Windows broadcasts `PBT_APMRESUMEAUTOMATIC`.

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

`Wait` blocks the thread. To avoid that, register the `WaitHandle` with the thread pool:

<!-- sample: SystemStateSamples.TimerWaitHandle -->
```csharp
var timer = new WaitableTimer();
timer.SetOnce(TimeSpan.FromMinutes(5));

// Don't block a thread: let the thread pool call you when the timer fires
System.Threading.ThreadPool.RegisterWaitForSingleObject(timer.WaitHandle, (state, timedOut) =>
{
    Console.WriteLine("Five minutes are over");
    timer.Dispose();
}, null, System.Threading.Timeout.Infinite, executeOnlyOnce: true);
```

A timer with a name can be opened by other processes:

<!-- sample: SystemStateSamples.TimerNamed -->
```csharp
// Process A creates the timer ...
using var timerA = new WaitableTimer("MyApp_WakeTimer");
timerA.SetOnce(TimeSpan.FromMinutes(30), wakeSystem: true);

// ... process B opens the same timer by its name and waits for it
using var timerB = new WaitableTimer("MyApp_WakeTimer");
bool fired = timerB.Wait(TimeSpan.FromHours(1));
```

See [System Wake-up Events](https://learn.microsoft.com/windows/win32/power/system-wake-up-events).

## Power events

`PowerBroadcastListener` reports `WM_POWERBROADCAST` messages of the SharedMessageWindow as `PowerBroadcastEvent`
values. The events arrive on the SharedMessageWindow thread while Windows waits for the answer: for `PBT_APMSUSPEND`
Windows waits about 2 seconds, so save your state synchronously.

| Observable | Event | When |
|---|---|---|
| `Suspending` | `PBT_APMSUSPEND` | the system is about to sleep or hibernate |
| `ResumedAutomatically` | `PBT_APMRESUMEAUTOMATIC` | after every wake-up; the user might not be there |
| `ResumedFromSuspend` | `PBT_APMRESUMESUSPEND` | after a wake-up by the user (key, lid, power button) |
| `PowerStatusChanged` | `PBT_APMPOWERSTATUSCHANGE` | AC / battery switch, battery level |
| `PowerEvents` | all of them | |

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

When a timer or another program wakes the PC, only `PBT_APMRESUMEAUTOMATIC` arrives; don't show UI then, nobody might
be looking. When the user is there, `PBT_APMRESUMESUSPEND` follows.

<!-- sample: SystemStateSamples.ResumedAutomatically -->
```csharp
// Fires after every wake-up, e.g. by a wake timer. Don't show UI here, maybe nobody is in front of the PC.
// When a user is present, ResumedFromSuspend follows.
var subscription = PowerBroadcastListener.ResumedAutomatically
    .Subscribe(_ => RunScheduledWork());
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

## Wake up, work, sleep again

A nightly job: a wake timer wakes the PC, the work runs on another thread under a `SleepBlocker`, and when the blocker
is disposed Windows goes back to sleep after its idle timeout.

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

## See also

- [Window messages and the SharedMessageWindow](window-messages.md) for session lock / unlock
- [Restart Manager](restart-manager.md) for shutdown requests to your application
