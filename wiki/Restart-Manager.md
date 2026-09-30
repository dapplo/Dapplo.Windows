# Restart Manager

Packages **Dapplo.Windows.AppRestartManager** (applications) and **Dapplo.Windows.InstallerManager** (installers).
Full version: [Restart Manager](https://www.dapplo.net/Dapplo.Windows/articles/restart-manager.html).

## Application side

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

`ListenForEndSession()` reports `WM_QUERYENDSESSION` / `WM_ENDSESSION` on the [[SharedMessageWindow]] thread. Answer
synchronously; after `WM_ENDSESSION` the process can end at any moment.

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

## Installer side

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

`Shutdown()` is graceful by default: it fails when an application refuses to close. `RmShutdownType.RmForceShutdown`
is opt-in.

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
