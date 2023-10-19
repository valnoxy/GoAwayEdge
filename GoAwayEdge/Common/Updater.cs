using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace GoAwayEdge.Common
{
    internal class Updater
    {
        public class GitHubRelease
        {
            public string tag_name { get; set; }
            public List<GitHubAsset> assets { get; set; }
        }

        public class GitHubAsset
        {
            public string browser_download_url { get; set; }
            public string name { get; set; }
        }


        /// <summary>
        /// Initialize the current environment.
        /// </summary>
        /// <returns>
        ///     Boolean status of the initialization.
        /// </returns>
        public static bool InitialEnvironment()
        {
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
                if (key == null)
                {
                    Console.WriteLine("Registry key not found.");
                    return false;
                }

                var ifeoBinaryPath = (string)key.GetValue("FilterFullPath")!;
                if (string.IsNullOrEmpty(ifeoBinaryPath))
                {
                    Console.WriteLine("FilterFullPath value not found.");
                    return false;
                }

                FileConfiguration.EdgePath = Path.Combine(Path.GetDirectoryName(ifeoBinaryPath)!, "msedge.exe");
                FileConfiguration.IfeoPath = Path.Combine(Path.GetDirectoryName(ifeoBinaryPath)!, "msedge_ifeo.exe");
                return true;
            }
            catch (Exception ex)
            {
                var messageUi = new MessageUi("GoAwayEdge",
                    $"Initialization failed!\n{ex.Message}", "OK", null, true);
                messageUi.ShowDialog();
                return false;
            }
        }

        /// <summary>
        /// Validates if the installed IFEO-Binary is identical with the Edge-Binary.
        /// </summary>
        /// <returns>
        ///     Integer value if the Binary is identical, not identical or missing.
        ///     0 : true
        ///     1 : false
        ///     2 : missing
        /// </returns>
        public static int ValidateIfeoBinary()
        {
            var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
            if (key == null)
            {
                Console.WriteLine("Registry key not found.");
                return 2;
            }

            var binaryPath = (string)key.GetValue("FilterFullPath")!;
            if (string.IsNullOrEmpty(binaryPath))
            {
                Console.WriteLine("FilterFullPath value not found.");
                return 2;
            }

            var edgeBinaryPath = Path.Combine(Path.GetDirectoryName(binaryPath)!, "msedge.exe");
            var ifeoBinaryPath = Path.Combine(Path.GetDirectoryName(binaryPath)!, "msedge_ifeo.exe");

            if (File.Exists(ifeoBinaryPath))
            {
                var edgeHash = CalculateMd5(edgeBinaryPath);
                var ifeoHash = CalculateMd5(ifeoBinaryPath);
#if DEBUG
                if (edgeHash != ifeoHash) {
                    var messageUi = new MessageUi("GoAwayEdge",
                        $"The Edge Hash ({edgeHash}) and Ifeo Hash ({ifeoHash}) are not identical. Validation failed!", "OK", null,true);
                    messageUi.ShowDialog();
                }
#endif
                return edgeHash != ifeoHash ? 1 : 0;
            }

            Console.WriteLine($"Ifeo binary does not exist: {ifeoBinaryPath}");
            return 2;
        }

        /// <summary>
        /// Modifies the Ifeo binary.
        /// </summary>
        public static void ModifyIfeoBinary(ModifyAction action)
        {
            switch (action)
            {
                case ModifyAction.Update:
                case ModifyAction.Create:
                {
                    try
                    {
                        File.Copy(FileConfiguration.EdgePath, FileConfiguration.IfeoPath, true);
                        new ToastContentBuilder()
                            .AddText("Update successful")
                            .AddText("The IFEO binary was successfully updated.")
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        var messageUi = new MessageUi("GoAwayEdge",
                            $"Update failed!\n{ex.Message}!", "OK", null, true);
                        messageUi.ShowDialog();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if an newer version of GoAwayEdge exists.
        /// </summary>
        /// <returns>
        ///     Boolean status of the existence of a newer version.
        /// </returns>
        public static string CheckForAppUpdate()
        {
            const string url = "https://api.github.com/repos/valnoxy/GoAwayEdge/releases";

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);

                using var client = new WebClient();
                client.Headers.Add("User-Agent", $"GoAwayEdge/{versionInfo.FileVersion} valnoxy.dev");
                var json = client.DownloadString(url);

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    var tagName = Convert.ToString(releases[0].tag_name);
                    var tagVersion = tagName[1..];
                    var currentFileVersion = versionInfo.FileVersion;
                    var parts = currentFileVersion.Split('.');
                    var partsResult = string.Join(".", parts.Take(3));
                    var currentVersion = new Version(partsResult);
                    var latestVersion = new Version(tagVersion);

                    if (currentVersion < latestVersion)
                        return latestVersion.ToString();
                }
                return null!;
            }
            catch
            {
                return null!;
            }
        }
        
        /// <summary>
        /// Download and run the new version of GoAwayEdge.
        /// </summary>
        public static int UpdateClient()
        {
            const string url = "https://api.github.com/repos/valnoxy/GoAwayEdge/releases";

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
                
                using var client = new WebClient();
                client.Headers.Add("User-Agent", $"GoAwayEdge/{versionInfo.FileVersion} valnoxy.dev");
                var json = client.DownloadString(url);

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    string assetUrl = null;
                    foreach (var asset in releases[0].assets.Where(asset => asset.name == "GoAwayEdge.exe"))
                    {
                        assetUrl = asset.browser_download_url;
                    }
                    
                    var tempFolder = Path.GetTempPath();
                    if (assetUrl == null)
                    {
                        return 1;
                    }

                    if (File.Exists(Path.Combine(tempFolder, "GoAwayEdge.exe")))
                        File.Delete(Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    client.DownloadFile(assetUrl, Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    Process.Start(Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    return 0;
                }
            }
            catch
            {
                return 1;
            }
            return 1;
        }

        private static string CalculateMd5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
