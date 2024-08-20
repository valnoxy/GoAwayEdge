using System.IO;
using System.Windows;
using GoAwayEdge.Common.Debugging;
using ManagedShell;
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

    public enum AiProvider
    {
        Copilot,
        ChatGPT,
        Gemini,
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
        public static AiProvider Provider { get; set; }
        public static bool Uninstall { get; set; }
        public static bool UninstallEdge { get; set; }
        public static bool NoEdgeInstalled { get; set; }
        public static bool InstallControlPanel { get; set; }
        public static string? CustomQueryUrl { get; set; }
        public static string? CustomProviderUrl { get; set; }

        public static string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "valnoxy",
            "GoAwayEdge");
        public static ShellManager ShellManager { get; set; }
        public static bool AppBarIsAttached { get; set; }


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

                if (NoEdgeInstalled)
                    return true;

                ShellManager = new ShellManager();

                FileConfiguration.EdgePath = RegistryConfig.GetKey("EdgeFilePath");
                FileConfiguration.NonIfeoPath = RegistryConfig.GetKey("EdgeNonIEFOFilePath");
                try
                {
                    Channel = Runtime.ArgumentParse.ParseEdgeChannel(RegistryConfig.GetKey("EdgeChannel"));
                    Search = Runtime.ArgumentParse.ParseSearchEngine(RegistryConfig.GetKey("SearchEngine"));
                    Provider = Runtime.ArgumentParse.ParseAiProvider(RegistryConfig.GetKey("AiProvider", userSetting: true));
                    if (Search == SearchEngine.Custom)
                    {
                        CustomQueryUrl = RegistryConfig.GetKey("CustomQueryUrl");
                    }
                    if (Provider == AiProvider.Custom)
                    {
                        CustomProviderUrl = RegistryConfig.GetKey("CustomProviderUrl", userSetting: true);
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

        /// <summary>
        ///     Get a list of all available AI Providers.
        /// </summary>
        /// <returns>
        ///     List of AI Providers.
        /// </returns>
        public static List<string> GetAiProviders()
        {
            var list = (from aiProvider in (AiProvider[])Enum.GetValues(typeof(AiProvider))
                where aiProvider != AiProvider.Custom
                select aiProvider.ToString()).ToList();

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
        /// <param name="userSetting">Use the CurrentUser Registry key instead.</param> 
        public static void SetKey(string option, object value, RegistryValueKind valueKind = RegistryValueKind.String,
            bool isUninstall = false, bool userSetting = false)
        {
            try
            {
                RegistryKey? key;
                if (userSetting)
                {
                    key = Registry.CurrentUser.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                else if (isUninstall)
                {
                    key = Registry.LocalMachine.CreateSubKey(UninstallRegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                else
                {
                    key = Registry.LocalMachine.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                key.SetValue(option, value, valueKind); 
            }
            catch (Exception ex)
            {
                Logging.Log("An error has occurred while writing to the registry: " + ex.Message, Logging.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Retrieves the value of a key from the Registry.
        /// </summary>
        /// <param name="option">Name of the key.</param>
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param> 
        /// <param name="userSetting">Use the CurrentUser Registry key instead.</param> 
        /// <returns>The value of the key if it exists, otherwise null.</returns>
        public static string GetKey(string option, bool isUninstall = false, bool userSetting = false)
        {
            try
            {
                RegistryKey? key;
                if (userSetting)
                {
                    key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                }
                else if (isUninstall)
                {
                    key = Registry.LocalMachine.OpenSubKey(UninstallRegistryPath);
                }
                else
                {
                    key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                }
                if (key != null)
                {
                    var value = key.GetValue(option);
                    if (value != null)
                    {
                        return value.ToString()!;
                    }
                    Logging.Log($"Value for key '{option}' not found in the registry.", Logging.LogLevel.ERROR);
                    return "";
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"An error has occurred while reading the registry: {ex.Message}", Logging.LogLevel.ERROR);
                return "";
            }
            Logging.Log($"Registry key '{RegistryPath}' not found.", Logging.LogLevel.ERROR);
            return "";
        }

        /// <summary>
        /// Removes a Key in the Registry 
        /// </summary>
        /// <param name="option">Key Name</param>
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param>
        /// <param name="userSetting">Use the CurrentUser Registry key instead.</param>
        public static bool RemoveKey(string option, bool isUninstall = false, bool userSetting = false)
        {
            try
            {
                RegistryKey? key;
                if (userSetting)
                {
                    key = Registry.CurrentUser.OpenSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                else if (isUninstall)
                {
                    key = Registry.LocalMachine.OpenSubKey(UninstallRegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                else
                {
                    key = Registry.LocalMachine.OpenSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                var value = key?.GetValue(option);
                if (value != null)
                {
                    key?.DeleteValue(option);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("An error has occurred while removing a key from the registry: " + ex.Message, Logging.LogLevel.ERROR);
            }

            return false;
        }

        /// <summary> 
        /// Removes a SubKey in the Registry 
        /// </summary> 
        /// <param name="option">SubKey Name</param> 
        /// <param name="isUninstall">Use the Uninstall Registry key instead.</param>
        /// <param name="userSetting">Use the CurrentUser Registry key instead.</param>
        public static bool RemoveSubKey(string option, bool isUninstall = false, bool userSetting = false)
        {
            try
            {
                RegistryKey? key;
                if (userSetting)
                {
                    key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                }
                else if (isUninstall)
                {
                    key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", true);
                }
                else
                {
                    key = Registry.LocalMachine.OpenSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                key?.DeleteSubKey(option);
                key?.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("An error has occurred while removing a subkey from the registry: " + ex.Message, Logging.LogLevel.ERROR);
            }

            return false;
        }
    }
}
