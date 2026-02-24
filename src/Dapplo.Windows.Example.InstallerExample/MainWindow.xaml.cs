using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dapplo.Windows.InstallerManager;

namespace Dapplo.Windows.Example.InstallerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PopulateList();
        }

        private void PopulateList()
        {
            using (var session = InstallerRestartManager.CreateSession())
            {
                session.RegisterFile(@"D:\code\greenshot-restartmanager\src\Greenshot\bin\Debug\net480\Greenshot.exe");
                var processes = session.GetProcessesUsingResources();
         
                foreach (var process in processes)
                {
                    Debug.WriteLine($"Process {process.strAppName} (PID: {process.Process.dwProcessId}) is using the file");
                }
                session.Shutdown(Kernel32.Enums.RmShutdownType.RmShutdownOnlyRegistered, (progress) =>
                {
                    Debug.WriteLine($"Shutdown progress {progress}");
                });
                session.Restart((progress) =>
                {
                    Debug.WriteLine($"Restart progress {progress}");
                });
            }
        }
    }
}
