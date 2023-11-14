using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GoAwayEdge.Common
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
			// Ok, this is very wip. Current plan:
			//
			//  1. Remove Edge via edge setup (setup.exe --uninstall --system-level --verbose-logging --force-uninstall)
			//  2. Prevent Edge from reinstalling
			//  3. Recreate the URI protocol
            //
            // This way should left WebView2 etc in tact.
			// 

            string edgeSetupPath;
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
                edgeSetupPath = Path.Combine(newestDirectory!.DirectoryPath, "Installer", "setup.exe");
            }
            else
            {
                return false; // Might not be installed - TODO: Add handler
            }

            // Terminate processes
            KillProcess("MicrosoftEdge");
            KillProcess("chredge");
            KillProcess("msedge");
            KillProcess("edge");

            // Clean up registry
            RemoveRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe");
            RemoveRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ie_to_edge_stub.exe");
            RemoveUserRegistryKey(@"Software\Classes\microsoft-edge");
            RemoveUserRegistryKey(@"Software\Classes\MSEdgeHTM");

            // Write temporary URI handler into the registry
            try
            {
                // HKLM\SOFTWARE\Classes\microsoft-edge
                using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Classes\microsoft-edge", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Classes\microsoft-edge"))
                {
                    using (var shellKey = key.CreateSubKey("shell"))
                    using (var openKey = shellKey.CreateSubKey("open"))
                    using (var commandKey = openKey.CreateSubKey("command"))
                    {
                        commandKey.SetValue("", $"\"{FileConfiguration.EdgePath}\" \" --single-argument %1\"", RegistryValueKind.String);
                    }
                }

                // HKLM\SOFTWARE\Classes\EdgeHTM
                using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Classes\MSEdgeHTM", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Classes\MSEdgeHTM"))
                {
                    using (var shellKey = key.CreateSubKey("shell"))
                    using (var openKey = shellKey.CreateSubKey("open"))
                    using (var commandKey = openKey.CreateSubKey("command"))
                    {
                        commandKey.SetValue("", $"\"{FileConfiguration.EdgePath}\" \" --single-argument %1\"", RegistryValueKind.String);
                    }
                }
            }
            catch
            {
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
                return false;
            }

            // Find and copy ie_to_edge_stub.exe
            var ieToEdgeStubFile = Path.Combine(Path.GetDirectoryName(FileConfiguration.EdgePath)!, "ie_to_edge_stub.exe");
            if (File.Exists(ieToEdgeStubFile))
            {
                try
                {
                    File.Copy(ieToEdgeStubFile, Path.Combine(Configuration.InstallDir, "ie_to_edge_stub.exe"));
                }
                catch
                {
                    return false;
                }
            }
            else return false;

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
                return false;
            }


            // Remove Edge Update
            var p = new Process();
            ProcessStartInfo psi;
            var timeoutStopwatch = new Stopwatch();
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var edgeUpdatePath = Path.Combine(programFiles, "Microsoft", "EdgeUpdate", "MicrosoftEdgeUpdate.exe");

            if (File.Exists(edgeUpdatePath))
            {
                psi = new ProcessStartInfo
                {
                    FileName = edgeUpdatePath,
                    Arguments = "/uninstall",
                };
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();

                timeoutStopwatch.Start();
                while (timeoutStopwatch.Elapsed.TotalSeconds < 60 && (IsProcessRunning("setup") || IsEdgeProcessRunning()))
                {
                    Thread.Sleep(3000);
                }
                timeoutStopwatch.Stop();
                if (timeoutStopwatch.Elapsed.TotalSeconds >= 60)
                    return false; // Timeout
                if (p.ExitCode != 0 && p.ExitCode != 19)
                    return false; // Unknown error?
            }

            // Remove Edge via setup file
            psi = new ProcessStartInfo
            {
                FileName = edgeSetupPath,
                Arguments = "--uninstall --msedge --system-level --verbose-logging --force-uninstall",
                RedirectStandardOutput = true
            };
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();

            timeoutStopwatch.Start();
            while (timeoutStopwatch.Elapsed.TotalSeconds < 60 && (IsProcessRunning("setup") || IsEdgeProcessRunning()))
            {
                Thread.Sleep(3000);
            }
            timeoutStopwatch.Stop();
            if (timeoutStopwatch.Elapsed.TotalSeconds >= 60)
                return false; // Timeout
            if (p.ExitCode != 0 && p.ExitCode != 19)
                return false; // Unknown error?

            // Prevent Edge from reinstalling
            try
            {
                using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Microsoft"))
                {
                    using (var edgeUpdate = key.CreateSubKey("EdgeUpdate"))
                    {
                        edgeUpdate.SetValue("DoNotUpdateToEdgeWithChromium", 1, RegistryValueKind.DWord);
                    }
                }
			}
            catch
            {
                return false;
            }


            return false;
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
            catch
            {
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
    }
}
