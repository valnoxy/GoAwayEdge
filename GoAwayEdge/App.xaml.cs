/*
 * Go away Edge - IFEO Method
 * by valnoxy (valnoxy.dev)
 * ----------------------------------
 *  HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe
 *   > UseFilter (DWORD) = 1
 *
 *  HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0
 *   > Debugger (REG_SZ) = "Path\To\GoAwayEdge.exe"
 *   > FullFilterPath (REG_SZ) = C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
 */

using GoAwayEdge.Common;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Windows;
using GoAwayEdge.Common.Runtime;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für App.xaml
    /// </summary>
    public partial class App
    {
        public static bool IsDebug = false;
        private static string? _url;
        
        public void Application_Startup(object sender, StartupEventArgs e)
        {
#if DEBUG
            IsDebug = true;
#endif
            // Initialize the logging system
            Logging.Initialize();

            // Load Language
            LocalizationManager.LoadLanguage();

            string[] args = e.Args;
            if (args.Length == 0) // Opens Installer
            {
                if (IsAdministrator() == false)
                {
                    // Restart program and run as admin
                    var exeName = Process.GetCurrentProcess().MainModule?.FileName;
                    if (exeName != null)
                    {
                        var startInfo = new ProcessStartInfo(exeName)
                        {
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                    }
                    Environment.Exit(0);
                    return;
                }

                var installer = new Installer();
                installer.ShowDialog();
                Environment.Exit(0);
            }
            else if (args.Length > 0)
            {
                if (args.Contains("-ToastActivated")) // Clicked on notification, ignore it.
                    Environment.Exit(0);
                if (args.Contains("-s")) // Silent Installation
                {
                    foreach (var arg in args)
                    {
                        if (arg.StartsWith("-se:"))
                            Configuration.Search = ParseSearchEngine(arg);
                        if (arg.Contains("--url:"))
                        {
                            Configuration.CustomQueryUrl = ParseCustomSearchEngine(arg);
                            Configuration.Search = !string.IsNullOrEmpty(Configuration.CustomQueryUrl) ? SearchEngine.Custom : SearchEngine.Google;
                        }
                    }

                    if (IsAdministrator() == false)
                    {
                        // Restart program and run as admin
                        var exeName = Process.GetCurrentProcess().MainModule?.FileName;
                        if (exeName != null)
                        {
                            var startInfo = new ProcessStartInfo(exeName)
                            {
                                Verb = "runas",
                                UseShellExecute = true,
                                Arguments = string.Join(" ", args)
                            };
                            Process.Start(startInfo);
                        }
                        Environment.Exit(0);
                        return;
                    }

                    Configuration.InitialEnvironment();
                    InstallRoutine.Install(null);
                    Environment.Exit(0);
                }

                if (args.Contains("-u"))
                {
                    InstallRoutine.Uninstall(null);
                    Environment.Exit(0);
                }
                if (args.Contains("--update"))
                {
                    var statusEnv = Configuration.InitialEnvironment();
                    if (statusEnv == false) Environment.Exit(1);

                    // Check for app update
                    var updateAvailable = Updater.CheckForAppUpdate();

                    var updateSkipped = RegistryConfig.GetKey("SkipVersion");
                    if (updateAvailable == updateSkipped)
                        Environment.Exit(0);

                    if (!string.IsNullOrEmpty(updateAvailable))
                    {
                        var updateMessage = LocalizationManager.LocalizeValue("NewUpdateAvailable", updateAvailable);
                        var remindMeLaterBtn = LocalizationManager.LocalizeValue("RemindMeLater");
                        var installUpdateBtn = LocalizationManager.LocalizeValue("InstallUpdate");
                        var skipUpdateUpdateBtn = LocalizationManager.LocalizeValue("SkipUpdate");

                        var updateDialog = new MessageUi("GoAwayEdge", updateMessage, installUpdateBtn, remindMeLaterBtn, skipUpdateUpdateBtn, true);
                        updateDialog.ShowDialog();
                        switch (updateDialog.Summary)
                        {
                            case "Btn1":
                            {
                                var updateResult = Updater.UpdateClient();
                                if (!updateResult) Environment.Exit(0);
                                break;
                            }
                            case "Btn3":
                                RegistryConfig.SetKey("SkipVersion", updateAvailable);
                                Environment.Exit(0);
                                break;
                        }
                    }

                    // Validate Ifeo binary
                    var binaryStatus = Updater.ValidateIfeoBinary();
                    switch (binaryStatus)
                    {
                        case 0: // validated
                            break;
                        case 1: // failed validation
                            if (IsAdministrator() == false)
                            {
                                var updateNonIfeoMessage = LocalizationManager.LocalizeValue("NewNonIfeoUpdate");
                                var remindMeLaterBtn = LocalizationManager.LocalizeValue("RemindMeLater");
                                var installUpdateBtn = LocalizationManager.LocalizeValue("InstallUpdate");

                                var ifeoMessageUi = new MessageUi("GoAwayEdge", updateNonIfeoMessage, installUpdateBtn, remindMeLaterBtn);
                                ifeoMessageUi.ShowDialog();

                                if (ifeoMessageUi.Summary == "Btn1")
                                {
                                    // Restart program and run as admin
                                    var exeName = Process.GetCurrentProcess().MainModule?.FileName;
                                    if (exeName != null)
                                    {
                                        var startInfo = new ProcessStartInfo(exeName)
                                        {
                                            Verb = "runas",
                                            UseShellExecute = true,
                                            Arguments = "--update"
                                        };
                                        Process.Start(startInfo);
                                    }
                                    Environment.Exit(0);
                                    return;
                                }
                                Environment.Exit(0);
                            }
                            Updater.ModifyIfeoBinary(ModifyAction.Update);
                            break;
                        case 2: // missing
                            if (IsAdministrator() == false)
                            {
                                var ifeoMissingMessage = LocalizationManager.LocalizeValue("MissingIfeoFile");
                                var yesBtn = LocalizationManager.LocalizeValue("Yes");
                                var noBtn = LocalizationManager.LocalizeValue("No");
                                var ifeoMessageUi = new MessageUi("GoAwayEdge", ifeoMissingMessage, yesBtn, noBtn);
                                ifeoMessageUi.ShowDialog();

                                if (ifeoMessageUi.Summary == "Btn1")
                                {
                                    // Restart program and run as admin
                                    var exeName = Process.GetCurrentProcess().MainModule?.FileName;
                                    if (exeName != null)
                                    {
                                        var startInfo = new ProcessStartInfo(exeName)
                                        {
                                            Verb = "runas",
                                            UseShellExecute = true,
                                            Arguments = "--update"
                                        };
                                        Process.Start(startInfo);
                                    }
                                    Environment.Exit(0);
                                    return;
                                }
                                Environment.Exit(0);
                            }
                            Updater.ModifyIfeoBinary(ModifyAction.Create);
                            break;
                    }
                    Environment.Exit(0);
                }
            }

            Configuration.InitialEnvironment();
            try
            {
                Configuration.Search = ParseSearchEngine(RegistryConfig.GetKey("SearchEngine"));
            }
            catch
            {
                // ignored
            }

            ArgumentParse.Parse(args);
            Environment.Exit(0);
        }
        
        private static string? ParseCustomSearchEngine(string argument)
        {
            var argParsed = argument.Remove(0, 6);
            var result = Uri.TryCreate(argParsed, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result ? argParsed : null;
        }

        private static SearchEngine ParseSearchEngine(string argument)
        {
            var arg = argument;
            if (argument.StartsWith("-se:"))
                arg = argument.Remove(0, 4);
            
            return arg.ToLower() switch
            {
                "google" => SearchEngine.Google,
                "bing" => SearchEngine.Bing,
                "duckduckgo" => SearchEngine.DuckDuckGo,
                "yahoo" => SearchEngine.Yahoo,
                "yandex" => SearchEngine.Yandex,
                "ecosia" => SearchEngine.Ecosia,
                "ask" => SearchEngine.Ask,
                "qwant" => SearchEngine.Qwant,
                "perplexity" => SearchEngine.Perplexity,
                "custom" => SearchEngine.Custom,
                _ => SearchEngine.Google // Fallback search engine
            };
        }
        
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
