# System State

The `Dapplo.Windows.SystemState` package provides modern C# APIs for controlling and monitoring the Windows power state.

```powershell
Install-Package Dapplo.Windows.SystemState
```

## Putting the System to Sleep or Hibernate

```csharp
using Dapplo.Windows.SystemState;

// Sleep (suspend to RAM)
PowerManagementApi.Sleep();

// Hibernate (suspend to disk)
PowerManagementApi.Hibernate();
```

## Shutdown, Restart, and Logoff

> Shutdown and reboot require the `SE_SHUTDOWN_NAME` privilege.

```csharp
using Dapplo.Windows.SystemState;

PowerManagementApi.LogOff();
PowerManagementApi.Shutdown();
PowerManagementApi.Restart();
PowerManagementApi.LockWorkStation();
```

## Thread Execution State — Preventing Sleep

```csharp
using Dapplo.Windows.SystemState;

// Prevent both system and display from sleeping
SystemStateApi.PreventSleep();
DoLongRunningWork();
SystemStateApi.AllowSleep(); // always restore when done

// Prevent only the system from sleeping (screen may still turn off)
SystemStateApi.PreventSystemSleep();
DoBackgroundSync();
SystemStateApi.AllowSleep();
```

## WaitableTimer — Scheduled Wake-Up

```csharp
using Dapplo.Windows.SystemState;

// Wake the PC from sleep in 2 hours
using var timer = new WaitableTimer();
timer.SetOnce(TimeSpan.FromHours(2), wakeSystem: true);
timer.Wait(TimeSpan.FromHours(3));
Console.WriteLine("System woke up");
```

## Observing Power Broadcast Events

`PowerBroadcastListener` exposes `WM_POWERBROADCAST` messages as `IObservable<PowerBroadcastEvent>` streams
powered by `SharedMessageWindow`.

```csharp
using Dapplo.Windows.SystemState;
using Dapplo.Windows.SystemState.Enums;
using System.Reactive.Linq;

// React before the system suspends
var suspendSub = PowerBroadcastListener.Suspending
    .Subscribe(_ => FlushApplicationState());

// React when the user wakes the PC
var resumeSub = PowerBroadcastListener.ResumedFromSuspend
    .Subscribe(_ => ReconnectServices());

// React to a scheduled (timer) wake — no user present
var autoResumeSub = PowerBroadcastListener.ResumedAutomatically
    .Subscribe(_ => PerformScheduledBackgroundTask());

// Filter the full event stream
var allSub = PowerBroadcastListener.PowerEvents
    .Subscribe(e => Console.WriteLine($"Power event: {e}"));

// Dispose when done
suspendSub.Dispose();
resumeSub.Dispose();
autoResumeSub.Dispose();
allSub.Dispose();
```

## Combining Wake Timers with Power Events

```csharp
using Dapplo.Windows.SystemState;
using System.Reactive.Linq;

// 1. Subscribe before setting the timer
var wakeHandlerSub = PowerBroadcastListener.ResumedAutomatically
    .Take(1)
    .Subscribe(_ => PerformNightlyBackup());

// 2. Schedule the wake-up for 3 AM tomorrow
using var timer = new WaitableTimer();
timer.SetAt(DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(3), wakeSystem: true);

// 3. Optionally sleep the system now
PowerManagementApi.Sleep();

// 4. Dispose when no longer needed
wakeHandlerSub.Dispose();
```

## See Also

- [WM_POWERBROADCAST (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast)
- [SetSuspendState (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-setsuspendstate)
- [SetThreadExecutionState (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate)
- [System Wake-Up Events (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events)
