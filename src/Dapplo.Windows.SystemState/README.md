# Dapplo.Windows.SystemState

Modern Windows power state management API — control and monitor system sleep, hibernate, shutdown, logoff, lock, thread execution state, and scheduled wake-up timers.

Targets: `net480`, `netstandard2.0`, `net8.0-windows`, `net10.0-windows`

---

## Table of Contents

- [Installation](#installation)
- [Putting the System to Sleep or Hibernate](#putting-the-system-to-sleep-or-hibernate)
- [Shutdown, Restart, and Logoff](#shutdown-restart-and-logoff)
- [Locking the Workstation](#locking-the-workstation)
- [Thread Execution State (Preventing Sleep)](#thread-execution-state-preventing-sleep)
- [Waitable Timers (Scheduled Wake-Up)](#waitable-timers-scheduled-wake-up)
  - [One-Shot Timer (Relative)](#one-shot-timer-relative)
  - [One-Shot Timer (Absolute UTC Time)](#one-shot-timer-absolute-utc-time)
  - [Periodic Timer](#periodic-timer)
  - [Wake the System on Timer Fire](#wake-the-system-on-timer-fire)
- [Observing Power Broadcast Events](#observing-power-broadcast-events)
  - [Detect Suspend and Resume](#detect-suspend-and-resume)
  - [Detect Automatic Resume (Wake Timer)](#detect-automatic-resume-wake-timer)
  - [All Power Events](#all-power-events)
- [Enum Reference](#enum-reference)
  - [PowerBroadcastEvent](#powerbroadcastevent)
  - [ThreadExecutionStateFlags](#threadexecutionstateflags)
  - [ExitWindowsFlags](#exitwindowsflags)

---

## Installation

```powershell
Install-Package Dapplo.Windows.SystemState
```

---

## Putting the System to Sleep or Hibernate

Use `PowerManagementApi` which wraps `SetSuspendState` from `powrprof.dll`.

```csharp
using Dapplo.Windows.SystemState;

// Sleep (suspend to RAM)
PowerManagementApi.Sleep();

// Hibernate (suspend to disk)
PowerManagementApi.Hibernate();

// Sleep, disabling any scheduled wake events
PowerManagementApi.Sleep(disableWakeEvent: true);

// Raw call — forceCritical skips the WM_POWERBROADCAST notification
PowerManagementApi.SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);
```

> **Privilege note:** No special privileges are required to initiate sleep or hibernate in most scenarios.

---

## Shutdown, Restart, and Logoff

`PowerManagementApi` wraps `ExitWindowsEx` from `user32.dll`.

> **Privilege note:** Shutdown and reboot require the `SE_SHUTDOWN_NAME` privilege. Logoff does not.

```csharp
using Dapplo.Windows.SystemState;

// Log off the current user
PowerManagementApi.LogOff();

// Shut down (power off)
PowerManagementApi.Shutdown();

// Restart
PowerManagementApi.Restart();

// Force close hung applications before shutting down
PowerManagementApi.Shutdown(force: true);

// Raw call with explicit flags
using Dapplo.Windows.SystemState.Enums;

PowerManagementApi.ExitWindowsEx(
    ExitWindowsFlags.EWX_SHUTDOWN | ExitWindowsFlags.EWX_FORCEIFHUNG);
```

---

## Locking the Workstation

```csharp
using Dapplo.Windows.SystemState;

// Lock the screen (equivalent to Win+L)
PowerManagementApi.LockWorkStation();
```

`LockWorkStation` runs asynchronously — a `true` return means the lock was initiated, not that the screen is locked yet.

---

## Thread Execution State (Preventing Sleep)

Use `SystemStateApi` which wraps `SetThreadExecutionState` from `kernel32.dll`.

```csharp
using Dapplo.Windows.SystemState;

// Prevent the system AND the display from sleeping while a long task runs
SystemStateApi.PreventSleep();

DoLongRunningWork();

// Re-enable normal sleep behavior when done
SystemStateApi.AllowSleep();

// Prevent only the system from sleeping (screen may still turn off)
SystemStateApi.PreventSystemSleep();
DoBackgroundWork();
SystemStateApi.AllowSleep();

// One-shot: reset the system idle timer without persistent prevention
using Dapplo.Windows.SystemState.Enums;
SystemStateApi.SetThreadExecutionState(ThreadExecutionStateFlags.ES_SYSTEM_REQUIRED);
```

| Convenience method | What it does |
|---|---|
| `PreventSleep()` | Keeps both system and display awake (`ES_CONTINUOUS \| ES_SYSTEM_REQUIRED \| ES_DISPLAY_REQUIRED`) |
| `PreventSystemSleep()` | Keeps only the system awake, screen may still turn off (`ES_CONTINUOUS \| ES_SYSTEM_REQUIRED`) |
| `AllowSleep()` | Clears the continuous flag, restoring default sleep behavior (`ES_CONTINUOUS`) |

> **Important:** Always call `AllowSleep()` (or `ES_CONTINUOUS` on its own) when the work is complete. Forgetting to restore the state will prevent the system from sleeping until the process exits.

---

## Waitable Timers (Scheduled Wake-Up)

`WaitableTimer` wraps `CreateWaitableTimer`, `SetWaitableTimer`, and `CancelWaitableTimer` from `kernel32.dll` in a convenient `IDisposable` class.

### One-Shot Timer (Relative)

```csharp
using Dapplo.Windows.SystemState;
using System;

using var timer = new WaitableTimer();

// Fire once in 30 seconds
timer.SetOnce(TimeSpan.FromSeconds(30));

// Block the current thread until the timer fires (with a 60-second safety timeout)
bool signaled = timer.Wait(TimeSpan.FromSeconds(60));
Console.WriteLine(signaled ? "Timer fired" : "Timed out waiting");
```

### One-Shot Timer (Absolute UTC Time)

```csharp
using var timer = new WaitableTimer();

// Fire at a specific moment in the future
var wakeAt = DateTimeOffset.UtcNow.AddHours(2);
timer.SetAt(wakeAt);
timer.Wait(TimeSpan.FromHours(3));
```

### Periodic Timer

```csharp
using var timer = new WaitableTimer();

// Start after 1 second, then fire every 5 seconds
timer.SetPeriodic(initialDelay: TimeSpan.FromSeconds(1), period: 5000);

for (int i = 0; i < 5; i++)
{
    timer.Wait(TimeSpan.FromSeconds(10));
    Console.WriteLine($"Tick {i + 1}");
}

timer.Cancel();
```

### Wake the System on Timer Fire

```csharp
using var timer = new WaitableTimer();

// Wake the PC from sleep or hibernate in 2 hours
// Requires SE_SYSTEMTIME_NAME privilege (typically held by services / elevated processes)
timer.SetOnce(TimeSpan.FromHours(2), wakeSystem: true);

// The system will wake up and PBT_APMRESUMEAUTOMATIC will be broadcast.
// Your process will resume execution here:
timer.Wait(TimeSpan.FromHours(3));
Console.WriteLine("System woke up");
```

See [System Wake-Up Events (Microsoft Docs)](https://learn.microsoft.com/en-us/windows/win32/power/system-wake-up-events) for details on how the OS handles programmatic wake events.

### Named Timer (Cross-Process)

```csharp
// Process A — creates the timer
using var timerA = new WaitableTimer("MyApp_WakeTimer");
timerA.SetOnce(TimeSpan.FromMinutes(30), wakeSystem: true);

// Process B — opens the same timer by name
using var timerB = new WaitableTimer("MyApp_WakeTimer");
bool fired = timerB.Wait(TimeSpan.FromHours(1));
```

---

## Observing Power Broadcast Events

`PowerBroadcastListener` exposes `WM_POWERBROADCAST` messages as `IObservable<PowerBroadcastEvent>` streams using `SharedMessageWindow` under the hood.

> Requires `net8.0-windows` or `net10.0-windows` (or `net480`). Not available for `netstandard2.0`.

### Detect Suspend and Resume

```csharp
using Dapplo.Windows.SystemState;
using System.Reactive.Linq;

// Notified just before the system goes to sleep
var suspendSub = PowerBroadcastListener.Suspending
    .Subscribe(_ =>
    {
        Console.WriteLine("System is about to sleep — flushing state...");
        FlushApplicationState();
    });

// Notified after a user-triggered resume (user pressed power button / opened lid)
var resumeSub = PowerBroadcastListener.ResumedFromSuspend
    .Subscribe(_ =>
    {
        Console.WriteLine("System resumed — reconnecting...");
        ReconnectServices();
    });

// Dispose when done
suspendSub.Dispose();
resumeSub.Dispose();
```

### Detect Automatic Resume (Wake Timer)

When the system wakes because of a scheduled `WaitableTimer` (or other programmatic wake event), `PBT_APMRESUMEAUTOMATIC` fires **without** a subsequent `PBT_APMRESUMESUSPEND` (no user present). Your application must not interact with the user during this period.

```csharp
var autoResumeSub = PowerBroadcastListener.ResumedAutomatically
    .Subscribe(_ =>
    {
        Console.WriteLine("System woke automatically — performing background task");
        PerformScheduledBackgroundWork();
    });
```

### All Power Events

```csharp
using Dapplo.Windows.SystemState.Enums;

var allEventsSub = PowerBroadcastListener.PowerEvents
    .Subscribe(e =>
    {
        switch (e)
        {
            case PowerBroadcastEvent.PBT_APMSUSPEND:
                Console.WriteLine("Suspending...");
                break;
            case PowerBroadcastEvent.PBT_APMRESUMEAUTOMATIC:
                Console.WriteLine("Resumed automatically");
                break;
            case PowerBroadcastEvent.PBT_APMRESUMESUSPEND:
                Console.WriteLine("Resumed by user");
                break;
            case PowerBroadcastEvent.PBT_APMPOWERSTATUSCHANGE:
                Console.WriteLine("Power source changed (AC/battery)");
                break;
        }
    });
```

---

## Enum Reference

### PowerBroadcastEvent

| Value | Hex | Description |
|---|---|---|
| `PBT_APMSUSPEND` | `0x0004` | System is about to suspend |
| `PBT_APMRESUMEDCRITICAL` | `0x0006` | Resumed from critical failure (low battery) |
| `PBT_APMRESUMESUSPEND` | `0x0007` | Resumed from sleep — user present |
| `PBT_APMBATTERYLOW` | `0x0009` | Battery power is low |
| `PBT_APMPOWERSTATUSCHANGE` | `0x000A` | AC/battery power status changed |
| `PBT_APMRESUMEAUTOMATIC` | `0x0012` | Resumed automatically (programmatic wake) |
| `PBT_POWERSETTINGCHANGE` | `0x8013` | Power setting changed (lParam → `POWERBROADCAST_SETTING`) |

### ThreadExecutionStateFlags

| Value | Hex | Description |
|---|---|---|
| `ES_SYSTEM_REQUIRED` | `0x00000001` | Reset the system idle timer |
| `ES_DISPLAY_REQUIRED` | `0x00000002` | Reset the display idle timer |
| `ES_AWAYMODE_REQUIRED` | `0x00000040` | Away mode (media playback; must combine with `ES_CONTINUOUS`) |
| `ES_CONTINUOUS` | `0x80000000` | Keep state until cleared by another call with `ES_CONTINUOUS` |

### ExitWindowsFlags

| Value | Hex | Description |
|---|---|---|
| `EWX_LOGOFF` | `0x00000000` | Log off the interactive user |
| `EWX_SHUTDOWN` | `0x00000001` | Shut down the system |
| `EWX_REBOOT` | `0x00000002` | Restart the system |
| `EWX_FORCE` | `0x00000004` | Force close of running apps (no WM_QUERYENDSESSION) |
| `EWX_POWEROFF` | `0x00000008` | Shut down and power off |
| `EWX_FORCEIFHUNG` | `0x00000010` | Force close hung apps after timeout |
| `EWX_RESTARTAPPS` | `0x00000040` | Restart registered apps after reboot |
| `EWX_HYBRID_SHUTDOWN` | `0x00400000` | Fast startup (Windows 8+) |
| `EWX_BOOTOPTIONS` | `0x01000000` | Restart the boot application only |
