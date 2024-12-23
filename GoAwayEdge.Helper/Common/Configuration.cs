using GoAwayEdge.Helper.Common.Debugging;
using Microsoft.Win32;

namespace GoAwayEdge.Helper.Common
{
    internal class Configuration
    {
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
                Logging.Log("Fetching settings from registry ...");

                try
                {
                    CopilotExternalApp = RegistryConfig.GetKey("ExternalApp", userSetting: true);
                    CopilotExternalAppArgument = RegistryConfig.GetKey("ExternalAppArgs", userSetting: true);
                }
                catch (Exception ex)
                {
                    Logging.Log("An error has occurred while reading the registry: " + ex.Message, Logging.LogLevel.ERROR);
                }
                Logging.Log($"Value of CopilotExternalApp: {CopilotExternalApp}");
                Logging.Log($"Value of CopilotExternalAppArgument: {CopilotExternalAppArgument}");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log($"Error while initializing environment: {ex.Message}", Logging.LogLevel.ERROR);
                return false;
            }
        }
    }
    
    public class RegistryConfig
    {
        private const string Company = "valnoxy";
        private const string Product = "GoAwayEdge";
        private const string RegistryPath = @$"SOFTWARE\{Company}\{Product}";
        public const string UninstallGuid = "{DC021E0D-1809-4102-8888-506D3121F1E9}";
        private const string UninstallRegistryPath = @$"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallGuid}";

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
    }
}
