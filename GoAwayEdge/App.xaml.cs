/*
 * GoAwayEdge
 * Copyright (c) 2018 - 2025 Exploitox (valnoxy).
 *
 * GoAwayEdge is licensed under MIT License (https://github.com/valnoxy/GoAwayEdge/blob/main/LICENSE).
 * So you are allowed to use freely and modify the application.
 * I will not be responsible for any outcome.
 * Proceed with any action at your own risk.
 *
 * Source code: https://github.com/valnoxy/GoAwayEdge
 */

using GoAwayEdge.Common;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using GoAwayEdge.Common.Runtime;
using GoAwayEdge.Common.Debugging;
using GoAwayEdge.Common.Installation;
using InstallRoutine = GoAwayEdge.Common.Installation.InstallRoutine;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für App.xaml
    /// </summary>
    public partial class App
    {
        public static bool IsDebug = false;

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
            switch (args.Length)
            {
                // Opens Installer
                case 0:
                case 1 when args.Contains("--debug"):
                {
                    if (args.Contains("--debug"))
                            IsDebug = true;
                    if (IsAdministrator() == false)
                    {
                        ElevateAsAdmin();
                        Environment.Exit(0);
                        return;
                    }

                    var installer = new UserInterface.Setup.Installer();
                    installer.ShowDialog();
                    Environment.Exit(0);
                    break;
                }
                case > 0:
                {
                    if (args.Contains("--debug"))
                        IsDebug = true;
                    if (args.Contains("-ToastActivated")) // Clicked on notification, ignore it.
                        Environment.Exit(0);
                    if (args.Contains("--control-panel"))
                    {
                        if (IsAdministrator() == false)
                        {
                            ElevateAsAdmin(string.Join(" ", args));
                            Environment.Exit(0);
                            return;
                        }

                        // Check if user allowed opening the control panel
                        if (RegistryConfig.GetKey("ControlPanelIsInstalled") != "True")
                        {
                            Logging.Log("Control Panel is not allowed on this system, exiting ...", Logging.LogLevel.ERROR);
                            Environment.Exit(0);
                            return;
                        }

                        Configuration.InitialEnvironment();
                        var controlCenter = new UserInterface.ControlPanel.ControlPanel();
                        controlCenter.ShowDialog();
                        Environment.Exit(0);
                    }

                    if (args.Contains("--copilot-dock"))
                    {
                        Configuration.InitialEnvironment();

                        if (Configuration.AiProvider != AiProvider.Default)
                        {
                            DebugMessage.DisplayDebugMessage("GoAwayEdge",
                                $"Opening AI Provider '{Configuration.AiProvider}' (Triggered with argument) ...");
                            UserInterface.CopilotDock.InterfaceManager.ShowDock();
                            Environment.Exit(0);
                        }
                        else
                        {
                            IsDebug = true;
                            DebugMessage.DisplayDebugMessage("GoAwayEdge",
                                "You cannot open the Copilot dock if your AI provider is set to default");
                            Environment.Exit(1);
                        }
                    }

                    if (args.Contains("-s")) // Silent Installation
                    {
                        foreach (var arg in args)
                        {
                            if (arg.StartsWith("-e:"))
                                Configuration.Search = ArgumentParse.ParseSearchEngine(arg);
                            if (arg.Contains("--search-url:"))
                            {
                                Configuration.CustomQueryUrl = ParseCustomUrl(arg, 13);
                                Configuration.Search = !string.IsNullOrEmpty(Configuration.CustomQueryUrl)
                                    ? SearchEngine.Custom
                                    : SearchEngine.Google;
                            }

                            if (arg.Contains("-a:"))
                                Configuration.AiProvider = ArgumentParse.ParseAiProvider(arg);
                            if (arg.Contains("--ai-url:"))
                            {
                                Configuration.CustomAiProviderUrl = ParseCustomUrl(arg, 9);
                                Configuration.AiProvider = !string.IsNullOrEmpty(Configuration.CustomAiProviderUrl)
                                    ? AiProvider.Custom
                                    : AiProvider.Default;
                            }

                            if (arg.Contains("-w:"))
                                Configuration.WeatherProvider = ArgumentParse.ParseWeatherProvider(arg);
                            if (arg.Contains("--weather-url:"))
                            {
                                Configuration.CustomWeatherProviderUrl = ParseCustomUrl(arg, 14);
                                Configuration.WeatherProvider = !string.IsNullOrEmpty(Configuration.CustomWeatherProviderUrl)
                                    ? WeatherProvider.Custom
                                    : WeatherProvider.Default;
                            }
                        }

                        if (IsAdministrator() == false)
                        {
                            ElevateAsAdmin(string.Join(" ", args));
                            Environment.Exit(0);
                            return;
                        }

                        Configuration.InitialEnvironment();
                        var result = InstallRoutine.Install(null);
                        Environment.Exit(result);
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
                                        ElevateAsAdmin("--update");

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
                                        ElevateAsAdmin("--update");
                                    
                                    Environment.Exit(0);
                                }
                                Updater.ModifyIfeoBinary(ModifyAction.Create);
                                break;
                        }
                        Environment.Exit(0);
                    }

                    break;
                }
            }

            Configuration.InitialEnvironment();
            ArgumentParse.Parse(args);
            Environment.Exit(0);
        }

        private static void ElevateAsAdmin(string? arguments = null)
        {
            // Restart program and run as admin
            var exeName = Process.GetCurrentProcess().MainModule?.FileName;
            if (exeName == null) return;
            var startInfo = new ProcessStartInfo(exeName)
            {
                Verb = "runas",
                UseShellExecute = true,
                Arguments = arguments
            };
            Process.Start(startInfo);
        }

        private static string? ParseCustomUrl(string argument, int count)
        {
            var argParsed = argument.Remove(0, count);
            var result = Uri.TryCreate(argParsed, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result ? argParsed : null;
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
