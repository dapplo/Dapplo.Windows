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

### Check Whether the App Was Restarted

```csharp
if (ApplicationRestartManager.WasRestartRequested())
{
    Console.WriteLine("Restarted after update — restoring previous state.");
    RestorePreviousState();
}
```

### Control When Not to Restart

```csharp
using Dapplo.Windows.Kernel32.Enums;

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

`ApplicationRestartManager.ListenForEndSession()` returns an `IObservable<EndSessionReasons>` that fires when Windows sends `WM_QUERYENDSESSION` or `WM_ENDSESSION`:

```csharp
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

var sub = ApplicationRestartManager.ListenForEndSession()
    .Subscribe(reason =>
    {
        if (reason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
        {
            // Restart Manager is shutting us down for an update
            SaveApplicationState();
        }
        else if (reason.HasFlag(EndSessionReasons.ENDSESSION_LOGOFF))
        {
            SaveUserSettings();
        }
    });

// Dispose on application exit
sub.Dispose();
```

#### `EndSessionReasons` Flags

| Flag | Value | Description |
|------|-------|-------------|
| `ENDSESSION_CLOSEAPP` | `0x1` | App is being closed so a locked file can be replaced |
| `ENDSESSION_CRITICAL` | `0x40000000` | Critical system component update — forced close |
| `ENDSESSION_LOGOFF` | `0x80000000` | User is logging off |

### Complete Example

```csharp
using Dapplo.Windows.AppRestartManager;
using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 1. Register early
        ApplicationRestartManager.RegisterForRestart("/restore");

        // 2. Check if restarted
        if (ApplicationRestartManager.WasRestartRequested())
            RestorePreviousState();

        // 3. Listen for shutdown
        var shutdownSub = ApplicationRestartManager.ListenForEndSession()
            .Subscribe(reason =>
            {
                if (reason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
                    SaveApplicationState();
            });

        Application.Run(new MainForm());

        shutdownSub.Dispose();
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

using var session = InstallerRestartManager.StartSession();

// Register the files your installer needs to replace
session.RegisterResources(new[]
{
    @"C:\Program Files\MyApp\MyApp.exe",
    @"C:\Program Files\MyApp\MyApp.dll"
});
```

### Enumerate Affected Processes

```csharp
foreach (var process in session.GetAffectedApplications())
{
    Console.WriteLine($"Affected: {process.ApplicationName} (PID {process.ProcessId})");
}
```

### Shut Down Affected Applications

```csharp
session.ShutdownApplications(RestartManagerShutdownType.ForceShutdown, progress =>
{
    Console.WriteLine($"Shutdown progress: {progress}%");
});
```

### Replace Files and Restart

```csharp
// Install new files here...

session.RestartApplications(progress =>
{
    Console.WriteLine($"Restart progress: {progress}%");
});
```

## See Also

- [[Getting-Started]]
- [[Common-Scenarios]]
- [Windows Restart Manager (MSDN)](https://docs.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-portal)
