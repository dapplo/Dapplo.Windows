// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Linq;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.InstallerRestartManager;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Examples;

/// <summary>
///     Examples demonstrating the usage of the Restart Manager API for both installers and applications
/// </summary>
public static class RestartManagerExample
{
    // ========================================
    // APPLICATION-SIDE EXAMPLES
    // (For applications that want to be restarted)
    // ========================================

    /// <summary>
    ///     Example: Register an application for automatic restart
    /// </summary>
    public static void RegisterApplicationForRestart()
    {
        Console.WriteLine("Registering application for restart...");
        
        try
        {
            // Register with command-line arguments to restore state
            RestartManager.RegisterForRestart("/restore /minimized");
            
            Console.WriteLine("Application successfully registered for restart.");
            Console.WriteLine("If shut down by Restart Manager, it will restart with: /restore /minimized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to register for restart: {ex.Message}");
        }
    }

    /// <summary>
    ///     Example: Check if the application was restarted
    /// </summary>
    public static void CheckIfRestarted()
    {
        if (RestartManager.WasRestartRequested())
        {
            Console.WriteLine("Application was restarted by Restart Manager!");
            
            var args = RestartManager.GetRestartCommandLineArgs();
            Console.WriteLine($"Restart arguments: {string.Join(" ", args)}");
            
            // Restore application state here
            Console.WriteLine("Restoring previous application state...");
        }
        else
        {
            Console.WriteLine("Normal application start (not restarted)");
        }
    }

    /// <summary>
    ///     Example: Register with flags to control restart behavior
    /// </summary>
    public static void RegisterWithFlags()
    {
        Console.WriteLine("Registering with custom restart flags...");
        
        // Don't restart if the application crashes or hangs
        RestartManager.RegisterForRestart(
            commandLineArgs: "/restore",
            flags: ApplicationRestartFlags.RestartNoCrash | ApplicationRestartFlags.RestartNoHang
        );
        
        Console.WriteLine("Registered: Will restart only for updates, not for crashes/hangs");
    }

    /// <summary>
    ///     Example: Unregister from automatic restart
    /// </summary>
    public static void UnregisterApplication()
    {
        Console.WriteLine("Unregistering from restart...");
        
        try
        {
            RestartManager.UnregisterForRestart();
            Console.WriteLine("Application successfully unregistered from restart.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to unregister: {ex.Message}");
        }
    }

    /// <summary>
    ///     Example: Listen for shutdown/end session events
    /// </summary>
    public static IDisposable ListenForShutdownEvents()
    {
        Console.WriteLine("Starting to listen for shutdown events...");
        
        var subscription = RestartManager.ListenForEndSession()
            .Subscribe(reason =>
            {
                Console.WriteLine($"Shutdown event received: {reason}");
                
                if (reason.HasFlag(EndSessionReasons.ENDSESSION_CLOSEAPP))
                {
                    Console.WriteLine("  -> Application is being closed for an update");
                    Console.WriteLine("  -> Saving application state...");
                    // SaveApplicationState();
                }
                else if (reason.HasFlag(EndSessionReasons.ENDSESSION_LOGOFF))
                {
                    Console.WriteLine("  -> User is logging off");
                    Console.WriteLine("  -> Saving user settings...");
                    // SaveUserSettings();
                }
                else if (reason.HasFlag(EndSessionReasons.ENDSESSION_CRITICAL))
                {
                    Console.WriteLine("  -> Critical shutdown event");
                    Console.WriteLine("  -> Performing emergency save...");
                    // PerformEmergencySave();
                }
            });
        
        Console.WriteLine("Listening for shutdown events. Press any key to stop...");
        return subscription;
    }

    /// <summary>
    ///     Example: Complete application integration with Restart Manager
    /// </summary>
    public static void CompleteApplicationExample()
    {
        Console.WriteLine("=== Complete Application Example ===");
        Console.WriteLine();
        
        // Step 1: Register for restart
        Console.WriteLine("Step 1: Registering for restart...");
        RestartManager.RegisterForRestart("/restore");
        Console.WriteLine("  Registered!");
        Console.WriteLine();
        
        // Step 2: Check if restarted
        Console.WriteLine("Step 2: Checking if app was restarted...");
        if (RestartManager.WasRestartRequested())
        {
            Console.WriteLine("  Yes, restoring state...");
            var args = RestartManager.GetRestartCommandLineArgs();
            Console.WriteLine($"  Command-line args: {string.Join(" ", args)}");
        }
        else
        {
            Console.WriteLine("  No, normal startup");
        }
        Console.WriteLine();
        
        // Step 3: Listen for shutdown events
        Console.WriteLine("Step 3: Setting up shutdown event listener...");
        var subscription = RestartManager.ListenForEndSession()
            .Subscribe(reason =>
            {
                Console.WriteLine($"  Shutdown event: {reason}");
            });
        Console.WriteLine("  Listener active!");
        Console.WriteLine();
        
        Console.WriteLine("Application is now fully integrated with Restart Manager");
        Console.WriteLine("Press any key to cleanup and exit...");
        Console.ReadKey();
        
        // Clean up
        subscription.Dispose();
        Console.WriteLine("Cleaned up!");
    }

    // ========================================
    // INSTALLER/UPDATER EXAMPLES
    // (For installers that manage other applications)
    // ========================================

    /// <summary>
    ///     Example: Find all processes using a specific file
    /// </summary>
    public static void FindProcessesUsingFile(string filePath)
    {
        Console.WriteLine($"Finding processes using: {filePath}");
        Console.WriteLine();

        using (var session = InstallerRestartManager.CreateSession())
        {
            session.RegisterFile(filePath);
            var processes = session.GetProcessesUsingResources(out var rebootReason);

            if (processes.Count == 0)
            {
                Console.WriteLine("No processes are currently using this file.");
                return;
            }

            Console.WriteLine($"Found {processes.Count} process(es) using the file:");
            Console.WriteLine();

            foreach (var process in processes)
            {
                Console.WriteLine($"Process: {process.strAppName}");
                Console.WriteLine($"  PID: {process.Process.dwProcessId}");
                Console.WriteLine($"  Type: {process.ApplicationType}");
                Console.WriteLine($"  Status: {process.AppStatus}");
                Console.WriteLine($"  Restartable: {process.bRestartable}");
                
                if (process.ApplicationType == RmAppType.RmService)
                {
                    Console.WriteLine($"  Service Name: {process.strServiceShortName}");
                }
                
                Console.WriteLine();
            }

            if (rebootReason != RmRebootReason.RmRebootReasonNone)
            {
                Console.WriteLine($"Note: A reboot may be required. Reason: {rebootReason}");
            }
        }
    }

    /// <summary>
    ///     Example: Find processes using multiple files
    /// </summary>
    public static void FindProcessesUsingMultipleFiles(params string[] filePaths)
    {
        Console.WriteLine($"Finding processes using {filePaths.Length} file(s)...");
        Console.WriteLine();

        using (var session = InstallerRestartManager.CreateSession())
        {
            session.RegisterFiles(filePaths);
            var processes = session.GetProcessesUsingResources();

            if (processes.Count == 0)
            {
                Console.WriteLine("No processes are currently using these files.");
                return;
            }

            Console.WriteLine($"Found {processes.Count} process(es) using one or more files:");
            foreach (var process in processes)
            {
                Console.WriteLine($"  - {process.strAppName} (PID: {process.Process.dwProcessId})");
            }
        }
    }

    /// <summary>
    ///     Example: Check if reboot is required for updating a file
    /// </summary>
    public static bool CheckRebootRequired(string filePath)
    {
        Console.WriteLine($"Checking if reboot is required to update: {filePath}");
        
        using (var session = InstallerRestartManager.CreateSession())
        {
            session.RegisterFile(filePath);
            
            if (session.IsRebootRequired())
            {
                var reason = session.GetRebootReason();
                Console.WriteLine($"Reboot IS required. Reason: {reason}");
                
                if (reason.HasFlag(RmRebootReason.RmRebootReasonCriticalProcess))
                {
                    Console.WriteLine("  - A critical process is using the file");
                }
                if (reason.HasFlag(RmRebootReason.RmRebootReasonCriticalService))
                {
                    Console.WriteLine("  - A critical service is using the file");
                }
                if (reason.HasFlag(RmRebootReason.RmRebootReasonPermissionDenied))
                {
                    Console.WriteLine("  - Insufficient privileges to shut down processes");
                }
                if (reason.HasFlag(RmRebootReason.RmRebootReasonSessionMismatch))
                {
                    Console.WriteLine("  - Processes are in different Terminal Services sessions");
                }
                
                return true;
            }
            else
            {
                Console.WriteLine("No reboot required - all processes can be shut down and restarted");
                return false;
            }
        }
    }

    /// <summary>
    ///     Example: Shutdown and restart applications using a file
    ///     WARNING: This will actually shut down applications!
    /// </summary>
    public static void ShutdownAndRestartApplications(string filePath)
    {
        Console.WriteLine($"Preparing to shutdown applications using: {filePath}");
        Console.WriteLine("WARNING: This will shut down applications!");
        
        using (var session = InstallerRestartManager.CreateSession())
        {
            session.RegisterFile(filePath);
            
            var processes = session.GetProcessesUsingResources(out var rebootReason);
            
            if (processes.Count == 0)
            {
                Console.WriteLine("No processes to shut down.");
                return;
            }

            if (rebootReason != RmRebootReason.RmRebootReasonNone)
            {
                Console.WriteLine($"Cannot proceed - reboot would be required: {rebootReason}");
                return;
            }

            Console.WriteLine($"Found {processes.Count} process(es) to shut down:");
            foreach (var process in processes)
            {
                Console.WriteLine($"  - {process.strAppName}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Shutting down applications...");
            
            try
            {
                session.Shutdown(
                    RmShutdownType.RmForceShutdown,
                    progress => Console.WriteLine($"Shutdown progress: {progress}%")
                );
                
                Console.WriteLine("Applications shut down successfully.");
                Console.WriteLine();
                Console.WriteLine("You can now safely update the file...");
                Console.WriteLine("Press Enter to restart applications...");
                Console.ReadLine();
                
                Console.WriteLine("Restarting applications...");
                session.Restart(
                    progress => Console.WriteLine($"Restart progress: {progress}%")
                );
                
                Console.WriteLine("Applications restarted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during shutdown/restart: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Example: Get detailed information about applications using resources
    /// </summary>
    public static void GetDetailedProcessInformation(string filePath)
    {
        Console.WriteLine($"Getting detailed information for processes using: {filePath}");
        Console.WriteLine();

        using (var session = InstallerRestartManager.CreateSession())
        {
            session.RegisterFile(filePath);
            var processes = session.GetProcessesUsingResources();

            foreach (var process in processes)
            {
                Console.WriteLine("═══════════════════════════════════════════════════");
                Console.WriteLine($"Application: {process.strAppName}");
                Console.WriteLine($"Process ID: {process.Process.dwProcessId}");
                Console.WriteLine($"Session Key: {session.SessionKey}");
                Console.WriteLine();
                
                Console.WriteLine($"Application Type: {GetApplicationTypeDescription(process.ApplicationType)}");
                Console.WriteLine($"Status: {process.AppStatus}");
                Console.WriteLine($"Can Restart: {(process.bRestartable ? "Yes" : "No")}");
                
                if (process.ApplicationType == RmAppType.RmService)
                {
                    Console.WriteLine($"Service Short Name: {process.strServiceShortName}");
                }
                
                if (process.TSSessionId != RestartManagerApi.RmInvalidTsSession)
                {
                    Console.WriteLine($"Terminal Services Session: {process.TSSessionId}");
                }
                
                Console.WriteLine();
            }
        }
    }

    private static string GetApplicationTypeDescription(RmAppType type)
    {
        return type switch
        {
            RmAppType.RmUnknownApp => "Unknown Application (cannot be classified)",
            RmAppType.RmMainWindow => "Application with Main Window",
            RmAppType.RmOtherWindow => "Application without Main Window",
            RmAppType.RmService => "Windows Service",
            RmAppType.RmExplorer => "Windows Explorer",
            RmAppType.RmConsole => "Console Application",
            RmAppType.RmCritical => "Critical Process (requires system restart)",
            _ => "Unknown"
        };
    }

    /// <summary>
    ///     Main example runner - uncomment to run different examples
    /// </summary>
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Restart Manager Examples ===");
            Console.WriteLine();
            
            // ========================================
            // APPLICATION-SIDE EXAMPLES
            // ========================================
            Console.WriteLine("--- Application-Side Examples ---");
            Console.WriteLine();
            
            // Example 1: Register for restart
            RegisterApplicationForRestart();
            Console.WriteLine();
            
            // Example 2: Check if restarted
            CheckIfRestarted();
            Console.WriteLine();
            
            // Example 3: Listen for shutdown events
            // Uncomment to test - this will block until a key is pressed
            // var subscription = ListenForShutdownEvents();
            // Console.ReadKey();
            // subscription.Dispose();
            // Console.WriteLine();
            
            // Example 4: Complete application integration
            // CompleteApplicationExample();
            // Console.WriteLine();
            
            // Example 5: Register with custom flags
            // RegisterWithFlags();
            // Console.WriteLine();
            
            // Example 6: Unregister (uncomment if needed)
            // UnregisterApplication();
            // Console.WriteLine();
            
            Console.WriteLine("--- Installer/Updater Examples ---");
            Console.WriteLine();
            
            // ========================================
            // INSTALLER/UPDATER EXAMPLES
            // ========================================
            
            // Example 7: Find processes using a file
            // Note: Replace with an actual file path that might be in use
            var testFile = @"C:\Windows\System32\kernel32.dll";
            
            if (File.Exists(testFile))
            {
                FindProcessesUsingFile(testFile);
            }
            else
            {
                Console.WriteLine($"Test file not found: {testFile}");
                Console.WriteLine("Please modify the testFile variable to point to an existing file.");
            }

            // Example 8: Check if reboot is required
            // CheckRebootRequired(testFile);

            // Example 9: Get detailed process information
            // GetDetailedProcessInformation(testFile);

            // Example 10: Find processes using multiple files
            // FindProcessesUsingMultipleFiles(
            //     @"C:\path\to\file1.dll",
            //     @"C:\path\to\file2.dll"
            // );

            // WARNING: Example 11 will actually shut down applications!
            // Only uncomment if you understand the consequences
            // ShutdownAndRestartApplications(testFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Details: {ex}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
