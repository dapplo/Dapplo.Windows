using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.Messages;
using System;
using System.Diagnostics;
using System.Windows;

namespace Dapplo.Windows.Example.InstalleeExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            SharedMessageWindow.Listen().Subscribe(message =>
            {
                Debug.WriteLine($"Received windows message {message.Msg}");
            });
            ApplicationRestartManager.RegisterForRestart("restart");
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }
    }
}
