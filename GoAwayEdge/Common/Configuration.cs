using System.IO;
using System.Windows;
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
        public static bool NoEdgeInstalled { get; set; }
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
            // Check if Edge is installed
            try
            {
                NoEdgeInstalled = !File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"));
                RegistryConfig.SetKey("NoEdgeInstalled", NoEdgeInstalled);

                if (NoEdgeInstalled)
                    return true;

                FileConfiguration.EdgePath = RegistryConfig.GetKey("EdgeFilePath");
                FileConfiguration.NonIfeoPath = RegistryConfig.GetKey("EdgeNonIEFOFilePath");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = LocalizationManager.LocalizeValue("FailedInitialization", ex.Message);
                var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK", null, true);
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
        public static string NonIfeoPath = string.Empty;
    }

    public class RegistryConfig
    {
        private const string Company = "valnoxy";
        private const string Product = "GoAwayEdge";
        private const string RegistryPath = @$"SOFTWARE\{Company}\{Product}";

        /// <summary>
        /// Create a Key in the Registry
        /// </summary>
        /// <param name="option">Key Name</param>
        /// <param name="value">Key Value</param>
        public static void SetKey(string option, object value)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                key.SetValue(option, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while writing to the registry: " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the value of a key from the Registry.
        /// </summary>
        /// <param name="option">Name of the key.</param>
        /// <returns>The value of the key if it exists, otherwise null.</returns>
        public static string GetKey(string option)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    var value = key.GetValue(option);
                    if (value != null)
                    {
                        return value.ToString()!;
                    }
                    else
                    {
                        Console.WriteLine($"Value for key '{option}' not found in the registry.");
                        return "";
                    }
                }
                else
                {
                    Console.WriteLine($"Registry key '{RegistryPath}' not found.");
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while reading the registry: " + ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Create a Key in the Registry
        /// </summary>
        /// <param name="option">Key Name</param>
        public static bool RemoveKey(string option)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                var value = key?.GetValue(option);
                if (value != null)
                {
                    key?.DeleteValue(option);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while reading the registry: " + ex.Message);
            }

            return false;
        }
    }
}
