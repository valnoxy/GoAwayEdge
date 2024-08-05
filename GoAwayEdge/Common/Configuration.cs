using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace GoAwayEdge.Common
{
    public enum SearchEngine
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

    public enum EdgeChannel
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
        public static bool InstallControlPanel { get; set; }
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
                // RegistryConfig.SetKey("NoEdgeInstalled", NoEdgeInstalled);

                if (NoEdgeInstalled)
                    return true;

                FileConfiguration.EdgePath = RegistryConfig.GetKey("EdgeFilePath");
                FileConfiguration.NonIfeoPath = RegistryConfig.GetKey("EdgeNonIEFOFilePath");

                try
                {
                    Channel = Runtime.ArgumentParse.ParseEdgeChannel(RegistryConfig.GetKey("Stable"));
                    Search = Runtime.ArgumentParse.ParseSearchEngine(RegistryConfig.GetKey("SearchEngine"));
                    if (Search == SearchEngine.Custom)
                    {
                        CustomQueryUrl = RegistryConfig.GetKey("CustomQueryUrl");
                    }
                }
                catch
                {
                    // ignored
                }
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = LocalizationManager.LocalizeValue("FailedInitialization", ex.Message);
                var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK", isMainThread: true);
                messageUi.ShowDialog();
                return false;
            }
        }

        /// <summary>
        ///     Get a list of all available Edge Channels.
        /// </summary>
        /// <returns>
        ///     List of Edge Channels.
        /// </returns>
        public static List<string> GetEdgeChannels()
        {
            var list = (from edgeChannel in (EdgeChannel[])Enum.GetValues(typeof(EdgeChannel))
                select edgeChannel.ToString()).ToList();
            return list;
        }


        /// <summary>
        ///     Get a list of all available Search Engines.
        /// </summary>
        /// <returns>
        ///     List of Search Engines.
        /// </returns>
        public static List<string> GetSearchEngines()
        {
            var list = (from searchEngine in (SearchEngine[])Enum.GetValues(typeof(SearchEngine))
                where searchEngine != SearchEngine.Custom
                select searchEngine.ToString()).ToList();

            try
            {
                var resourceValue =
                    (string)Application.Current.MainWindow!.FindResource("SettingsSearchEngineCustomItem");
                list.Add(!string.IsNullOrEmpty(resourceValue) ? resourceValue : "Custom");
            }
            catch
            {
                list.Add("Custom");
            }

            return list;
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
        public const string UninstallGuid = "{DC021E0D-1809-4102-8888-506D3121F1E9}";
        private const string UninstallRegistryPath = @$"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallGuid}";

        /// <summary>
        /// Create a Key in the Registry
        /// </summary>
        /// <param name="option">Name of key</param> 
        /// <param name="value">Value of key</param> 
        /// <param name="valueKind">Type of value</param> 
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param> 
        public static void SetKey(string option, object value, RegistryValueKind valueKind = RegistryValueKind.String, bool isUninstall = false)
        {
            try
            {
                using var key = isUninstall
                    ? Registry.LocalMachine.CreateSubKey(UninstallRegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree)
                    : Registry.LocalMachine.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                key.SetValue(option, value, valueKind); 
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
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param> 
        /// <returns>The value of the key if it exists, otherwise null.</returns>
        public static string GetKey(string option, bool isUninstall = false)
        {
            try
            {
                using var key = isUninstall
                    ? Registry.LocalMachine.OpenSubKey(UninstallRegistryPath)
                    : Registry.LocalMachine.OpenSubKey(RegistryPath); 
                if (key != null)
                {
                    var value = key.GetValue(option);
                    if (value != null)
                    {
                        return value.ToString()!;
                    }
                    Console.WriteLine($"Value for key '{option}' not found in the registry.");
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while reading the registry: " + ex.Message);
                return "";
            }
            Console.WriteLine($"Registry key '{RegistryPath}' not found.");
            return "";
        }

        /// <summary>
        /// Removes a Key in the Registry 
        /// </summary>
        /// <param name="option">Key Name</param>
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param> 
        public static bool RemoveKey(string option, bool isUninstall = false)
        {
            try
            {
                using var key = isUninstall
                    ? Registry.LocalMachine.OpenSubKey(UninstallRegistryPath)
                    : Registry.LocalMachine.OpenSubKey(RegistryPath); 
                var value = key?.GetValue(option);
                if (value != null)
                {
                    key?.DeleteValue(option);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while removing a key from the registry: " + ex.Message);
            }

            return false;
        }

        /// <summary> 
        /// Removes a SubKey in the Registry 
        /// </summary> 
        /// <param name="option">SubKey Name</param> 
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param> 
        public static bool RemoveSubKey(string option, bool isUninstall = false)
        {
            try
            {
                using var key = isUninstall
                    ? Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", true)
                    : Registry.LocalMachine.OpenSubKey(RegistryPath, true);
                key?.DeleteSubKey(option);
                key?.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred while removing a subkey from the registry: " + ex.Message);
            }

            return false;
        }
    }
}
