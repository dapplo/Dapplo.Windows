# Restart Manager

Windows Restart Manager minimizes application downtime during software installations and updates. It identifies which applications have locked files, shuts them down gracefully, and restarts them once the update is complete.

Dapplo.Windows provides two packages for Restart Manager integration:

| Package | Use case |
|---------|----------|
| `Dapplo.Windows.AppRestartManager` | Your **application** registers to be restarted after an update |
| `Dapplo.Windows.InstallerManager` | Your **installer** coordinates shutting down and restarting other processes |

## Application-Side (`Dapplo.Windows.AppRestartManager`)

### Installation

```powershell
Install-Package Dapplo.Windows.AppRestartManager
```

### Register for Automatic Restart

Call this early in your application's startup — before any windows are shown:

```csharp
using Dapplo.Windows.AppRestartManager;

// Register with command-line arguments that restore the previous state
ApplicationRestartManager.RegisterForRestart("/restore /minimized");

// Register without arguments
ApplicationRestartManager.RegisterForRestart();
```

A restart after a crash or a hang only happens when the process was running for at least 60 seconds.

### Check Whether the App Was Restarted

Windows does not tell a process that it was restarted: the restarted instance only gets the command line you registered.
Register an argument that you use only for this, and check for it with `WasRestartRequested(argument)`
(a manual start with the same argument also returns true):

```csharp
using Dapplo.Windows.AppRestartManager;

static void Main(string[] args)
{
    // Register early; these arguments are passed to the restarted instance
    ApplicationRestartManager.RegisterForRestart("/restore");

    if (ApplicationRestartManager.WasRestartRequested("/restore"))
    {
        Console.WriteLine("Restarted after an update, restoring the previous state.");
        RestorePreviousState();
    }

    Application.Run(new MainForm());
}
```

### Control When Not to Restart

```csharp
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

ApplicationRestartManager.RegisterForRestart(
    commandLineArgs: "/restore",
    flags: ApplicationRestartFlags.RestartNoCrash   // do not restart on crash
          | ApplicationRestartFlags.RestartNoHang); // do not restart on hang
```

| Flag | Meaning |
|------|---------|
| `RestartNoCrash` | Do not restart if the process crashed |
| `RestartNoHang` | Do not restart if the process was unresponsive |
| `RestartNoPatch` | Do not restart as part of a software patch |
| `RestartNoReboot` | Do not restart as part of a system reboot |

### Unregister

```csharp
ApplicationRestartManager.UnregisterForRestart();
```

### Listen for Shutdown Events

`ApplicationRestartManager.ListenForEndSession()` returns an `IObservable<EndSessionMessage>` which produces a message for every
`WM_QUERYENDSESSION` (the system or the Restart Manager asks whether the session may end) and `WM_ENDSESSION` (the outcome).
Every subscriber sees every message, independent subscriptions are fine.

- `Msg`, `IsQuery`: which of the two messages it is.
- `EndSessionReason`: the `EndSessionReasons` flags (lParam), e.g. `ENDSESSION_CLOSEAPP` when the Restart Manager wants the application to close.
- `CanEndSession` / `Veto(reason)`: answer a query. The default is to allow the session to end. `Veto` blocks it and, when a reason is given,
  shows that text in the Windows shutdown UI (ShutdownBlockReasonCreate). The user can still choose to shut down anyway.
- `IsSessionEnding`: for `WM_ENDSESSION`, true when the session really ends, false when the shutdown was cancelled.

The messages are delivered synchronously on the thread of the `SharedMessageWindow`. Answer and save **inside** `OnNext`:
after an `ObserveOn`, `await` or any other thread hop the reply has already been sent to Windows. After `WM_ENDSESSION` with
`IsSessionEnding` the process can be terminated as soon as `OnNext` returns, so don't rely on marshalling work to the UI thread.

```csharp
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

var subscription = ApplicationRestartManager.ListenForEndSession()
    .Subscribe(endSession =>
    {
        if (endSession.IsQuery)
        {
            if (endSession.EndSessionReason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
            {
                // The Restart Manager closes us for an update, we will be restarted: save and allow
                SaveApplicationState();
            }
            else if (HasUnsavedDocuments())
            {
                // Block the shutdown / log off and tell the user why
                endSession.Veto("There are unsaved documents");
            }
            return;
        }

        if (endSession.IsSessionEnding)
        {
            // The session really ends: save now, synchronously
            SaveUserSettings();
        }
    });

// Dispose on application exit
subscription.Dispose();
```

#### `EndSessionReasons` Flags

| Flag | Value | Description |
|------|-------|-------------|
| `None` | `0x0` | The system is shutting down or restarting |
| `ENDSESSION_CLOSEAPP` | `0x1` | The Restart Manager closes the application, e.g. so a locked file can be replaced |
| `ENDSESSION_CRITICAL` | `0x40000000` | The application is forced to shut down |
| `ENDSESSION_LOGOFF` | `0x80000000` | The user is logging off |

### Complete Example

```csharp
using System;
using System.Windows.Forms;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 1. Register early
        ApplicationRestartManager.RegisterForRestart("/restore");

        // 2. Check if we were restarted
        if (ApplicationRestartManager.WasRestartRequested("/restore"))
        {
            RestorePreviousState();
        }

        // 3. Listen for shutdown, this runs on the SharedMessageWindow thread
        var shutdownSubscription = ApplicationRestartManager.ListenForEndSession()
            .Subscribe(endSession =>
            {
                if (endSession.IsQuery && endSession.EndSessionReason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
                {
                    SaveApplicationState();
                }
            });

        Application.Run(new MainForm());

        shutdownSubscription.Dispose();
        ApplicationRestartManager.UnregisterForRestart();
    }
}
```

## Installer-Side (`Dapplo.Windows.InstallerManager`)

### Installation

```powershell
Install-Package Dapplo.Windows.InstallerManager
```

### Start a Restart Manager Session

```csharp
using Dapplo.Windows.InstallerManager;

using var session = InstallerRestartManager.CreateSession();

// Register the files your installer needs to replace
session.RegisterFiles(
    @"C:\Program Files\MyApp\MyApp.exe",
    @"C:\Program Files\MyApp\MyApp.dll");
```

### Enumerate Affected Processes

```csharp
foreach (var process in session.GetProcessesUsingResources())
{
    Console.WriteLine($"Affected: {process.strAppName} (PID {process.Process.dwProcessId})");
}
```

### Shut Down Affected Applications

```csharp
using Dapplo.Windows.Kernel32.Enums;

// RmForceShutdown forces applications which don't respond to close, they can lose unsaved data
session.Shutdown(RmShutdownType.RmForceShutdown, progress =>
{
    Console.WriteLine($"Shutdown progress: {progress}%");
});
```

### Replace Files and Restart

```csharp
// Install new files here...

session.Restart(progress =>
{
    Console.WriteLine($"Restart progress: {progress}%");
});
```

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [Windows Restart Manager (MSDN)](https://docs.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-portal)
