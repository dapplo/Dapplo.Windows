// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using System;
using System.ComponentModel;
using System.Linq;
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
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string pwzCommandline, ApplicationRestartFlags dwFlags);

    /// <summary>
    ///     Removes the active instance of an application from the restart list.
    ///     See <a href="https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-unregisterapplicationrestart">UnregisterApplicationRestart function</a>
    /// </summary>
    /// <returns>
    ///     Returns S_OK (0) on success, or an error value on failure.
    /// </returns>
    [DllImport("kernel32.dll")]
    private static extern int UnregisterApplicationRestart();

    /// <summary>
    ///     Maximum length for the command line arguments (in characters).
    ///     This corresponds to the Windows RESTART_MAX_CMD_LINE constant.
    /// </summary>
    public const int RestartMaxCmdLine = 1024;

    /// <summary>
    ///     Registers the current application for automatic restart.
    ///     When the Restart Manager shuts down the application during an update, it will be automatically restarted afterwards.
    /// </summary>
    /// <remarks>
    ///     A restart after a crash or hang (unless <see cref="ApplicationRestartFlags"/> excludes it) only happens when the process was running for at least 60 seconds.
    /// </remarks>
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
        if (!string.IsNullOrEmpty(commandLineArgs) && commandLineArgs.Length > RestartMaxCmdLine)
        {
            throw new ArgumentException($"Command line arguments cannot exceed {RestartMaxCmdLine} characters", nameof(commandLineArgs));
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
    ///     Checks if the current process was started with the specified command-line argument.
    ///     Register for restart with an argument which is only used for this, e.g. <c>RegisterForRestart("/restore")</c>,
    ///     and check for that argument on startup with <c>WasRestartRequested("/restore")</c>.
    /// </summary>
    /// <param name="restartArgument">One of the arguments which were passed to <see cref="RegisterForRestart"/>, compared case-insensitive</param>
    /// <returns>True if the current process was started with the argument</returns>
    /// <remarks>
    ///     Windows cannot tell a process that it was restarted, this only checks the command line: a manual start with the same argument also returns true.
    /// </remarks>
    /// <exception cref="ArgumentException">When restartArgument is null or empty</exception>
    public static bool WasRestartRequested(string restartArgument)
    {
        if (string.IsNullOrEmpty(restartArgument))
        {
            throw new ArgumentException("The restart argument must be specified", nameof(restartArgument));
        }
        return GetRestartCommandLineArgs().Any(arg => arg.Equals(restartArgument, StringComparison.OrdinalIgnoreCase));
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
    ///     var args = ApplicationRestartManager.GetRestartCommandLineArgs();
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
    ///     An observable stream of the WM_QUERYENDSESSION and WM_ENDSESSION messages which the shared message window receives,
    ///     this allows applications to be notified when the session ends (shutdown, restart, log off) or the Restart Manager wants the application to close.
    /// </summary>
    /// <remarks>
    ///     Every call returns an independent subscription source, all subscribers see every message.
    ///     OnNext is called synchronously on the thread of the shared message window, WM_QUERYENDSESSION can be answered with
    ///     <see cref="EndSessionMessage.CanEndSession"/> or <see cref="EndSessionMessage.Veto"/> from within OnNext.
    ///     If nobody answers, the session end is allowed.
    ///     For WM_ENDSESSION with <see cref="EndSessionMessage.IsSessionEnding"/> the process can be terminated at any time after OnNext returned,
    ///     save state synchronously and do not rely on marshalling to the UI thread.
    /// </remarks>
    /// <returns>IObservable of EndSessionMessage</returns>
    public static IObservable<EndSessionMessage> ListenForEndSession()
    {
        return SharedMessageWindow.Messages
            .Where(windowMessage => windowMessage.Msg == WindowsMessages.WM_QUERYENDSESSION || windowMessage.Msg == WindowsMessages.WM_ENDSESSION)
            .Select(windowMessage =>
            {
                if (windowMessage.Msg == WindowsMessages.WM_ENDSESSION)
                {
                    // The query is over (it was either cancelled or the session ends), remove a block reason which was set by EndSessionMessage.Veto
                    EndSessionMessage.RemoveBlockReason(windowMessage.Hwnd);
                }
                return new EndSessionMessage(windowMessage);
            });
    }
}
