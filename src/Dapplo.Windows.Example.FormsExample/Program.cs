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
using System.Threading;
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

        // The end session messages arrive on the thread of the shared message window, not on the UI thread
        var uiContext = SynchronizationContext.Current;
        ApplicationRestartManager.ListenForEndSession().Subscribe(endSessionMessage =>
        {
            Debug.WriteLine($"Received {endSessionMessage}");
            if (endSessionMessage.IsQuery)
            {
                // Answer synchronously, this is the default and could be left out. Use endSessionMessage.Veto("reason") to block.
                endSessionMessage.CanEndSession = true;
                return;
            }
            if (endSessionMessage.IsSessionEnding)
            {
                // Save state here, synchronously: the process can be terminated as soon as this returns
                Debug.WriteLine($"Shutting down application due to {endSessionMessage.EndSessionReason}");
                uiContext?.Post(_ => Application.Exit(), null);
            }
        });
        Application.Run();
    }
}