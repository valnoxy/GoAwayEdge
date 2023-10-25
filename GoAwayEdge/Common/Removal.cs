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

			// Remove Edge via setup file
			var p = new Process();
			var psi = new ProcessStartInfo(edgeSetupPath)
            {
                FileName = edgeSetupPath,
                Arguments = "--uninstall --system-level --verbose-logging --force-uninstall",
                RedirectStandardOutput = true
            };
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0 && p.ExitCode != 19)
                return false;

            // Prevent Edge from reinstalling
            try
            {
                using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Microsoft"))
                {
                    if (key != null)
                    {
                        using (var edgeUpdate = key.CreateSubKey("EdgeUpdate"))
                        {
                            edgeUpdate.SetValue("DoNotUpdateToEdgeWithChromium", 1, RegistryValueKind.DWord);
						}
                    }
                }
			}
            catch
            {
                return false;
            }

            // Write new URI handler into the registry
            try
            {
				// SOFTWARE\Classes\microsoft-edge
                using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Classes\microsoft-edge", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Classes\microsoft-edge"))
                {
					if (key != null)
                    {
                        key.SetValue("", "URL:microsoft-edge", RegistryValueKind.String);
                        key.SetValue("URL Protocol", "", RegistryValueKind.String);
                        key.SetValue("NoOpenWith", "", RegistryValueKind.String);

                        using (var shellKey = key.CreateSubKey("shell"))
                        using (var openKey = shellKey.CreateSubKey("open"))
                        using (var commandKey = openKey.CreateSubKey("command"))
                        {
                            commandKey.SetValue("", $"\"{Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe")}\" \"%*\"", RegistryValueKind.String);
                        }
                    }
                }

				// SOFTWARE\Classes\EdgeHTM
				using (var baseKey = Registry.LocalMachine)
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Classes\MSEdgeHTM", true) ??
                                 baseKey.CreateSubKey(@"SOFTWARE\Classes\MSEdgeHTM")) 
                {
					if (key != null)
                    {
                        key.SetValue("NoOpenWith", "", RegistryValueKind.String);

                        using (var shellKey = key.CreateSubKey("shell"))
                        using (var openKey = shellKey.CreateSubKey("open"))
                        using (var commandKey = openKey.CreateSubKey("command"))
                        {
                            commandKey.SetValue("",$"\"{Path.Combine(Configuration.InstallDir, "GoAwayEdge.exe")}\" \"%*\"", RegistryValueKind.String);
                        }
                    }
                }

				return true;
            }
			catch
            {
                return false;
            }
        }
    }
}
