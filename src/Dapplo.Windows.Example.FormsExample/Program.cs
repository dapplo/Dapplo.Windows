// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Log.Loggers;

namespace Dapplo.Windows.Example.FormsExample;

internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
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