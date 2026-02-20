// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Kernel32;

/// <summary>
///     High-level helper class for applications that want to participate in Restart Manager.
///     This allows applications to be gracefully shut down and automatically restarted during updates.
/// </summary>
/// <example>
///     Example usage to register an application for restart:
///     <code>
///     // Register the application to restart with command-line arguments
///     ApplicationRestart.RegisterForRestart("/restore");
///     
///     // Later, when shutting down, check if restarting
///     if (ApplicationRestart.IsRestartRequested())
///     {
///         // Save state, prepare for restart
///     }
///     </code>
/// </example>
public static class ApplicationRestart
{
    /// <summary>
    ///     Maximum length for the command line arguments (in characters).
    ///     This corresponds to the Windows RESTART_MAX_CMD_LINE constant.
    /// </summary>
    public const int RestartMaxCmdLine = 1024;
    
    /// <summary>
    ///     Maximum length for the command line arguments (in characters).
    ///     Alias for RestartMaxCmdLine for backward compatibility.
    /// </summary>
    public const int MaxCommandLineLength = RestartMaxCmdLine;

    /// <summary>
    ///     Registers the current application for automatic restart.
    ///     When the Restart Manager shuts down the application during an update, it will be automatically restarted afterwards.
    /// </summary>
    /// <param name="commandLineArgs">
    ///     Command-line arguments to pass to the application when it is restarted.
    ///     Do not include the executable name - it will be added automatically.
    ///     Maximum length is 1024 characters. Use null or empty string to clear previous registration.
    /// </param>
    /// <param name="flags">
    ///     Flags that control when the application should NOT be restarted.
    ///     Default is None, meaning the application will always be restarted.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when command line arguments exceed maximum length.</exception>
    /// <exception cref="Win32Exception">Thrown when registration fails.</exception>
    public static void RegisterForRestart(string commandLineArgs = null, ApplicationRestartFlags flags = ApplicationRestartFlags.None)
    {
        if (!string.IsNullOrEmpty(commandLineArgs) && commandLineArgs.Length > MaxCommandLineLength)
        {
            throw new ArgumentException($"Command line arguments cannot exceed {MaxCommandLineLength} characters", nameof(commandLineArgs));
        }

        var result = Kernel32Api.RegisterApplicationRestart(commandLineArgs, flags);

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to register application for restart");
        }
    }

    /// <summary>
    ///     Unregisters the current application from automatic restart.
    ///     Call this if you no longer want the application to be restarted by Restart Manager.
    /// </summary>
    /// <exception cref="Win32Exception">Thrown when unregistration fails.</exception>
    public static void UnregisterForRestart()
    {
        var result = Kernel32Api.UnregisterApplicationRestart();

        if (result != 0)
        {
            throw new Win32Exception(result, "Failed to unregister application from restart");
        }
    }

    /// <summary>
    ///     Checks if the current process was started by Restart Manager.
    ///     This allows the application to detect if it was automatically restarted after an update.
    /// </summary>
    /// <returns>True if the application was restarted by Restart Manager, false otherwise.</returns>
    /// <remarks>
    ///     This method checks if the application was started with the command-line arguments
    ///     that were registered via RegisterForRestart(). Applications should implement their own
    ///     restart detection based on the command-line arguments they register.
    ///     
    ///     For example, if you registered with "/restore", check for that specific argument:
    ///     <code>
    ///     var args = Environment.GetCommandLineArgs();
    ///     bool wasRestarted = args.Contains("/restore");
    ///     </code>
    /// </remarks>
    public static bool IsRestartRequested()
    {
        // When Restart Manager restarts an application, it passes the registered command-line arguments
        // Since we don't know what arguments the application registered with, we provide a helper
        // that checks for common restart indicators, but applications should implement their own logic
        var args = Environment.GetCommandLineArgs();
        
        // Check for common restart-related arguments
        // Applications should use their own specific arguments for more reliable detection
        return args.Any(arg => arg.Equals("/restart", StringComparison.OrdinalIgnoreCase) ||
                               arg.Equals("-restart", StringComparison.OrdinalIgnoreCase) ||
                               arg.Equals("--restart", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the command-line arguments that were passed to the current process.
    ///     Applications can use this to implement their own restart detection logic
    ///     based on the specific arguments they registered via RegisterForRestart().
    /// </summary>
    /// <returns>Array of command-line arguments, excluding the executable path.</returns>
    /// <example>
    ///     <code>
    ///     // If you registered with "/restore", check for it:
    ///     var args = ApplicationRestart.GetRestartCommandLineArgs();
    ///     if (args.Contains("/restore"))
    ///     {
    ///         // Application was restarted, restore state
    ///     }
    ///     </code>
    /// </example>
    public static string[] GetRestartCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();
        // Skip the first argument which is the executable path
        return args.Skip(1).ToArray();
    }
}
