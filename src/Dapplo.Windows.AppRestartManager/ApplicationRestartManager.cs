// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Structs;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.AppRestartManager;

/// <summary>
/// Provides methods for registering and unregistering the current application for automatic restart using Windows
/// Restart Manager, as well as utilities for detecting restart events and handling system shutdown notifications.
/// </summary>
/// <remarks>Use this class to enable your application to be automatically restarted after system updates or
/// shutdowns managed by Windows Restart Manager. It also provides helper methods for detecting restart conditions and
/// responding to session end events, allowing applications to preserve state and handle shutdowns gracefully.</remarks>
public static class ApplicationRestartManager
{
    private static IObservable<EndSessionMessage> _endSessionObservable;

    /// <summary>
    ///     Registers the active instance of an application for restart.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-registerapplicationrestart">RegisterApplicationRestart function</a>
    /// </summary>
    /// <param name="pwzCommandline">
    ///     A pointer to a Unicode string that specifies the command-line arguments for the application when it is restarted.
    ///     The maximum size of the command line that you can specify is RESTART_MAX_CMD_LINE characters.
    ///     Do not include the name of the executable in the command line; this function adds it for you.
    ///     If this parameter is NULL or an empty string, the previously registered command line is removed.
    ///     If the argument contains spaces, use quotes around the argument.
    /// </param>
    /// <param name="dwFlags">
    ///     This parameter can be 0 or one or more of the ApplicationRestartFlags values.
    /// </param>
    /// <returns>
    ///     Returns S_OK (0) on success, or an error value on failure.
    /// </returns>
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string pwzCommandline, ApplicationRestartFlags dwFlags);

    /// <summary>
    ///     Removes the active instance of an application from the restart list.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-unregisterapplicationrestart">UnregisterApplicationRestart function</a>
    /// </summary>
    /// <returns>
    ///     Returns S_OK (0) on success, or an error value on failure.
    /// </returns>
    [DllImport("kernel32")]
    private static extern int UnregisterApplicationRestart();

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

        var result = RegisterApplicationRestart(commandLineArgs, flags);

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
        var result = UnregisterApplicationRestart();

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
    public static bool WasRestartRequested()
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
    ///     var args = RestartManager.GetRestartCommandLineArgs();
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

    /// <summary>
    ///     Creates an observable stream that listens for WM_QUERYENDSESSION and WM_ENDSESSION messages.
    ///     This allows applications to be notified when the system is about to shut down or restart.
    /// </summary>
    /// <returns>
    ///     An observable stream that emits EndSessionMessage values when a session end event occurs.
    ///     Subscribe to this observable to handle shutdown requests gracefully.
    /// </returns>
    public static IObservable<EndSessionMessage> ListenForEndSession()
    {
        return _endSessionObservable ??= Observable.Create<EndSessionMessage>(observer =>
        {
            // Subscribe to the SharedMessageWindow for handling the WM_INPUT
            var messageSubscription = SharedMessageWindow.Messages
                .Where(windowsMessage => windowsMessage.Msg.IsIn(WindowsMessages.WM_QUERYENDSESSION, WindowsMessages.WM_ENDSESSION)) // filter for raw input
                .Subscribe(windowsMessage =>
                {
                    var endSessionMessage = new EndSessionMessage(windowsMessage.Msg, (EndSessionReasons)windowsMessage.LParam);
                    observer.OnNext(endSessionMessage);
                    if (endSessionMessage.Handled)
                    {
                        windowsMessage.Handled = true;
                    }
                });

            // Return the disposal logic
            return Disposable.Create(() =>
            {
                // Clear the cached observable so it can be recreated if Listen() is called again.
                _endSessionObservable = null; 
                // Dispose the SharedMessageWindow subscription
                messageSubscription.Dispose();
            });
        })
        // This is the magic part:
        // .Publish() multicasts the observable to all subscribers.
        // .RefCount() keeps track of subscribers and automatically disposes 
        // the inner subscription when the count reaches 0.
        .Publish()
        .RefCount();
    }
}
