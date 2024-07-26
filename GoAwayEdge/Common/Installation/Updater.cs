using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
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
        /// Validates if the installed IFEO-Binary is identical with the Edge-Binary.
        /// </summary>
        /// <returns>
        ///     Integer value if the Binary is identical, not identical or missing.
        ///     0 : true (also reported if Edge is not installed)
        ///     1 : false
        ///     2 : missing
        /// </returns>
        public static int ValidateIfeoBinary()
        {
            if (Configuration.NoEdgeInstalled) return 0;

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
            var ifeoBinaryPath = Path.Combine(Path.GetDirectoryName(binaryPath)!, "msedge_non_ifeo.exe");

            if (File.Exists(ifeoBinaryPath))
            {
                var edgeHash = CalculateMd5(edgeBinaryPath);
                var ifeoHash = CalculateMd5(ifeoBinaryPath);
#if DEBUG
                if (edgeHash != ifeoHash)
                {
                    var messageUi = new MessageUi("GoAwayEdge",
                        $"The Edge Hash ({edgeHash}) and Ifeo Hash ({ifeoHash}) are not identical. Validation failed!", "OK", isMainThread: true);
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
                {
                    try
                    {
                        File.Copy(FileConfiguration.EdgePath, FileConfiguration.NonIfeoPath, true);

                        var title = LocalizationManager.LocalizeValue("IfeoUpdateSuccessfulTitle");
                        var description = LocalizationManager.LocalizeValue("IfeoUpdateSuccessfulDescription");
                        new ToastContentBuilder()
                            .AddText(title)
                            .AddText(description)
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        var message = LocalizationManager.LocalizeValue("FailedUpdate", ex.Message);
                        var messageUi = new MessageUi("GoAwayEdge", message, "OK", isMainThread: true);
                        messageUi.ShowDialog();
                    }
                    break;
                }
                case ModifyAction.Create:
                {
                    try
                    {
                        File.Copy(FileConfiguration.EdgePath, FileConfiguration.NonIfeoPath, true);

                        var title = LocalizationManager.LocalizeValue("IfeoCreateSuccessfulTitle");
                        var description = LocalizationManager.LocalizeValue("IfeoCreateSuccessfulDescription");
                        new ToastContentBuilder()
                            .AddText(title)
                            .AddText(description)
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        var message = LocalizationManager.LocalizeValue("FailedUpdate", ex.Message);
                        var messageUi = new MessageUi("GoAwayEdge", message, "OK", isMainThread: true);
                        messageUi.ShowDialog();
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        /// <summary>
        /// Checks if a newer version of GoAwayEdge exists.
        /// </summary>
        /// <returns>
        ///     Boolean status of the existence of a newer version.
        /// </returns>
        public static string? CheckForAppUpdate()
        {
            const string url = "https://api.github.com/repos/valnoxy/GoAwayEdge/releases";

            try
            {
                var appVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                var currentVersion = new Version(appVersion.Major, appVersion.Minor, appVersion.Build);

                using var client = new WebClient();
                client.Headers.Add("User-Agent", $"GoAwayEdge/{currentVersion} valnoxy.dev");
                var json = client.DownloadString(url);

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    var tagName = Convert.ToString(releases[0].tag_name);
                    var tagVersion = tagName[1..];
                    var latestVersion = new Version(tagVersion);

                    if (currentVersion < latestVersion)
                        return latestVersion.ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        
        /// <summary>
        /// Download and run the new version of GoAwayEdge.
        /// </summary>
        public static bool UpdateClient()
        {
            const string url = "https://api.github.com/repos/valnoxy/GoAwayEdge/releases";

            try
            {
                var appVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                var currentVersion = new Version(appVersion.Major, appVersion.Minor, appVersion.Build);

                using var client = new WebClient();
                client.Headers.Add("User-Agent", $"GoAwayEdge/{currentVersion} valnoxy.dev");
                var json = client.DownloadString(url);

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    var assetUrl = string.Empty;
                    foreach (var asset in releases[0].assets.Where(asset => asset.name == "GoAwayEdge.exe"))
                    {
                        assetUrl = asset.browser_download_url;
                    }
                    
                    var tempFolder = Path.GetTempPath();
                    if (string.IsNullOrEmpty(assetUrl))
                    {
                        return false;
                    }

                    if (File.Exists(Path.Combine(tempFolder, "GoAwayEdge.exe")))
                        File.Delete(Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    client.DownloadFile(assetUrl, Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    Process.Start(Path.Combine(tempFolder, "GoAwayEdge.exe"));
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
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
