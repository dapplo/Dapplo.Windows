# Restart Manager

The Restart Manager is a Windows feature that helps minimize application downtime during software installations and updates. It identifies which applications are using locked files, allows you to shut them down gracefully, and then restart them after the update is complete.

## Overview

Dapplo.Windows provides a comprehensive .NET API for the Windows Restart Manager split into two separate projects for better separation of concerns:

### For Installers/Updaters

- **`Dapplo.Windows.InstallerManager`** - Package for installers
  - **`InstallerRestartManager`** - High-level helper class that manages Restart Manager sessions
  - **`RestartManagerApi`** (in Kernel32) - Low-level P/Invoke declarations (for advanced scenarios)

### For Applications

- **`Dapplo.Windows.AppRestartManager`** - Package for applications
  - **`ApplicationRestartManager`** - High-level helper class for applications to register for automatic restart
  - **Contains `RegisterApplicationRestart` and `UnregisterApplicationRestart` P/Invoke declarations

## Application-Side API

If you're developing an application that may be updated or needs to be restarted during system updates, use the `ApplicationRestartManager` class from the `Dapplo.Windows.AppRestartManager` package.

### Registering Your Application for Restart

Applications can register themselves to be automatically restarted by Restart Manager:

```csharp
using Dapplo.Windows.AppRestartManager;

// Register with command-line arguments that restore the previous state
ApplicationRestartManager.RegisterForRestart("/restore /minimized");

// Register without arguments
ApplicationRestartManager.RegisterForRestart();
```

A restart after a crash or a hang only happens when the process was running for at least 60 seconds.

### When to Register for Restart

The best time to register for restart is during application startup, in your `Main` method or application initialization.

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

### Controlling Restart Behavior

You can control when your application should NOT be restarted using flags:

```csharp
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;

ApplicationRestartManager.RegisterForRestart(
    commandLineArgs: "/restore",
    flags: ApplicationRestartFlags.RestartNoCrash   // do not restart on crash
          | ApplicationRestartFlags.RestartNoHang); // do not restart on hang
```

### Unregistering from Restart

If your application no longer wants to be restarted automatically:

```csharp
ApplicationRestartManager.UnregisterForRestart();
```

### Listening for Shutdown Events

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

#### EndSession Reasons

The `EndSessionReasons` enum provides flags indicating why the session is ending:

| Flag | Value | Description |
|------|-------|-------------|
| `None` | `0x0` | The system is shutting down or restarting |
| `ENDSESSION_CLOSEAPP` | `0x1` | The Restart Manager closes the application, e.g. so a locked file can be replaced |
| `ENDSESSION_CRITICAL` | `0x40000000` | The application is forced to shut down |
| `ENDSESSION_LOGOFF` | `0x80000000` | The user is logging off |

### Complete Application Example

Here's a complete example showing how an application should integrate with Restart Manager:

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

### Detecting Restart Manager Shutdown (Alternative Methods)

When your application is being shut down by Restart Manager, it also receives normal Windows shutdown messages (WM_QUERYENDSESSION). You can handle these in traditional ways as well:

```csharp
// In a Windows Forms application
protected override void OnFormClosing(FormClosingEventArgs e)
{
    if (e.CloseReason == CloseReason.WindowsShutDown)
    {
        // Save application state
        SaveState();
        
        // Allow the shutdown to proceed
        e.Cancel = false;
    }
    
    base.OnFormClosing(e);
}

// In a WPF application
protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
{
    // Save application state
    SaveState();
    
    // Allow the session to end
    e.Cancel = false;
    
    base.OnSessionEnding(e);
}

// In a console application
Console.CancelKeyPress += (sender, e) =>
{
    SaveState();
};
```

## Installer/Updater API

If you're developing an installer or updater, use the `InstallerRestartManager` class from the `Dapplo.Windows.InstallerManager` package to manage other applications.

## Common Use Cases

### Finding Processes Using a File

One of the most common use cases is finding out which processes are locking a specific file:

```csharp
using System;
using Dapplo.Windows.InstallerManager;
using Dapplo.Windows.Kernel32.Enums;

// Find processes using a file
using (var session = InstallerRestartManager.CreateSession())
{
    session.RegisterFile(@"C:\path\to\locked\file.dll");
    var processes = session.GetProcessesUsingResources();
    
    foreach (var process in processes)
    {
        Console.WriteLine($"Process: {process.strAppName}");
        Console.WriteLine($"  PID: {process.Process.dwProcessId}");
        Console.WriteLine($"  Type: {process.ApplicationType}");
        Console.WriteLine($"  Restartable: {process.bRestartable}");
        Console.WriteLine();
    }
}
```

### Finding Processes Using Multiple Files

You can also register multiple files at once:

```csharp
using (var session = InstallerRestartManager.CreateSession())
{
    session.RegisterFiles(
        @"C:\path\to\file1.dll",
        @"C:\path\to\file2.dll",
        @"C:\path\to\file3.exe"
    );
    
    var processes = session.GetProcessesUsingResources(out var rebootReason);
    
    if (rebootReason != RmRebootReason.RmRebootReasonNone)
    {
        Console.WriteLine($"Reboot required: {rebootReason}");
    }
    
    foreach (var process in processes)
    {
        Console.WriteLine($"{process.strAppName} is using one or more registered files");
    }
}
```

### Checking if Reboot is Required

Before installing updates, you might want to check if a system reboot would be required:

```csharp
using (var session = InstallerRestartManager.CreateSession())
{
    // Register the files you plan to update
    session.RegisterFiles(@"C:\Windows\System32\somedriver.sys");
    
    if (session.IsRebootRequired())
    {
        var reason = session.GetRebootReason();
        Console.WriteLine($"Reboot will be required: {reason}");
        
        if (reason.HasFlag(RmRebootReason.RmRebootReasonCriticalProcess))
        {
            Console.WriteLine("A critical process is using the file");
        }
        if (reason.HasFlag(RmRebootReason.RmRebootReasonCriticalService))
        {
            Console.WriteLine("A critical service is using the file");
        }
    }
    else
    {
        Console.WriteLine("No reboot required - all processes can be restarted");
    }
}
```

### Shutting Down and Restarting Applications

For installer scenarios, you can shut down applications using the registered resources and restart them later:

```csharp
using (var session = InstallerRestartManager.CreateSession())
{
    // Register the files you need to update
    session.RegisterFiles(@"C:\Program Files\MyApp\MyApp.exe");
    
    var processes = session.GetProcessesUsingResources();
    
    if (processes.Count > 0)
    {
        Console.WriteLine("Shutting down applications...");
        
        // Shut down with progress callback
        session.Shutdown(
            RmShutdownType.RmForceShutdown,
            progress => Console.WriteLine($"Shutdown progress: {progress}%")
        );
        
        // Now you can safely update the files
        Console.WriteLine("Files are no longer locked. Performing update...");
        // ... perform your file operations ...
        
        // Restart the applications
        Console.WriteLine("Restarting applications...");
        session.Restart(
            progress => Console.WriteLine($"Restart progress: {progress}%")
        );
    }
}
```

### Working with Services

You can also register Windows services:

```csharp
using (var session = InstallerRestartManager.CreateSession())
{
    // Register services by their short names
    session.RegisterServices("MyService", "AnotherService");
    
    var processes = session.GetProcessesUsingResources();
    
    foreach (var process in processes)
    {
        if (process.ApplicationType == RmAppType.RmService)
        {
            Console.WriteLine($"Service: {process.strServiceShortName}");
            Console.WriteLine($"  Display Name: {process.strAppName}");
        }
    }
}
```

## Understanding Process Information

The `RmProcessInfo` structure provides detailed information about each process:

- **`Process`** - Contains the process ID and start time (as `RmUniqueProcess`)
- **`strAppName`** - User-friendly application name or service display name
- **`strServiceShortName`** - Short service name (for services only)
- **`ApplicationType`** - Type of application (service, main window, console, etc.)
- **`AppStatus`** - Current status (running, stopped, restarted, etc.)
- **`TSSessionId`** - Terminal Services session ID
- **`bRestartable`** - Whether the application can be automatically restarted

## Application Types

The `RmAppType` enumeration describes different types of applications:

- **`RmUnknownApp`** - Cannot be classified; requires forced shutdown
- **`RmMainWindow`** - Stand-alone application with a top-level window
- **`RmOtherWindow`** - Application without a top-level window
- **`RmService`** - Windows service
- **`RmExplorer`** - Windows Explorer
- **`RmConsole`** - Console application
- **`RmCritical`** - Critical process; system restart required

## Reboot Reasons

The `RmRebootReason` flags indicate why a system restart might be needed:

- **`RmRebootReasonNone`** - No restart required
- **`RmRebootReasonPermissionDenied`** - Insufficient privileges to shut down processes
- **`RmRebootReasonSessionMismatch`** - Processes in different Terminal Services sessions
- **`RmRebootReasonCriticalProcess`** - Critical processes need to be shut down
- **`RmRebootReasonCriticalService`** - Critical services need to be shut down
- **`RmRebootReasonDetectedSelf`** - The current process needs to be shut down

## Best Practices

1. **Always use `using` statements** - The `InstallerRestartManager` class implements `IDisposable` and will properly clean up the session when disposed.

2. **Check reboot requirements first** - Before attempting shutdown/restart, check if a reboot would be required to avoid unexpected results.

3. **Handle exceptions** - The Restart Manager can throw `Win32Exception` for various error conditions. Always wrap calls in try-catch blocks in production code.

4. **Use appropriate shutdown types** - Choose between `RmForceShutdown` (force unresponsive apps) and `RmShutdownOnlyRegistered` (only shutdown apps registered for restart).

5. **Test with elevated privileges** - Some operations may require administrator privileges to shut down certain processes or services.

## Error Handling Example

```csharp
using System;
using System.ComponentModel;
using Dapplo.Windows.InstallerManager;

try
{
    using (var session = InstallerRestartManager.CreateSession())
    {
        session.RegisterFile(@"C:\path\to\file.dll");
        
        var processes = session.GetProcessesUsingResources();
        // Process the results...
    }
}
catch (Win32Exception ex)
{
    Console.WriteLine($"Restart Manager error: {ex.Message} (Error code: {ex.NativeErrorCode})");
}
catch (ObjectDisposedException)
{
    Console.WriteLine("Session has already been disposed");
}
```

## Advanced: Using Low-Level API

For advanced scenarios, you can use the low-level `RestartManagerApi` class directly:

```csharp
using System.Text;
using Dapplo.Windows.Kernel32;

var sessionKey = new StringBuilder(RestartManagerApi.RmSessionKeyLen + 1);
var result = RestartManagerApi.RmStartSession(out var sessionHandle, 0, sessionKey);

if (result == 0)
{
    try
    {
        // Use RestartManagerApi functions directly...
        string[] files = { @"C:\path\to\file.dll" };
        result = RestartManagerApi.RmRegisterResources(sessionHandle, 1, files, 0, null, 0, null);
        // ... more operations ...
    }
    finally
    {
        RestartManagerApi.RmEndSession(sessionHandle);
    }
}
```

## Additional Resources

- [Microsoft Restart Manager Documentation](https://docs.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-portal)
- [Restart Manager API Reference](https://docs.microsoft.com/en-us/windows/win32/rstmgr/restart-manager-reference)
