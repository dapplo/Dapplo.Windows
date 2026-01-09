// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Examples;

/// <summary>
///     Example demonstrating the usage of the Restart Manager API
/// </summary>
public static class RestartManagerExample
{
    /// <summary>
    ///     Example: Find all processes using a specific file
    /// </summary>
    public static void FindProcessesUsingFile(string filePath)
    {
        Console.WriteLine($"Finding processes using: {filePath}");
        Console.WriteLine();

        using (var session = RestartManager.CreateSession())
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

        using (var session = RestartManager.CreateSession())
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
        
        using (var session = RestartManager.CreateSession())
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
        
        using (var session = RestartManager.CreateSession())
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

        using (var session = RestartManager.CreateSession())
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
            // Example 1: Find processes using a file
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

            // Example 2: Check if reboot is required
            // CheckRebootRequired(testFile);

            // Example 3: Get detailed process information
            // GetDetailedProcessInformation(testFile);

            // Example 4: Find processes using multiple files
            // FindProcessesUsingMultipleFiles(
            //     @"C:\path\to\file1.dll",
            //     @"C:\path\to\file2.dll"
            // );

            // WARNING: Example 5 will actually shut down applications!
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
