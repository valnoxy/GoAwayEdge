using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using GoAwayEdge.Common;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class Installation : UserControl
    {
        private BackgroundWorker applyBackgroundWorker;

        public Installation()
        {
            InitializeComponent();

            // Background worker for deployment
            applyBackgroundWorker = new BackgroundWorker();
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
            applyBackgroundWorker.ProgressChanged += applyBackgroundWorker_ProgressChanged;
            applyBackgroundWorker.RunWorkerAsync();
        }

        private void Install(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // Create installation directory
            var InstDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");
            Directory.CreateDirectory(InstDir);

            bool status = false;

            if (Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) == InstDir)
                status = CopyItself(Path.Combine(InstDir, "GoAwayEdge.exe"), false);
            else status = CopyItself(Path.Combine(InstDir, "GoAwayEdge.exe"), true);
            
            if (status == false)
            {
                MessageBox.Show("Installation failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Apply IFEO registry key
            string msEdge = "";
            string engine = "";
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
                key.CreateSubKey("msedge.exe");
                key = key.OpenSubKey("msedge.exe", true);
                key.SetValue("UseFilter", 1, RegistryValueKind.DWord);

                key.CreateSubKey("0");
                key = key.OpenSubKey("0", true);
                key.SetValue("Debugger", Path.Combine(InstDir, $"GoAwayEdge.exe -se{engine}"));
                key.SetValue("FilterFullPath", msEdge);

                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Switch FrameWindow content to InstallationSuccess
            worker.ReportProgress(100, "");
        }

        private void Uninstall(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // Remove installation directory
            var InstDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");

            bool status = false;

            try
            {
                var dir = new DirectoryInfo(InstDir);
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
                key.DeleteSubKeyTree("msedge.exe");
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uninstallation failed! Please try again.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Switch FrameWindow content to InstallationSuccess
            worker.ReportProgress(100, "");
        }


        private void applyBackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 100)
            {
                Installer.ContentWindow.FrameWindow.NavigationService.Navigate(new InstallationSuccess());
            }
        }

        public static bool CopyItself(string pathTo, bool overwrite = false)
        {
            string fileName = String.Concat(Process.GetCurrentProcess().ProcessName, ".exe");
            string filePath = Path.Combine(Environment.CurrentDirectory, fileName);
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
