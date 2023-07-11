using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Path = System.IO.Path;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class Installation : UserControl
    {
        public Installation()
        {
            InitializeComponent();

            // Background worker for deployment
            var applyBackgroundWorker = new BackgroundWorker();
            applyBackgroundWorker.WorkerReportsProgress = true;
            applyBackgroundWorker.WorkerSupportsCancellation = true;
            if (Common.Configuration.Uninstall)
            {
                applyBackgroundWorker.DoWork += Uninstall;
            }
            else
            {
                applyBackgroundWorker.DoWork += Install;
            }
            applyBackgroundWorker.ProgressChanged += ApplyBackgroundWorker_ProgressChanged;
            applyBackgroundWorker.RunWorkerAsync();
        }

        private static void Install(object? sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            // Create installation directory
            var instDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");
            Directory.CreateDirectory(instDir);

            var status = CopyItself(Path.Combine(instDir, "GoAwayEdge.exe"), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) != instDir);
            if (status == false)
            {
                MessageBox.Show("Installation failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Apply IFEO registry key
            var msEdge = "";
            var engine = "";
            msEdge = Common.Configuration.Channel switch
            {
                EdgeChannel.Stable => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                EdgeChannel.Beta => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Beta", "Application", "msedge.exe"),
                EdgeChannel.Dev => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Dev", "Application", "msedge.exe"),
                EdgeChannel.Canary => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Canary", "Application", "msedge.exe"),
                _ => msEdge
            };

            engine = Common.Configuration.Search switch
            {
                SearchEngine.Ask => "Ask",
                SearchEngine.Bing => "Bing",
                SearchEngine.DuckDuckGo => "DuckDuckGo",
                SearchEngine.Ecosia => "Ecisua",
                SearchEngine.Google => "Google",
                SearchEngine.Yahoo => "Yahoo",
                SearchEngine.Yandex => "Yandex",
                _ => engine
            };

            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
                key?.CreateSubKey("msedge.exe");
                key = key?.OpenSubKey("msedge.exe", true);
                key?.SetValue("UseFilter", 1, RegistryValueKind.DWord);

                key?.CreateSubKey("0");
                key = key?.OpenSubKey("0", true);
                key?.SetValue("Debugger", Path.Combine(instDir, $"GoAwayEdge.exe -se{engine}"));
                key?.SetValue("FilterFullPath", msEdge);

                key?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Create non-IFEO injected copy of msedge.exe
            try
            {
                File.Copy(msEdge, Path.Combine(Path.GetDirectoryName(msEdge),"msedge_ifeo.exe"), true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Create Task Schedule for IFEO update 
            try
            {
                var ts = new TaskService();
                var td = ts.NewTask();
                td.RegistrationInfo.Description = "Checks validation between Edge and IFEO binary.";
                td.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromMinutes(5) });
                td.Actions.Add(new ExecAction(Path.Combine(instDir, "GoAwayEdge.exe"), "--update", instDir));
                ts.RootFolder.RegisterTaskDefinition(@"valnoxy\GoAwayEdge\GoAwayEdge IFEO Validation", td);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }


            // Switch FrameWindow content to InstallationSuccess
            worker?.ReportProgress(100, "");
        }

        private static void Uninstall(object? sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            // Remove installation directory
            var instDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");

            try
            {
                var dir = new DirectoryInfo(instDir);
                dir.Delete(true);
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Uninstallation failed! Please try again.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }
            
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
                key?.DeleteSubKeyTree("msedge.exe");
                key?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uninstallation failed! Please try again.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            try
            {
                var ts = new TaskService();
                ts.RootFolder.DeleteTask("valnoxy\\GoAwayEdge\\GoAwayEdge IFEO Validation");
            }
            catch
            {
                // ignore
            }

            // Switch FrameWindow content to InstallationSuccess
            worker?.ReportProgress(100, "");
        }

        private static void ApplyBackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 100)
            {
                Installer.ContentWindow?.FrameWindow.NavigationService.Navigate(new InstallationSuccess());
            }
        }

        public static bool CopyItself(string pathTo, bool overwrite = false)
        {
            var fileName = string.Concat(Process.GetCurrentProcess().ProcessName, ".exe");
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            try
            {
                if (overwrite) System.IO.File.Copy(filePath, pathTo, true);

                else if (!File.Exists(pathTo))
                {
                    System.IO.File.Copy(filePath, pathTo);
                }
            }

            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }

            return true;
        }

    }
}
