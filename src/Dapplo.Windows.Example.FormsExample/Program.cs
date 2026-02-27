// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Dapplo.Windows.Example.FormsExample;

internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--restart"))
        {
            MessageBox.Show("Application restarted by Windows Restart Manager, exiting now.", "Restarted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SharedMessageWindow.Listen().Subscribe(message =>
        {
            Debug.WriteLine($"Received windows message {message.Msg}");
        });
        ApplicationRestartManager.RegisterForRestart("--restart");
        ApplicationRestartManager.ListenForEndSession(
                onQuerySession: (endSessionReason) => {
                    return true;
                },
                onEndSession: (endSessionReason) =>
                {
                    Debug.WriteLine($"Shutting down application due to {endSessionReason}");
                    Application.Exit();
                    return true;
                }
                ).Subscribe(endSessionMessage =>
                {
                    Debug.WriteLine($"{endSessionMessage.Msg} with session reason: {endSessionMessage.EndSessionReason}");
                });

        LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var formDpiUnaware = new FormDpiUnaware();
        formDpiUnaware.Show();
        var formWithAttachedDpiHandler = new FormWithAttachedDpiHandler();
        formWithAttachedDpiHandler.Show();
        var formExtendsDpiAwareForm = new FormExtendsDpiAwareForm();
        formExtendsDpiAwareForm.Show();
        var webBrowserForm = new WebBrowserForm();
        webBrowserForm.Show();
        Application.Run();
    }
}