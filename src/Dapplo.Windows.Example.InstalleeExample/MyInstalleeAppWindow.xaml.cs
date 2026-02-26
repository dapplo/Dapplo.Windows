using Dapplo.Windows.AppRestartManager;
using System.Windows;
using System.Windows.Documents;
using System;
using System.Diagnostics;

namespace Dapplo.Windows.Example.InstalleeExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void AddLine(string line)
        {
            LogText.Inlines.Add(new Run(line));
            LogText.Inlines.Add(new LineBreak());
        }
        public MainWindow()
        {
            InitializeComponent();
            SetupRestart();
        }

        private void SetupRestart()
        {
            ApplicationRestartManager.ListenForEndSession().Subscribe(endSessionMessage =>
            {
                var logLine = $"{endSessionMessage.Msg} with session reason: {endSessionMessage.EndSessionReason}";
                Debug.WriteLine(logLine);
                this.Dispatcher.BeginInvoke(() =>
                {
                    AddLine(logLine);
                });
                endSessionMessage.Handled = true;
                if (endSessionMessage.Msg == Messages.Enumerations.WindowsMessages.WM_ENDSESSION)
                {
                    logLine = "Shutting down application...";
                    Debug.WriteLine(logLine);
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        AddLine(logLine);
                        Application.Current.Shutdown();
                    });
                }
            });

        }
    }
}
