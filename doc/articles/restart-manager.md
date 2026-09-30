# Restart Manager

The Windows [Restart Manager](https://learn.microsoft.com/windows/win32/rstmgr/restart-manager-portal) lets an
installer find the applications that use the files it wants to replace, close them, and restart them afterwards.
Dapplo.Windows has a package for each side:

| Package | For | Main type |
|---|---|---|
| **Dapplo.Windows.AppRestartManager** | applications which should survive an update or a reboot | `ApplicationRestartManager` |
| **Dapplo.Windows.InstallerManager** | installers and updaters | `InstallerRestartManager` |

```powershell
dotnet add package Dapplo.Windows.AppRestartManager
dotnet add package Dapplo.Windows.InstallerManager
```

Namespaces used on this page: `Dapplo.Windows.AppRestartManager`, `Dapplo.Windows.AppRestartManager.Enums`,
`Dapplo.Windows.InstallerManager`, `Dapplo.Windows.InstallerManager.Enums`, `System.Reactive.Linq`.

## Application side

### Register for restart

Register early, for example in `Main`. When an installer closes the application through the Restart Manager (or
Windows Update restarts the PC and "Restart apps" is enabled), Windows starts it again with the command line you
registered. Windows doesn't tell the new process that it was restarted, so register an argument that you only use
for this and check for it.

<!-- sample: RestartManagerSamples.Register -->
```csharp
// Early in Main: restart me with "/restore" when an installer or Windows Update closes me.
// Don't include the executable, Windows adds it. At most RestartMaxCmdLine (1024) characters.
ApplicationRestartManager.RegisterForRestart("/restore");

// Windows can't tell a process that it was restarted, check for your own argument instead
// (a manual start with the same argument returns true too)
if (ApplicationRestartManager.WasRestartRequested("/restore"))
{
    RestoreDocuments();
}
```

The flags exclude cases in which you don't want a restart. A restart after a crash or a hang only happens when the
process was running for at least 60 seconds.

<!-- sample: RestartManagerSamples.RegisterFlags -->
```csharp
// Restart after an update, but not after a crash or hang (those restarts need 60 seconds of uptime anyway)
ApplicationRestartManager.RegisterForRestart("/restore",
    ApplicationRestartFlags.RestartNoCrash | ApplicationRestartFlags.RestartNoHang);

// No automatic restart anymore
ApplicationRestartManager.UnregisterForRestart();
```

| `ApplicationRestartFlags` | Don't restart when |
|---|---|
| `RestartNoCrash` | the application crashed |
| `RestartNoHang` | the application hung |
| `RestartNoPatch` | the application is closed for an update |
| `RestartNoReboot` | the system restarts for an update |

### Being asked to close

Before the session ends (shutdown, restart, log off) and when an installer wants to close the application, Windows
sends `WM_QUERYENDSESSION` and then `WM_ENDSESSION` to every top-level window. `ListenForEndSession()` gives you these
messages of the SharedMessageWindow as `EndSessionMessage`, so this also works for console applications and services
without windows.

- Answer a query (`IsQuery`) synchronously inside `OnNext`: `Veto(reason)` asks Windows not to end the session and
  shows the reason in the "apps are preventing shutdown" screen; `CanEndSession = true` (or not answering) allows it.
  The user can still choose to shut down anyway.
- `EndSessionReason` says why: `ENDSESSION_CLOSEAPP` is the Restart Manager (an installer), `ENDSESSION_LOGOFF` a
  log off, `ENDSESSION_CRITICAL` a forced shutdown.
- For `WM_ENDSESSION` with `IsSessionEnding`, the process can be ended as soon as `OnNext` returns: save synchronously,
  don't rely on marshalling to the UI thread.

<!-- sample: RestartManagerSamples.EndSession -->
```csharp
// WM_QUERYENDSESSION and WM_ENDSESSION, received by the SharedMessageWindow.
// OnNext runs on the SharedMessageWindow thread, answer synchronously: no ObserveOn before the answer.
IDisposable subscription = ApplicationRestartManager.ListenForEndSession()
    .Subscribe(message =>
    {
        if (message.IsQuery)
        {
            // ENDSESSION_CLOSEAPP: an installer (Restart Manager) wants to replace files which this process uses
            bool isRestartManager = (message.EndSessionReason & EndSessionReasons.ENDSESSION_CLOSEAPP) != 0;
            if (HasUnsavedWork() && !isRestartManager)
            {
                // Windows shows the reason in its "apps are preventing shutdown" screen
                message.Veto("Unsaved changes");
            }
            // Not answering allows the session to end
        }
        else if (message.IsSessionEnding)
        {
            // WM_ENDSESSION: the process can be terminated as soon as this returns, save synchronously
            SaveState();
        }
    });
```

Windows Forms (`FormClosing` with `CloseReason.WindowsShutDown`) and WPF (`Application.SessionEnding`) report the
same messages for their own windows; `ListenForEndSession` works without a UI framework.

## Installer side

`InstallerRestartManager.CreateSession()` starts a Restart Manager session; dispose it to end the session. Register
the files, processes or services you want to replace, then ask which processes use them.

<!-- sample: RestartManagerSamples.FindLockingProcesses -->
```csharp
using var session = InstallerRestartManager.CreateSession();
session.RegisterFiles(@"C:\Program Files\MyApp\MyApp.exe", @"C:\Program Files\MyApp\MyApp.Core.dll");

var processes = session.GetProcessesUsingResources(out RmRebootReason rebootReason);
foreach (var process in processes)
{
    Console.WriteLine($"{process.AppName} (PID {process.Process.ProcessId}, {process.ApplicationType}), can be restarted: {process.IsRestartable}");
}
if (rebootReason != RmRebootReason.RmRebootReasonNone)
{
    Console.WriteLine($"Replacing the files needs a reboot: {rebootReason}");
}
```

`RmProcessInfo` has `AppName`, `Process.ProcessId` (with `Process.ProcessStartTime` to tell a reused process id apart),
`ServiceShortName`, `ApplicationType` (`RmMainWindow`, `RmService`, `RmExplorer`, `RmConsole`, `RmCritical`, ...),
`AppStatus`, `TerminalServicesSessionId` and `IsRestartable`.

### Replace files and restart the applications

`Shutdown()` asks the applications to close. The default, `RmShutdownType.Graceful`, fails with a `Win32Exception`
when an application refuses (for example because of unsaved work), so nobody loses data.
`RmShutdownType.RmForceShutdown` kills applications which don't respond; use it only when you must.
`RmShutdownOnlyRegistered` only closes applications which registered for restart. After the files are replaced,
`Restart()` starts the applications again which registered for restart.

<!-- sample: RestartManagerSamples.UpdateFiles -->
```csharp
using var session = InstallerRestartManager.CreateSession();
session.RegisterFiles(Directory.GetFiles(installDirectory, "*.dll"));

if (session.IsRebootRequired())
{
    Console.WriteLine("Can't update without a reboot, schedule the update instead");
    return;
}

try
{
    // Graceful (the default): asks the applications to close, fails when one of them refuses (e.g. unsaved work)
    session.Shutdown(statusCallback: percent => Console.WriteLine($"Closing applications: {percent}%"));
}
catch (Win32Exception ex)
{
    Console.WriteLine($"An application refused to close: {ex.Message}");
    // Only when you must: RmShutdownType.RmForceShutdown kills unresponsive applications, which can lose data
    return;
}

foreach (var file in Directory.GetFiles(newFilesDirectory))
{
    File.Copy(file, Path.Combine(installDirectory, Path.GetFileName(file)), overwrite: true);
}

// Restart the applications which registered for restart (RegisterApplicationRestart)
session.Restart();
```

`IsRebootRequired()` and `GetRebootReason()` query the Restart Manager again; when you also need the process list, use
`GetProcessesUsingResources(out rebootReason)` which gives both.

| `RmRebootReason` | Meaning |
|---|---|
| `RmRebootReasonNone` | no reboot needed |
| `RmRebootReasonPermissionDenied` | a process can't be closed with the current rights (run elevated) |
| `RmRebootReasonSessionMismatch` | a process runs in another session |
| `RmRebootReasonCriticalProcess` / `RmRebootReasonCriticalService` | a critical process or service uses the files |
| `RmRebootReasonDetectedSelf` | the installer itself uses the files |

### Services

<!-- sample: RestartManagerSamples.Services -->
```csharp
using var session = InstallerRestartManager.CreateSession();
// Services are registered by their short name, stopping them needs administrator rights
session.RegisterServices("Spooler");
var affected = session.GetProcessesUsingResources();
Console.WriteLine($"Affected: {string.Join(", ", affected.Select(p => p.AppName))}");
```

`RestartManagerApi` has the raw P/Invoke declarations (`RmStartSession`, `RmRegisterResources`, `RmGetList`,
`RmShutdown`, `RmRestart`, `RmEndSession`) if you need more control.

The example project
[Dapplo.Windows.Example.InstallerExample](https://github.com/dapplo/Dapplo.Windows/tree/master/src/Dapplo.Windows.Example.InstallerExample)
closes and restarts the FormsExample.

## See also

- [Power and system state](system-state.md)
- [Window messages and the SharedMessageWindow](window-messages.md)
