# System State

`Dapplo.Windows.SystemState` provides modern, idiomatic C# APIs for controlling and monitoring the Windows power state — sleep, hibernate, shutdown, restart, logoff, workstation lock, thread execution state, and scheduled wake-up timers.

**Package:** `Dapplo.Windows.SystemState`

```powershell
Install-Package Dapplo.Windows.SystemState
```

---

## In This Page

- [Putting the System to Sleep or Hibernate](#putting-the-system-to-sleep-or-hibernate)
- [Shutdown, Restart, and Logoff](#shutdown-restart-and-logoff)
- [Locking the Workstation](#locking-the-workstation)
- [Preventing Sleep](#preventing-sleep)
- [WaitableTimer — Scheduled Wake-Up](#waitabletimer--scheduled-wake-up)
- [PowerBroadcastListener — Reacting to Power Events](#powerbroadcastlistener--reacting-to-power-events)
- [Combining Wake Timers with Power Events](#combining-wake-timers-with-power-events)

---

## Putting the System to Sleep or Hibernate

`PowerManagementApi.Sleep()` and `PowerManagementApi.Hibernate()` wrap `SetSuspendState` from `powrprof.dll`.

```csharp
using Dapplo.Windows.SystemState;

// Put the system to sleep (suspend to RAM)
PowerManagementApi.Sleep();

// Hibernate (suspend to disk)
PowerManagementApi.Hibernate();

// Sleep but disable any previously scheduled wake events
PowerManagementApi.Sleep(disableWakeEvent: true);
```

---

## Shutdown, Restart, and Logoff

`PowerManagementApi` wraps `ExitWindowsEx` from `user32.dll`.

> `Shutdown` and `Restart` enable the `SE_SHUTDOWN_NAME` privilege of the process (interactive users hold it, but it is disabled by default),
> `Shutdown` powers off (`EWX_POWEROFF`), and both log a planned shutdown (`ShutdownReasonPlannedOther`). Without `force` hung applications are
> terminated after a timeout (`EWX_FORCEIFHUNG`); `force: true` uses `EWX_FORCE`, which can lose data. When calling `ExitWindowsEx` directly,
> call `PowerManagementApi.EnableShutdownPrivilege()` first.

```csharp
using Dapplo.Windows.SystemState;

PowerManagementApi.LogOff();
PowerManagementApi.Shutdown();
PowerManagementApi.Restart();

// Force applications to close before shutting down (they can lose data)
PowerManagementApi.Shutdown(force: true);

// Raw call with explicit flags
using Dapplo.Windows.SystemState.Enums;

PowerManagementApi.EnableShutdownPrivilege();
PowerManagementApi.ExitWindowsEx(
    ExitWindowsFlags.EWX_POWEROFF | ExitWindowsFlags.EWX_FORCEIFHUNG, PowerManagementApi.ShutdownReasonPlannedOther);
```

---

## Locking the Workstation

```csharp
using Dapplo.Windows.SystemState;

// Equivalent to pressing Win+L
PowerManagementApi.LockWorkStation();
```

---

## Preventing Sleep

```csharp
using Dapplo.Windows.SystemState;

// Prevent the system AND the screen from sleeping while a task runs, until the blocker is disposed
using (SystemStateApi.PreventSleep("Rendering video"))
{
    await DoLongRunningWorkAsync();
}

// Prevent only the system from sleeping (screen may still turn off)
using (SystemStateApi.PreventSystemSleep("Synchronizing files"))
{
    await DoBackgroundSyncAsync();
}
```

`PreventSleep` / `PreventSystemSleep` return a `SleepBlocker` which uses a power request (`PowerCreateRequest` / `PowerSetRequest`).
Unlike `SetThreadExecutionState`, a power request is not bound to a thread: it can be created and disposed on different threads (e.g. around an `await`),
several blockers can be active at the same time, and the reason is shown by `powercfg /requests`. The system can sleep again when all blockers are disposed or the process exits.

`SystemStateApi.SetThreadExecutionState` is still available, but note that a state set with `ES_CONTINUOUS` belongs to the calling thread:
it is only reset by a call on the same thread and it ends when that thread exits, so don't use it from thread-pool threads or async code.

---

## WaitableTimer — Scheduled Wake-Up

`WaitableTimer` is a managed `IDisposable` wrapper around the kernel32 waitable timer APIs (`CreateWaitableTimer`, `SetWaitableTimer`, `CancelWaitableTimer`).

### One-Shot (relative delay)

```csharp
using var timer = new WaitableTimer();

timer.SetOnce(TimeSpan.FromMinutes(30));
bool fired = timer.Wait(TimeSpan.FromHours(1));
```

### One-Shot (absolute UTC time)

```csharp
using var timer = new WaitableTimer();

timer.SetAt(DateTimeOffset.UtcNow.AddHours(6));
timer.Wait(TimeSpan.FromHours(7));
```

### Periodic timer

```csharp
using var timer = new WaitableTimer();

// Start in 1 second, repeat every 10 seconds
timer.SetPeriodic(TimeSpan.FromSeconds(1), period: 10_000);

for (int i = 0; i < 5; i++)
{
    timer.Wait(TimeSpan.FromSeconds(15));
    DoPeriodicWork();
}

timer.Cancel();
```

### System Wake-Up

Pass `wakeSystem: true` to schedule the system to wake from sleep or hibernate when the timer fires. This requires the `SE_SYSTEMTIME_NAME` privilege (typically available to elevated processes and services).

```csharp
using var timer = new WaitableTimer();

// Wake the PC in 2 hours (even if it is sleeping)
timer.SetOnce(TimeSpan.FromHours(2), wakeSystem: true);
timer.Wait(TimeSpan.FromHours(3));
Console.WriteLine("System woke up");
```

See also: [System Wake-Up Events (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events)

---

## PowerBroadcastListener — Reacting to Power Events

`PowerBroadcastListener` exposes `WM_POWERBROADCAST` messages as `IObservable<PowerBroadcastEvent>` streams.
It is built on top of [[SharedMessageWindow]] and requires no extra registration step.

> Available on `net480`, `net8.0-windows`, `net10.0-windows` only (not `netstandard2.0`).

```csharp
using Dapplo.Windows.SystemState;
using Dapplo.Windows.SystemState.Enums;

// React just before the system suspends
var suspendSub = PowerBroadcastListener.Suspending
    .Subscribe(_ => FlushApplicationState());

// React when the user wakes the PC
var resumeSub = PowerBroadcastListener.ResumedFromSuspend
    .Subscribe(_ => ReconnectServices());

// React to a programmatic (timer-triggered) wake — no user present
var autoResumeSub = PowerBroadcastListener.ResumedAutomatically
    .Subscribe(_ => PerformScheduledBackgroundTask());

// React to AC/battery power source changes
var powerStatusSub = PowerBroadcastListener.PowerStatusChanged
    .Subscribe(_ => UpdateBatteryIndicator());

// Filter the full event stream for anything not covered above
var allSub = PowerBroadcastListener.PowerEvents
    .Subscribe(e => Log.Info($"Power event: {e}"));

// Dispose subscriptions when done
suspendSub.Dispose();
resumeSub.Dispose();
autoResumeSub.Dispose();
powerStatusSub.Dispose();
allSub.Dispose();
```

### Available Observables

| Property | Event(s) filtered |
|---|---|
| `PowerEvents` | All `WM_POWERBROADCAST` events |
| `Suspending` | `PBT_APMSUSPEND` |
| `ResumedFromSuspend` | `PBT_APMRESUMESUSPEND` |
| `ResumedAutomatically` | `PBT_APMRESUMEAUTOMATIC` |
| `PowerStatusChanged` | `PBT_APMPOWERSTATUSCHANGE` |

---

## Combining Wake Timers with Power Events

A common pattern is to schedule work to run on a wake-timer event:

```csharp
using Dapplo.Windows.SystemState;
using System.Reactive.Linq;

// 1. Subscribe to the automatic resume event BEFORE setting the timer
var wakeHandlerSub = PowerBroadcastListener.ResumedAutomatically
    .Take(1) // handle only the next automatic resume
    .Subscribe(_ =>
    {
        Console.WriteLine("Wake timer fired — performing backup");
        PerformNightlyBackup();
    });

// 2. Schedule the wake-up
using var timer = new WaitableTimer();
timer.SetAt(DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(3), wakeSystem: true); // 3 AM tomorrow

// 3. Optionally sleep the system now
PowerManagementApi.Sleep();

// 4. Dispose when no longer needed
wakeHandlerSub.Dispose();
```

---

## See Also

- [[SharedMessageWindow]] — powers `PowerBroadcastListener`
- [[Restart-Manager]] — react to `WM_QUERYENDSESSION` / `WM_ENDSESSION`
- [[Common-Scenarios]]
- [WM_POWERBROADCAST (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast)
- [SetSuspendState (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate)
- [SetThreadExecutionState (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate)
- [System Wake-Up Events (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events)
