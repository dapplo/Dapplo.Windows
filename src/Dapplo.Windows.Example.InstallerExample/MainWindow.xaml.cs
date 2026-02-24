using Dapplo.Windows.InstallerManager;
using Dapplo.Windows.Messages;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
namespace Dapplo.Windows.Example.InstallerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32")]
        private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

        public MainWindow()
        {
            InitializeComponent();
            PopulateList();
        }

        private void PopulateList()
        {
            using var session = InstallerRestartManager.CreateSession();
            session.RegisterFile(@"D:\code\greenshot-restartmanager\src\Greenshot\bin\Debug\net480\Greenshot.exe");
            var processes = session.GetProcessesUsingResources();

            foreach (var process in processes)
            {
                Debug.WriteLine($"Process {process.strAppName} (PID: {process.Process.dwProcessId}) is using the file, status: {process.AppStatus}");
            }
            try
            {
                session.Shutdown(Kernel32.Enums.RmShutdownType.RmShutdownOnlyRegistered, (progress) =>
                {
                    Debug.WriteLine($"Shutdown progress {progress}");
                });
            }
            catch (Exception ex)
            {
                processes = session.GetProcessesUsingResources();
                foreach (var process in processes)
                {
                    Debug.WriteLine($"Process {process.strAppName} (Status: {process.AppStatus})");
                }
                return;
            }
            session.Restart((progress) =>
            {
                Debug.WriteLine($"Restart progress {progress}");
            });
        }
    }
}
