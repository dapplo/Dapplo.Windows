//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Log.Loggers;

#endregion

namespace Dapplo.Windows.Example.FormsExample
{
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
}