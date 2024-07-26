using System.Diagnostics;
using System.IO;
using GoAwayEdge.Common.Debugging;
using Microsoft.Win32;

namespace GoAwayEdge.Common.Installation
{
    public class Removal
    {
        /// <summary>
        /// Removes Microsoft Edge completely from the system.
        /// </summary>
        /// <returns>
        ///     Result of the removal as boolean.
        /// </returns>
		public static bool RemoveMsEdge()
        {
            //
            // Ok, this should be stable now. Current plan:
            //
            //  1. Remove Edge via edge setup (setup.exe --uninstall --system-level --verbose-logging --force-uninstall)
            //  2. Prevent Edge from reinstalling
            //  3. Recreate the URI protocol
            //
            // This way should left WebView2 etc in tact.
            // 
            Logging.Log("Removing Microsoft Edge ...");

            string edgeSetupPath;
            string edgeNewestVersionPath;
            if (!Directory.Exists(Path.GetDirectoryName(FileConfiguration.EdgePath)))
                return false;

            var subDirectories = Directory.GetDirectories(Path.GetDirectoryName(FileConfiguration.EdgePath)!);
            var validDirectories = subDirectories
                .Where(dir => Version.TryParse(Path.GetFileName(dir), out _))
                .ToList();

            if (validDirectories.Any())
            {
                var sortedDirectories = validDirectories
                    .Select(dir => new
                    {
                        DirectoryPath = dir,
                        Version = new Version(Path.GetFileName(dir))
                    })
                    .OrderByDescending(x => x.Version);

                var newestDirectory = sortedDirectories.FirstOrDefault();
                edgeNewestVersionPath = newestDirectory!.DirectoryPath;
                edgeSetupPath = Path.Combine(newestDirectory!.DirectoryPath, "Installer", "setup.exe");
            }
            else
            {
                Logging.Log("Removal aborted: Edge was not found on this system.", Logging.LogLevel.WARNING);
                return false; // Edge is already removed
            }

            // Terminate processes
            Logging.Log("Terminating Edge processes ...");
            KillProcess("MicrosoftEdge");
            KillProcess("chredge");
            KillProcess("msedge");
            KillProcess("MicrosoftEdgeUpdate");
            KillProcess("edge");

            // Clean up registry
            Logging.Log("Cleaning up registry ...");
            RemoveRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe");
            RemoveRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ie_to_edge_stub.exe");
            RemoveUserRegistryKey(@"Software\Classes\microsoft-edge");
            RemoveUserRegistryKey(@"Software\Classes\MSEdgeHTM");

            // Write temporary URI handler into the registry
            var status = Register.UriHandler(Register.UriType.microsoftEdge,
                $"\"{FileConfiguration.EdgePath}\" \" --single-argument %1\"");
            if (!status)
            {
                Logging.Log("Failed to register URI handler for Microsoft Edge.", Logging.LogLevel.ERROR);
                return false;
            }

            status = Register.UriHandler(Register.UriType.EdgeHTM,
                $"\"{FileConfiguration.EdgePath}\" \" --single-argument %1\"");
            if (!status)
            {
                Logging.Log("Failed to register URI handler for EdgeHTM.", Logging.LogLevel.ERROR);
                return false;
            }

            // Remove certain registry properties
            try
            {
                string[] registryPaths = { "SOFTWARE\\Policies", "SOFTWARE", "SOFTWARE\\WOW6432Node" };
                string[] edgeProperties =
                {
                    "InstallDefault", "Install{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062",
                    "Install{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
                };

                foreach (var path in registryPaths)
                {
                    using var key = Registry.LocalMachine.OpenSubKey($@"{path}\Microsoft\EdgeUpdate", true);
                    if (key == null) continue;
                    foreach (var prop in edgeProperties)
                    {
                        key.DeleteValue(prop, false);
                    }
                }

                const string edgeUpdate = @"Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}";
                string[] onActions = { "on-os-upgrade", "on-logon", "on-logon-autolaunch", "on-logon-startup-boost" };
                string[] registryBases = { "SOFTWARE", "SOFTWARE\\Wow6432Node" };

                foreach (var baseKey in registryBases)
                {
                    foreach (var launch in onActions)
                    {
                        RemoveRegistryKey($@"HKLM\{baseKey}\{edgeUpdate}\Commands\{launch}");
                    }
                }

                registryPaths = new[] { "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE" };
                string[] nodes = { "", "\\Wow6432Node" };
                string[] removeWin32 = { "Microsoft Edge", "Microsoft Edge Update" };

                foreach (var regPath in registryPaths)
                {
                    foreach (var node in nodes)
                    {
                        foreach (var i in removeWin32)
                        {
                            var uninstallPath = $@"{regPath}\SOFTWARE{node}\Microsoft\Windows\CurrentVersion\Uninstall\{i}";

                            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                            {
                                using var uninstallKey = key.OpenSubKey(uninstallPath, true);
                                uninstallKey?.DeleteValue("NoRemove", false);
                            }
                            var edgeUpdateDevKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE{node}\Microsoft\EdgeUpdateDev");
                            edgeUpdateDevKey.SetValue("AllowUninstall", 1, RegistryValueKind.DWord);
                        }
                    }
                }
            }
            catch
            {
                Logging.Log("Failed to remove certain registry properties.", Logging.LogLevel.ERROR);
                return false;
            }

            // Find and copy ie_to_edge_stub.exe
            var ieToEdgeStubFile = Path.Combine(edgeNewestVersionPath, "BHO", "ie_to_edge_stub.exe");
            if (File.Exists(ieToEdgeStubFile))
            {
                try
                {
                    File.Copy(ieToEdgeStubFile, Path.Combine(Configuration.InstallDir, "ie_to_edge_stub.exe"), true);
                }
                catch
                {
                    Logging.Log("Failed to copy ie_to_edge_stub.exe.", Logging.LogLevel.ERROR);
                    return false;
                }
            }
            else
            {
                Logging.Log("ie_to_edge_stub.exe not found.", Logging.LogLevel.ERROR);
                return false;
            }

            // AppX Removal
            const string removeAppX = "MicrosoftEdge";
            const string storePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore";
            string[] userSids = { "S-1-5-18" };
            var packageFullNameList = new List<string>();

            try
            {
                if (Registry.LocalMachine.OpenSubKey(storePath) != null)
                {
                    var subKeyNames = Registry.LocalMachine.OpenSubKey(storePath)!.GetSubKeyNames();
                    foreach (var subKeyName in subKeyNames)
                    {
                        if (!subKeyName.Contains("S-1-5-21")) continue;
                        userSids = userSids.Append(subKeyName).ToArray();

                        var userKey = Registry.LocalMachine.OpenSubKey($"{storePath}\\{subKeyName}", false);
                        if (userKey == null) continue;
                        var subkeyNames = userKey.GetSubKeyNames();
                        foreach (var subkeyName in subkeyNames)
                        {
                            if (subkeyName.Contains(removeAppX) && !packageFullNameList.Contains(subkeyName))
                            {
                                packageFullNameList.Add(subkeyName);
                            }
                        }
                    }
                }
            }
            catch
            {
                Logging.Log("Failed to remove AppX packages.", Logging.LogLevel.ERROR);
                return false;
            }

            // Removing Edge (+ Updater)
            var timeoutStopwatch = new Stopwatch();
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var edgeUpdatePath = Path.Combine(programFiles, "Microsoft", "EdgeUpdate", "MicrosoftEdgeUpdate.exe");

            try
            {
                if (File.Exists(edgeUpdatePath))
                {
                    using (var proc = new Process()) // Remove Edge Updater
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = edgeUpdatePath,
                            Arguments = "/uninstall",
                        };
                        proc.StartInfo = psi;
                        proc.Start();
                        proc.WaitForExit();

                        timeoutStopwatch.Start();
                        while (timeoutStopwatch.Elapsed.TotalSeconds < 60 &&
                               (IsProcessRunning("MicrosoftEdgeUpdate") || IsEdgeProcessRunning()))
                        {
                            Thread.Sleep(3000);
                        }

                        timeoutStopwatch.Stop();
                        if (timeoutStopwatch.Elapsed.TotalSeconds >= 60)
                            return false; // Timeout
                        if (proc.ExitCode != 0 && proc.ExitCode != 19)
                            return false; // Unknown error?
                    }

                    using (var proc = new Process()) // Remove Edge via setup file
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = edgeSetupPath,
                            Arguments = "--uninstall --msedge --system-level --verbose-logging --force-uninstall",
                            RedirectStandardOutput = true
                        };
                        proc.StartInfo = psi;
                        proc.Start();
                        proc.WaitForExit();

                        timeoutStopwatch.Start();
                        while (timeoutStopwatch.Elapsed.TotalSeconds < 60 &&
                               (IsProcessRunning("setup") || IsEdgeProcessRunning()))
                        {
                            Thread.Sleep(3000);
                        }

                        timeoutStopwatch.Stop();
                        if (timeoutStopwatch.Elapsed.TotalSeconds >= 60)
                            return false; // Timeout
                        if (proc.ExitCode != 0 && proc.ExitCode != 19)
                            return false; // Unknown error?
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Exception: " + ex, Logging.LogLevel.ERROR);
                return false;
            }

            // Prevent Edge from reinstalling
            try
            {
                using var baseKey = Registry.LocalMachine;
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft", true) ??
                                baseKey.CreateSubKey(@"SOFTWARE\Microsoft");
                using var edgeUpdate = key.CreateSubKey("EdgeUpdate");
                edgeUpdate.SetValue("DoNotUpdateToEdgeWithChromium", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to prevent Edge from reinstalling: " + ex, Logging.LogLevel.ERROR);
                return false;
            }

            // Register new URIs
            status = Register.UriHandler(Register.UriType.microsoftEdge,
                $"\"{Path.Combine(Configuration.InstallDir, "ie_to_edge_stub.exe")}\" \"%1\"");
            if (!status)
            {
                Logging.Log("Failed to register URI handler for Microsoft Edge.", Logging.LogLevel.ERROR);
                return false;
            }

            status = Register.UriHandler(Register.UriType.EdgeHTM,
                $"\"{Path.Combine(Configuration.InstallDir, "ie_to_edge_stub.exe")}\" \"%1\"");
            if (!status)
            {
                Logging.Log("Failed to register URI handler for EdgeHTM.", Logging.LogLevel.ERROR);
                return false;
            }

            status = Register.ImageFileExecutionOption(
                Register.IfeoType.ie_to_edge_stub,
                Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe"),
                Path.Combine(Configuration.InstallDir, "ie_to_edge_stub.exe"));

            if (!status)
            {
                Logging.Log("Failed to register Image File Execution Option for ie_to_edge_stub.", Logging.LogLevel.ERROR);
                return false;
            }

            // Set registry config
            RegistryConfig.SetKey("NoEdgeInstalled", true);

            Logging.Log("Microsoft Edge has been removed successfully.");
            return true;
        }

        /// <summary>
        /// Terminate the specific process.
        /// </summary>
        /// <returns>
        ///     Result of the termination as boolean.
        /// </returns>
        public static bool KillProcess(string processName)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    process.Kill();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to kill process {processName}: {ex}", Logging.LogLevel.ERROR);
                return false;
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        private static bool IsEdgeProcessRunning()
        {
            var processes = Process.GetProcesses();
            return processes.Any(process => process.ProcessName.StartsWith("MicrosoftEdge", StringComparison.OrdinalIgnoreCase) && process.MainModule != null && process.MainModule.FileName.Contains("\\Microsoft\\Edge"));
        }

        private static void RemoveRegistryKey(string keyPath)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
            if (key != null)
            {
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
            }
        }

        private static void RemoveUserRegistryKey(string keyPath)
        {
            try
            {
                var users = Registry.Users.GetSubKeyNames();
                foreach (var userSid in users)
                {
                    using var userKey = Registry.Users.OpenSubKey(userSid);
                    using var key = userKey?.OpenSubKey(keyPath, true);
                    if (key != null)
                    {
                        userKey!.DeleteSubKeyTree(keyPath, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"Exception while removing registry key: {ex}");
                System.Diagnostics.Debug.WriteLine($"Exception while removing registry key: {ex.Message}");
            }
        }
    }
}
