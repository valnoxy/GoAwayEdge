using Microsoft.Win32.TaskScheduler;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;
using System.Reflection;
using GoAwayEdge.Common.Installation;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge.Common
{
    public partial class InstallRoutine
    {
        // Shell update for uninstallation
        public partial class Shell32
        {
            [LibraryImport("shell32.dll", SetLastError = true)]
            public static partial void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        }

        public static void Install(object? sender, DoWorkEventArgs? e = null)
        {
            var worker = sender as BackgroundWorker;

            Console.WriteLine("Start installation ...");
            Logging.Log("Start installation ...");

            // Create installation directory
            Directory.CreateDirectory(Configuration.InstallDir);

            var status = CopyItself(Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) != Configuration.InstallDir);
            if (!status)
            {
                Logging.Log("Failed to copy GoAwayEdge.exe to installation directory.", Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedInstallationGeneric");
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Apply registry key
            var msEdge = Configuration.Channel switch
            {
                EdgeChannel.Stable => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                EdgeChannel.Beta => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Beta", "Application", "msedge.exe"),
                EdgeChannel.Dev => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Dev", "Application", "msedge.exe"),
                EdgeChannel.Canary => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge Canary", "Application", "msedge.exe"),
                _ => ""
            };

            try
            {
                RegistryConfig.SetKey("EdgeChannel", Configuration.Channel.ToString());
                RegistryConfig.SetKey("EdgeFilePath", msEdge);
                RegistryConfig.SetKey("SearchEngine", Configuration.Search);
                if (Configuration.Search == SearchEngine.Custom)
                {
                    if (Configuration.CustomQueryUrl != null)
                        RegistryConfig.SetKey("CustomQueryUrl", Configuration.CustomQueryUrl);
                }
                else
                {
                    RegistryConfig.RemoveKey("CustomQueryUrl");
                }

                status = Register.ImageFileExecutionOption(
                    Register.IfeoType.msedge, 
                    Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"),
                    msEdge);
                if (!status)
                {
                    Logging.Log("Failed to register msedge.exe in Image File Execution Options.", Logging.LogLevel.ERROR);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedRegisterIFEO", "msedge.exe");
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                    Environment.Exit(1);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to apply registry keys: " + ex, Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedInstallation", ex.Message);
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Kill Microsoft Edge processes
            KillProcess("msedge");
            KillProcess("msedge_non_ifeo");

            if (Configuration.UninstallEdge)
            {
                FileConfiguration.EdgePath = msEdge;
                status = Removal.RemoveMsEdge();
                if (!status)
                {
                    Logging.Log("Failed to remove Microsoft Edge.");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedEdgeRemoval");
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                    Environment.Exit(1);
                    return;
                }
            }
            else if (!Configuration.NoEdgeInstalled)
            {
                // Create non-IFEO injected copy of msedge.exe
                try
                {
                    File.Copy(msEdge, Path.Combine(Path.GetDirectoryName(msEdge)!, "msedge_non_ifeo.exe"), true);
                    RegistryConfig.SetKey("EdgeNonIEFOFilePath", Path.Combine(Path.GetDirectoryName(msEdge)!, "msedge_non_ifeo.exe"));
                }
                catch (Exception ex)
                {
                    Logging.Log("Failed to create non-IFEO injected copy of msedge.exe: " + ex, Logging.LogLevel.ERROR);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedInstallation", ex.Message);
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                    Environment.Exit(1);
                    return;
                }
            }

            // Delete old tasks & create new task schedule for Update & Validation
            try
            {
                var ts = new TaskService();
                try
                {
                    ts.RootFolder.DeleteTask("valnoxy\\GoAwayEdge\\GoAwayEdge IFEO Validation");
                }
                catch (FileNotFoundException)
                {
                    Logging.Log("Old Task not found, skipping ...");
                }
                catch (Exception ex)
                {
                    Logging.Log("Failed to delete old task: " + ex, Logging.LogLevel.ERROR);
                }

                var td = ts.NewTask();
                td.RegistrationInfo.Description = "Checks for new versions and validates non-IEFO file.";
                td.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromMinutes(5) });
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Actions.Add(new ExecAction(Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), "--update", Configuration.InstallDir));
                ts.RootFolder.RegisterTaskDefinition(@"valnoxy\GoAwayEdge\GoAwayEdge Validation & Update Task", td);
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to create task schedule: " + ex, Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedInstallation", ex.Message);
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Register Uninstall data
            status = SetUninstallData();
            if (!status)
            {
                Logging.Log("Failed to set uninstall data.", Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedInstallationGeneric");
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Create Shortcut for Control Panel
            if (Configuration.InstallControlPanel)
            {
                var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs", "GoAwayEdge.lnk");
                Shortcut.Create(Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), "--control-panel", shortcutPath);
                RegistryConfig.SetKey("ControlPanelIsInstalled", true);
            }

            // Set enable flag
            RegistryConfig.SetKey("Enabled", true);
            RegistryConfig.SetKey("AiProvider", "Copilot", userSetting: true); // Temporary

            // Switch FrameWindow content to InstallationSuccess
            worker?.ReportProgress(100, "");
            Logging.Log("Installation finished.");
            Console.WriteLine("Installation finished.");
        }

        public static void Uninstall(object? sender, DoWorkEventArgs? e = null)
        {
            var worker = sender as BackgroundWorker;
            Logging.Log("Start uninstallation ...");
            Console.WriteLine("Start uninstallation ...");

            // Check if run in installation directory
            if (Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) == Configuration.InstallDir)
            {
                // Copy itself to temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), "GoAwayEdge");
                Directory.CreateDirectory(tempDir);
                var status = CopyItself(Path.Combine(tempDir, "GoAwayEdge.exe"), true);
                if (!status)
                {
                    Logging.Log("Failed to copy GoAwayEdge.exe to temp directory.", Logging.LogLevel.ERROR);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedUninstallation");
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                    Environment.Exit(1);
                    return;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(tempDir, "GoAwayEdge.exe"),
                    Arguments = "-u",
                    UseShellExecute = true
                });
                Environment.Exit(0);
            }

            // Remove installation directory
            try
            {
                var dir = new DirectoryInfo(Configuration.InstallDir);
                dir.Delete(true);
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to delete installation directory: " + ex, Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedUninstallation", ex.Message);
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Remove Ifeo & Uri handler from registry
            try
            {
                if (Configuration.NoEdgeInstalled)
                {
                    // Image File Execution Options
                    var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
                    key?.DeleteSubKeyTree("ie_to_edge_stub.exe");
                    key?.Close();

                    // Uri Handler
                    key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes", true);
                    key?.DeleteSubKeyTree("MSEdgeHTM");
                    key?.DeleteSubKeyTree("microsoft-edge");
                    key?.Close();
                }
                else // Default installation
                {
                    var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
                    key?.DeleteSubKeyTree("msedge.exe");
                    key?.Close();
                }

                // General infos about GoAwayEdge
                var generalKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\valnoxy", true);
                generalKey?.DeleteSubKeyTree("GoAwayEdge");
                generalKey?.Close();
                
                var currentUserKey = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\valnoxy", true);
                currentUserKey?.DeleteSubKeyTree("GoAwayEdge");
                currentUserKey?.Close();
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to remove registry keys: " + ex, Logging.LogLevel.ERROR);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedUninstallation", ex.Message);
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
                Environment.Exit(1);
                return;
            }

            // Clean up Task Scheduler
            try
            {
                var ts = new TaskService();
                try
                {
                    ts.RootFolder.DeleteTask("valnoxy\\GoAwayEdge\\GoAwayEdge IFEO Validation");
                }
                catch (Exception ex)
                {
                    Logging.Log("Failed to delete old task: " + ex);
                    // ignored; not existing
                }
                ts.RootFolder.DeleteTask("valnoxy\\GoAwayEdge\\GoAwayEdge Validation & Update Task");
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to delete old task: " + ex);
            }

            // Remove Uninstall data
            try
            {
                RegistryConfig.RemoveSubKey(RegistryConfig.UninstallGuid, true);
                Shell32.SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero); // Notify Shell about uninstallation
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to remove uninstall data: " + ex, Logging.LogLevel.ERROR);
            }

            // Remove Control Panel shortcut if exists
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Microsoft", "Windows", "Start Menu", "Programs", "GoAwayEdge.lnk");
            if (File.Exists(shortcutPath)) File.Delete(shortcutPath);

            // Switch FrameWindow content to InstallationSuccess
            worker?.ReportProgress(100, "");
            Logging.Log("Uninstallation finished.");
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

        internal static bool SetUninstallData()
        {
            try
            {
                var installDate = DateTime.Now.ToString("yyyyMMdd");
                RegistryConfig.SetKey("DisplayIcon", Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), isUninstall: true);
                RegistryConfig.SetKey("DisplayName", "GoAwayEdge", isUninstall: true);
                RegistryConfig.SetKey("DisplayVersion", Assembly.GetExecutingAssembly().GetName().Version!, isUninstall: true);
                RegistryConfig.SetKey("HelpLink", "https://github.com/valnoxy/GoAwayEdge/issues", isUninstall: true);
                RegistryConfig.SetKey("InstallDate", installDate, isUninstall: true);
                RegistryConfig.SetKey("InstallLocation", Configuration.InstallDir, isUninstall: true);
                RegistryConfig.SetKey("ModifyPath", Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"), isUninstall: true);
                RegistryConfig.SetKey("Publisher", "valnoxy", isUninstall: true);
                RegistryConfig.SetKey("QuietUninstallString", $"\"{Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe")}\"", isUninstall: true);
                RegistryConfig.SetKey("UninstallString", $"\"{Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe")}\" -u", isUninstall: true);
                RegistryConfig.SetKey("URLInfoAbout", "https://goawayedge.com", isUninstall: true);
                RegistryConfig.SetKey("URLUpdateInfo", "https://github.com/valnoxy/goawayedge/releases", isUninstall: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void KillProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }

    public class Register
    {
        public enum IfeoType
        {
            msedge,
            ie_to_edge_stub
        }
        public enum UriType
        {
            microsoftEdge,
            EdgeHTM
        }

        /// <summary>
        /// Register a Debugger for a specific application via Image File Execution Option.
        /// </summary>
        /// <param name="type">Type of application</param>
        /// <param name="debugger">Full command for debugger</param>
        /// <param name="filterFullPath">Path to application</param>
        /// <returns>
        ///     Result of the registration as boolean.
        /// </returns>
        public static bool ImageFileExecutionOption(IfeoType type, string debugger, string filterFullPath)
        {
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);

                switch (type)
                {
                    case IfeoType.msedge:
                        key?.CreateSubKey("msedge.exe");
                        key = key?.OpenSubKey("msedge.exe", true);
                        break;
                    case IfeoType.ie_to_edge_stub:
                        key?.CreateSubKey("ie_to_edge_stub.exe");
                        key = key?.OpenSubKey("ie_to_edge_stub.exe", true);
                        break;
                    default:
                        return false;
                }

                key?.SetValue("UseFilter", 1, RegistryValueKind.DWord);
                key?.CreateSubKey("0");
                key = key?.OpenSubKey("0", true);
                key?.SetValue("Debugger", debugger);
                key?.SetValue("FilterFullPath", filterFullPath);
                key?.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register a Uri Handler.
        /// </summary>
        /// <param name="type">Type of Uri</param>
        /// <param name="commandLine">Full command line</param>
        /// <returns>
        ///     Result of the registration as boolean.
        /// </returns>
        public static bool UriHandler(UriType type, string commandLine)
        {
            try
            {
                using var baseKey = Registry.LocalMachine;
                RegistryKey? key;

                switch (type)
                {
                    case UriType.microsoftEdge:
                        key = baseKey.OpenSubKey(@"SOFTWARE\Classes\microsoft-edge", true) ?? baseKey.CreateSubKey(@"SOFTWARE\Classes\microsoft-edge");
                        baseKey.SetValue("", "URL:microsoft-edge", RegistryValueKind.String);
                        baseKey.SetValue("URL Protocol", "", RegistryValueKind.String);
                        baseKey.SetValue("NoOpenWith", "", RegistryValueKind.String);
                        break;
                    case UriType.EdgeHTM:
                        key = baseKey.OpenSubKey(@"SOFTWARE\Classes\MSEdgeHTM", true) ?? baseKey.CreateSubKey(@"SOFTWARE\Classes\MSEdgeHTM");
                        baseKey.SetValue("NoOpenWith", "", RegistryValueKind.String);
                        break;
                    default:
                        return false;
                }

                using var shellKey = key.CreateSubKey("shell");
                using var openKey = shellKey.CreateSubKey("open");
                using var commandKey = openKey.CreateSubKey("command");
                commandKey.SetValue("", commandLine, RegistryValueKind.String);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
