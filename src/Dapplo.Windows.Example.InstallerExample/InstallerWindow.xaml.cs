using Dapplo.Windows.InstallerManager;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
namespace Dapplo.Windows.Example.InstallerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class InstallerWindow : Window
    {
        private const string FormsExampleProject = "Dapplo.Windows.Example.FormsExample";
        private const string FormsExampleExe = FormsExampleProject + ".exe";
        private readonly string _exeToInstall;

        public InstallerWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            _exeToInstall = FindExeToInstall();
            Start.IsEnabled = _exeToInstall != null;
            AddLine(_exeToInstall != null
                ? $"Using {_exeToInstall}, start it and press Install."
                : $"Couldn't find {FormsExampleExe}: build the FormsExample, or pass the path of the exe as the first command line argument.");
        }

        /// <summary>
        /// Find the FormsExample executable: the first command line argument, or the build output of the FormsExample project,
        /// which is searched by walking up from the directory of this application to the src directory.
        /// </summary>
        /// <returns>full path of the exe, or null when it can't be found</returns>
        private static string FindExeToInstall()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                return Path.GetFullPath(args[1]);
            }

            // e.g. ...\Dapplo.Windows.Example.InstallerExample\bin\Debug\net480
            var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var directory = baseDirectory;
            while (directory != null)
            {
                var projectBin = Path.Combine(directory.FullName, FormsExampleProject, "bin");
                if (Directory.Exists(projectBin))
                {
                    // Prefer the same configuration and target framework as this application, otherwise take the newest build
                    var configuration = baseDirectory.Parent?.Name;
                    var targetFramework = baseDirectory.Name;
                    var preferred = configuration == null ? null : Path.Combine(projectBin, configuration, targetFramework, FormsExampleExe);
                    if (preferred != null && File.Exists(preferred))
                    {
                        return preferred;
                    }
                    return Directory.EnumerateFiles(projectBin, FormsExampleExe, SearchOption.AllDirectories)
                        .OrderByDescending(file => File.GetLastWriteTimeUtc(file))
                        .FirstOrDefault();
                }
                directory = directory.Parent;
            }
            return null;
        }

        private void AddLine(string line)
        {
            LogText.Inlines.Add(new Run(line));
            LogText.Inlines.Add(new LineBreak());
        }

        private void TryRestart()
        {
            using var session = InstallerRestartManager.CreateSession();
            session.RegisterFile(_exeToInstall);
            var processes = session.GetProcessesUsingResources();
            
            foreach (var process in processes)
            {
                AddLine($"Process {process.AppName} (PID: {process.Process.ProcessId}) is using the file, status: {process.AppStatus}");
            }
            try
            {
                // Only shut down when all applications are registered for restart, the default (Graceful) would also ask unregistered applications
                session.Shutdown(Dapplo.Windows.InstallerManager.Enums.RmShutdownType.OnlyRegistered, (progress) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        AddLine($"Shutdown progress {progress}");
                    });
                });
            }
            catch (Win32Exception ex)
            {
                AddLine($"Shutdown failed: {ex.Message} (error {ex.NativeErrorCode})");
                processes = session.GetProcessesUsingResources();
                foreach (var process in processes)
                {
                    AddLine($"Process {process.AppName} (Status: {process.AppStatus})");
                }
                return;
            }
            try
            {
                session.Restart((progress) =>
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        AddLine($"Restart progress {progress}");
                    });
                });
            }
            catch (Win32Exception ex)
            {
                AddLine($"Restart failed: {ex.Message} (error {ex.NativeErrorCode})");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TryRestart();
        }
    }
}
