using Dapplo.Windows.InstallerManager;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
namespace Dapplo.Windows.Example.InstallerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class InstallerWindow : Window
    {
        [DllImport("user32")]
        private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);
        private const string ExeToInstall = @"..\..\..\..\Dapplo.Windows.Example.FormsExample\bin\Debug\net480\Dapplo.Windows.Example.FormsExample.exe";
        public InstallerWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Start.IsEnabled = File.Exists(ExeToInstall);
        }

        private void AddLine(string line)
        {
            LogText.Inlines.Add(new Run(line));
            LogText.Inlines.Add(new LineBreak());
        }

        private void TryRestart()
        {
            using var session = InstallerRestartManager.CreateSession();
            session.RegisterFile(ExeToInstall);
            var processes = session.GetProcessesUsingResources();
            
            foreach (var process in processes)
            {
                AddLine($"Process {process.strAppName} (PID: {process.Process.dwProcessId}) is using the file, status: {process.AppStatus}");
            }
            try
            {
                session.Shutdown(Kernel32.Enums.RmShutdownType.RmShutdownOnlyRegistered, (progress) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        AddLine($"Shutdown progress {progress}");
                    });
                });
            }
            catch (Exception ex)
            {
                processes = session.GetProcessesUsingResources();
                foreach (var process in processes)
                {
                    AddLine($"Process {process.strAppName} (Status: {process.AppStatus})");
                }
                return;
            }
            session.Restart((progress) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    AddLine($"Restart progress {progress}");
                });
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TryRestart();
        }
    }
}
