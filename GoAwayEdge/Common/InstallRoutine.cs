﻿using Microsoft.Win32.TaskScheduler;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GoAwayEdge.Common
{
    public class InstallRoutine
    {
        public static void Install(object? sender, DoWorkEventArgs? e = null)
        {
            var worker = sender as BackgroundWorker;

            Console.WriteLine("Start installation ...");

            // Create installation directory
            Directory.CreateDirectory(Configuration.InstallDir);

            var status = CopyItself(Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) != Configuration.InstallDir);
            if (!status)
            {
                MessageBox.Show("Installation failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Apply IFEO registry key
            var msEdge = "";
            msEdge = Configuration.Channel switch
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

            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
                key?.CreateSubKey("msedge.exe");
                key = key?.OpenSubKey("msedge.exe", true);
                key?.SetValue("UseFilter", 1, RegistryValueKind.DWord);

                key?.CreateSubKey("0");
                key = key?.OpenSubKey("0", true);
                key?.SetValue("Debugger", Path.Combine(Configuration.InstallDir, $"GoAwayEdge.exe -se:{Configuration.Search}"));
                key?.SetValue("FilterFullPath", msEdge);

                if (Configuration.Search == SearchEngine.Custom)
                {
                    if (Configuration.CustomQueryUrl != null)
                        key?.SetValue("CustomQueryUrl", Configuration.CustomQueryUrl);
                }
                else
                {
                    try
                    {
                        key?.DeleteValue("CustomQueryUrl");
                    }
                    catch
                    {
                        // ignore: never used custom search engine
                    }
                }

                key?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            if (Configuration.UninstallEdge)
            {
                // TODO: highly wip
                var summaryRemoval = Removal.RemoveMsEdge();
                if (!summaryRemoval)
                {
                    MessageBox.Show("Removal of Microsoft Edge failed! Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }
            }
            else
            {
                // Create non-IFEO injected copy of msedge.exe
                try
                {
                    File.Copy(msEdge, Path.Combine(Path.GetDirectoryName(msEdge)!, "msedge_ifeo.exe"), true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Installation failed!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }
            }

            // Create Task Schedule for IFEO update 
            try
            {
                var ts = new TaskService();
                var td = ts.NewTask();
                td.RegistrationInfo.Description = "Checks validation between Edge and IFEO binary.";
                td.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromMinutes(5) });
                td.Actions.Add(new ExecAction(Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), "--update", Configuration.InstallDir));
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
            Console.WriteLine("Installation finished.");
        }

        public static void Uninstall(object? sender, DoWorkEventArgs? e = null)
        {
            var worker = sender as BackgroundWorker;

            Console.WriteLine("Start uninstallation ...");

            // Remove installation directory
            try
            {
                var dir = new DirectoryInfo(Configuration.InstallDir);
                dir.Delete(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uninstallation failed! Please try again.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
                return;
            }

            // Remove Ifeo from registry
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
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

            // Remove URI handler and Edge Update block from registry
            var defaultEdgePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft", "Edge", "Application");
            if (!Directory.Exists(defaultEdgePath))
            {
                try
                {
                    var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft", true);
                    key?.DeleteSubKeyTree("EdgeUpdate");
                    key?.Close();

                    key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes", true);
                    key?.DeleteSubKeyTree("microsoft-edge");
                    key?.DeleteSubKeyTree("EdgeHTM");
                    key?.Close();
                }
                catch
                {
                    // ignore
                }
            }

            // Clean up Task Scheduler
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
            Console.WriteLine("Uninstallation finished.");
        }

        internal static bool CopyItself(string pathTo, bool overwrite = false)
        {
            var fileName = string.Concat(Process.GetCurrentProcess().ProcessName, ".exe");
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            try
            {
                if (overwrite) File.Copy(filePath, pathTo, true);

                else if (!File.Exists(pathTo))
                {
                    File.Copy(filePath, pathTo);
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