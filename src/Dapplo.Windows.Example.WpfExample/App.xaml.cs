// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Windows;
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Dapplo.Windows.Dpi;

namespace Dapplo.Windows.Example.WpfExample
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
            NativeDpiMethods.EnableDpiAware();
            base.OnStartup(e);
        }
    }
}