using System.IO;
using Microsoft.Win32;

namespace GoAwayEdge.Common
{
    internal enum SearchEngine
    {
        Google,
        Bing,
        DuckDuckGo,
        Yahoo,
        Yandex,
        Ecosia,
        Ask,
        Qwant,
        Perplexity,
        Custom
    }

    internal enum EdgeChannel
    {
        Stable,
        Beta,
        Dev,
        Canary
    }

    internal class Configuration
    {
        public static EdgeChannel Channel { get; set; }
        public static SearchEngine Search { get; set; }
        public static bool Uninstall { get; set; }
        public static bool UninstallEdge { get; set; }
        public static string? CustomQueryUrl { get; set; }

        public static string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "valnoxy",
            "GoAwayEdge");

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
                    var defaultEdgePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Microsoft", "Edge", "Application");

                    if (!Directory.Exists(defaultEdgePath)) return false;
                    
                    FileConfiguration.EdgePath = Path.Combine(defaultEdgePath, "msedge.exe");
                    FileConfiguration.IfeoPath = Path.Combine(defaultEdgePath, "msedge_ifeo.exe");
                    return true;
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
    }

    internal enum ModifyAction
    {
        Create,
        Update
    }

    internal class FileConfiguration
    {
        public static string EdgePath = string.Empty;
        public static string IfeoPath = string.Empty;
    }
}
