using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;
using GoAwayEdge.Common.Debugging;
using ManagedShell;
using Microsoft.Win32;

namespace GoAwayEdge.Common
{
    public enum SearchEngine
    {
        [Description("https://google.com/search?q=")]
        Google,
        
        [Description("https://bing.com/search?q=")]
        Bing,
        
        [Description("https://duckduckgo.com/?q=")]
        DuckDuckGo,
        
        [Description("https://search.yahoo.com/search?p=")]
        Yahoo,
        
        [Description("https://yandex.com/search/?text=")]
        Yandex,
        
        [Description("https://ecosia.org/search?q=")]
        Ecosia,
        
        [Description("https://ask.com/web?q=")]
        Ask,
        
        [Description("https://qwant.com/?q=")]
        Qwant,
        
        [Description("https://perplexity.ai/search?copilot=false&q=")]
        Perplexity,

        Custom
    }

    public enum AiProvider
    {
        Default,

        [Description("https://copilot.microsoft.com/")]
        Copilot,

        [Description("https://chatgpt.com/")]
        ChatGPT,
        
        [Description("https://gemini.google.com/")]
        Gemini,
        
        [Description("https://github.com/copilot")]
        GitHub_Copilot,
        
        [Description("https://x.com/i/grok")]
        Grok,
        
        Custom
    }

    public enum EdgeChannel
    {
        Stable,
        Beta,
        Dev,
        Canary
    }

    public enum WeatherProvider
    {
        Default,

        [Description("https://weather.com/{country-code}/weather/today/l/{latitude},{longitude}")]
        WeatherCom,

        [Description("https://accuweather.com/{short-country-code}/search-locations?query={latitude},{longitude}")]
        AccuWeather,

        Custom
    }

    internal class Configuration
    {
        public static EdgeChannel Channel { get; set; }
        public static SearchEngine Search { get; set; }
        public static AiProvider AiProvider { get; set; }
        public static WeatherProvider WeatherProvider { get; set; }
        public static bool LicenseAccepted { get; set; }
        public static bool Uninstall { get; set; }
        public static bool UninstallEdge { get; set; }
        public static bool NoEdgeInstalled { get; set; }
        public static bool InstallControlPanel { get; set; }
        public static string? CustomQueryUrl { get; set; }
        public static string? CustomAiProviderUrl { get; set; }
        public static string? CustomWeatherProviderUrl { get; set; }

        public static string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "valnoxy",
            "GoAwayEdge");

        public static string UserDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "valnoxy",
            "GoAwayEdge");

        public static ShellManager? ShellManager { get; set; }
        public static bool AppBarIsAttached { get; set; }
        public static string? CopilotExternalApp { get; set; }
        public static string? CopilotExternalAppArgument { get; set; }


        /// <summary>
        /// Initialize the current environment.
        /// </summary>
        /// <returns>
        ///     Boolean status of the initialization.
        /// </returns>
        public static bool InitialEnvironment(bool setupRunning = false)
        {
            // Check if Edge is installed
            try
            {
                Logging.Log("Initialize environment ...");
                NoEdgeInstalled = !File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"));

                if (NoEdgeInstalled)
                {
                    Logging.Log("Edge is not installed on this device - completed initialization");
                    return true;
                }

                if (!IsCopilotDockPipeAvailable() && !setupRunning)
                {
                    Logging.Log("Copilot Dock (Pipe) is not available - spawning ShellManager");
                    try { ShellManager = new ShellManager(); }
                    catch (Exception ex)
                    {
                        Logging.Log("An error has occurred while initializing the ShellManager: " + ex.Message, Logging.LogLevel.ERROR);
                    }
                }

                Logging.Log("Fetching settings from registry ...");
                FileConfiguration.EdgePath = RegistryConfig.GetKey("EdgeFilePath");
                FileConfiguration.NonIfeoPath = RegistryConfig.GetKey("EdgeNonIEFOFilePath");
                try
                {
                    Channel = Runtime.ArgumentParse.ParseEdgeChannel(RegistryConfig.GetKey("EdgeChannel"));
                    Search = Runtime.ArgumentParse.ParseSearchEngine(RegistryConfig.GetKey("SearchEngine"));
                    AiProvider = Runtime.ArgumentParse.ParseAiProvider(RegistryConfig.GetKey("AiProvider", userSetting: true));
                    WeatherProvider = Runtime.ArgumentParse.ParseWeatherProvider(RegistryConfig.GetKey("WeatherProvider", userSetting: true));
                    CopilotExternalApp = RegistryConfig.GetKey("ExternalApp", userSetting: true);
                    CopilotExternalAppArgument = RegistryConfig.GetKey("ExternalAppArgs", userSetting: true);
                    if (Search == SearchEngine.Custom)
                    {
                        CustomQueryUrl = RegistryConfig.GetKey("CustomQueryUrl");
                    }
                    if (AiProvider == AiProvider.Custom)
                    {
                        CustomAiProviderUrl = RegistryConfig.GetKey("CustomAiProviderUrl", userSetting: true);
                    }
                    if (WeatherProvider == WeatherProvider.Custom)
                    {
                        CustomWeatherProviderUrl = RegistryConfig.GetKey("CustomWeatherProviderUrl", userSetting: true);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log("An error has occurred while reading the registry: " + ex.Message, Logging.LogLevel.ERROR);
                }
                Logging.Log($"Value of EdgePath: {FileConfiguration.EdgePath}");
                Logging.Log($"Value of NonIfeoPath: {FileConfiguration.NonIfeoPath}");
                Logging.Log($"Value of Channel: {Channel}");
                Logging.Log($"Value of Search: {Search}");
                Logging.Log($"Value of AiProvider: {AiProvider}");
                Logging.Log($"Value of WeatherProvider: {WeatherProvider}");
                Logging.Log($"Value of CustomQueryUrl: {CustomQueryUrl}");
                Logging.Log($"Value of CustomAiProviderUrl: {CustomAiProviderUrl}");
                Logging.Log($"Value of CustomWeatherProviderUrl: {CustomWeatherProviderUrl}");
                Logging.Log($"Value of CopilotExternalApp: {CopilotExternalApp}");
                Logging.Log($"Value of CopilotExternalAppArgument: {CopilotExternalAppArgument}");
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

        public static bool IsCopilotDockPipeAvailable()
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", "GoAwayEdge_CopilotDockPipe", PipeDirection.In);
                pipeClient.Connect(1000);
                return true;
            }
            catch (TimeoutException)
            {
                // Pipe does not exist or is not available
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logging.Log($"Access Denied for Pipe 'GoAwayEdge_CopilotDockPipe': {ex.Message}", Logging.LogLevel.ERROR);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to check for the pipe 'GoAwayEdge_CopilotDockPipe': {ex.Message}", Logging.LogLevel.ERROR);
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
                where aiProvider != AiProvider.Custom && aiProvider != AiProvider.Default
                select aiProvider.ToString().Replace("_", " ")).ToList();

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

            try
            {
                var resourceValueDefault =
                    (string)Application.Current.MainWindow!.FindResource("Default");
                list.Insert(0, !string.IsNullOrEmpty(resourceValueDefault) ? resourceValueDefault : "Default");
            }
            catch
            {
                list.Insert(0, "Default");
            }

            return list;
        }

        /// <summary>
        ///     Get a list of all available Weather Services.
        /// </summary>
        /// <returns>
        ///     List of Weather Services.
        /// </returns>
        public static List<string> GetWeatherProviders()
        {
            var list = (from weatherProvider in (WeatherProvider[])Enum.GetValues(typeof(WeatherProvider))
                where weatherProvider != WeatherProvider.Custom && weatherProvider != WeatherProvider.Default
                select weatherProvider.ToString().Replace("_", " ")).ToList();

            try
            {
                var resourceValueCustom =
                    (string)Application.Current.MainWindow!.FindResource("SettingsSearchEngineCustomItem");
                list.Add(!string.IsNullOrEmpty(resourceValueCustom) ? resourceValueCustom : "Custom");
            }
            catch
            {
                list.Add("Custom");
            }

            try
            {
                var resourceValueDefault =
                    (string)Application.Current.MainWindow!.FindResource("Default");
                list.Insert(0, !string.IsNullOrEmpty(resourceValueDefault) ? resourceValueDefault : "Default");
            }
            catch
            {
                list.Insert(0, "Default");
            }

            return list;
        }

        /// <summary>
        /// Retrieves the description of an enumeration value based on the <see cref="DescriptionAttribute"/>.
        /// </summary>
        /// <param name="value">The enumeration value for which to retrieve the description.</param>
        /// <returns>
        /// The description defined by the <see cref="DescriptionAttribute"/> if it exists; 
        /// otherwise, the string representation of the enumeration value.
        /// </returns>
        /// <example>
        /// Example usage:
        /// <code>
        /// public enum SampleEnum
        /// {
        ///     [Description("First Value")]
        ///     First,
        ///     
        ///     [Description("Second Value")]
        ///     Second,
        ///     
        ///     Third // No DescriptionAttribute
        /// }
        /// 
        /// var value = SampleEnum.First;
        /// string description = GetEnumDescription(value); // "First Value"
        /// 
        /// value = SampleEnum.Third;
        /// description = GetEnumDescription(value); // "Third"
        /// </code>
        /// </example>
        public static string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fieldInfo!.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
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
