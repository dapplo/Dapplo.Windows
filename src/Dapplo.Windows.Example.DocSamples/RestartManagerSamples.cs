// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.InstallerManager;
using Dapplo.Windows.InstallerManager.Enums;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/restart-manager.md, wiki/Restart-Manager.md
/// </summary>
public static class RestartManagerSamples
{
    public static void Register()
    {
        #region Register
        // Early in Main: restart me with "/restore" when an installer or Windows Update closes me.
        // Don't include the executable, Windows adds it. At most RestartMaxCmdLine (1024) characters.
        ApplicationRestartManager.RegisterForRestart("/restore");

        // Windows can't tell a process that it was restarted, check for your own argument instead
        // (a manual start with the same argument returns true too)
        if (ApplicationRestartManager.WasRestartRequested("/restore"))
        {
            RestoreDocuments();
        }
        #endregion
    }

    public static void RegisterFlags()
    {
        #region RegisterFlags
        // Restart after an update, but not after a crash or hang (those restarts need 60 seconds of uptime anyway)
        ApplicationRestartManager.RegisterForRestart("/restore",
            ApplicationRestartFlags.RestartNoCrash | ApplicationRestartFlags.RestartNoHang);

        // No automatic restart anymore
        ApplicationRestartManager.UnregisterForRestart();
        #endregion
    }

    public static void EndSession()
    {
        #region EndSession
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
        #endregion
    }

    public static void FindLockingProcesses()
    {
        #region FindLockingProcesses
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
        #endregion
    }

    public static void UpdateFiles(string installDirectory, string newFilesDirectory)
    {
        #region UpdateFiles
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
            // Only when you must: RmShutdownType.Force kills unresponsive applications, which can lose data
            return;
        }

        foreach (var file in Directory.GetFiles(newFilesDirectory))
        {
            File.Copy(file, Path.Combine(installDirectory, Path.GetFileName(file)), overwrite: true);
        }

        // Restart the applications which registered for restart (RegisterApplicationRestart)
        session.Restart();
        #endregion
    }

    public static void Services()
    {
        #region Services
        using var session = InstallerRestartManager.CreateSession();
        // Services are registered by their short name, stopping them needs administrator rights
        session.RegisterServices("Spooler");
        var affected = session.GetProcessesUsingResources();
        Console.WriteLine($"Affected: {string.Join(", ", affected.Select(p => p.AppName))}");
        #endregion
    }

    private static bool HasUnsavedWork() => false;
    private static void SaveState() { }
    private static void RestoreDocuments() { }
}
