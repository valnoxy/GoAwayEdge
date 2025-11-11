using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;

namespace GoAwayEdge.Common.Installation
{
    internal class Updater
    {
        public class GitHubRelease
        {
            public string? tag_name { get; set; }
            public List<GitHubAsset>? assets { get; set; }
        }

        public class GitHubAsset
        {
            public string? browser_download_url { get; set; }
            public string? name { get; set; }
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

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"GoAwayEdge/{currentVersion} valnoxy.dev");

                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var json = response.Content.ReadAsStringAsync().Result;

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    var tagName = Convert.ToString(releases[0].tag_name);
                    var tagVersion = tagName![1..];
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

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"GoAwayEdge/{currentVersion} valnoxy.dev");

                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var json = response.Content.ReadAsStringAsync().Result;

                var releases = JsonConvert.DeserializeObject<GitHubRelease[]>(json);
                if (releases is { Length: > 0 })
                {
                    var assetUrl = string.Empty;
                    foreach (var asset in releases[0].assets!.Where(asset => asset.name == "GoAwayEdge.exe"))
                    {
                        assetUrl = asset.browser_download_url;
                    }

                    if (string.IsNullOrEmpty(assetUrl))
                    {
                        return false;
                    }

                    var tempFolder = Path.GetTempPath();
                    var tempFilePath = Path.Combine(tempFolder, "GoAwayEdge.exe");

                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }

                    using (var downloadStream = client.GetStreamAsync(assetUrl).Result)
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        downloadStream.CopyTo(fileStream);
                    }

                    Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
