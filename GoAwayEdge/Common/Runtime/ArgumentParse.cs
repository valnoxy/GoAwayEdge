﻿using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge.Common.Runtime
{
    public class ArgumentParse
    {
        private static string? _url;

        /// <summary>
        /// Main method for parsing command line arguments and starting the appropriate process.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Parse(string[] args)
        {
            var quotedArgs = args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg);
            var argumentJoin = string.Join(" ", quotedArgs);
            DebugMessage.DisplayDebugMessage("GoAwayEdge", $"The following args are redirected (CTRL+C to copy):\n\n{argumentJoin}");

            if (RegistryConfig.GetKey("Enabled") == "False")
            {
                // Redirect to Edge
                StartProcess(FileConfiguration.NonIfeoPath, argumentJoin, "GoAwayEdge is disabled. Redirecting everything to Edge ...");
                return;
            }

            var (isFile, isApp, isOnlyEdge, singleArgument) = ParseArguments(args);
            if (_url != null)
            {
                HandleUrl(_url);
            }
            else if (isOnlyEdge)
            {
                StartProcess(FileConfiguration.NonIfeoPath, "", "Opening Microsoft Edge without any arguments.");
            }
            else if (isFile)
            {
                StartProcess(FileConfiguration.NonIfeoPath, $"--single-argument {singleArgument}", $"Opening '{singleArgument}' with Edge.");
            }
            else if (isApp)
            {
                const string pattern = @"--app-id=([^\s]+)";
                const string copilotAppId = "khiogjgiicnghciboipemonlmgelhblf";
                var match = Regex.Match(argumentJoin, pattern);
                if (match.Success)
                {
                    var appId = match.Groups[1].Value;
                    Logging.Log($"Opening PWA App with ID {appId}");
                    if (appId == copilotAppId && Common.Configuration.AiProvider != AiProvider.Default)
                    {
                        DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening AI Provider '{Configuration.AiProvider}' (PWA) ...");
                        UserInterface.CopilotDock.InterfaceManager.ShowDock();
                        return;
                    }
                }
                StartProcess(FileConfiguration.NonIfeoPath, singleArgument, $"Opening PWA Application with following arguments: '{singleArgument}'.");
            }
        }

        /// <summary>
        /// Parses the command line arguments to identify if a file path or app ID is present.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>A tuple with the values isFile, isApp, isOnlyEdge and singleArgument.</returns>
        private static (bool isFile, bool isApp, bool isOnlyEdge, string singleArgument) ParseArguments(string[] args)
        {
            bool isFile = false, isApp = false, collectSingleArgument = false, isOnlyEdge = false;
            var singleArgument = string.Empty;
            var cleanedArgs = args
                .Where(str => str != "--debug")
                .ToArray();
            if (cleanedArgs.Length == 1)
            {
                if (File.Exists(cleanedArgs[0]) && cleanedArgs[0].EndsWith("msedge.exe"))
                {
                    isOnlyEdge = true;
                }
            }

            foreach (var arg in cleanedArgs)
            {
                if (arg.Contains("microsoft-edge:"))
                {
                    _url = arg;
                }

                if (collectSingleArgument)
                {
                    singleArgument += (singleArgument.Length > 0 ? " " : "") + arg;
                    continue;
                }

                if (arg == "--single-argument" || arg.Contains("--app-id"))
                {
                    collectSingleArgument = true;
                }

                if (arg.Contains("--app-id"))
                {
                    singleArgument = arg;
                }
            }

            if (File.Exists(singleArgument))
                isFile = true;

            if (singleArgument.Contains("--app-id"))
                isApp = true;

            return (isFile, isApp, isOnlyEdge, singleArgument);
        }

        /// <summary>
        /// Handles processing and starting a process based on the given URL.
        /// </summary>
        /// <param name="url">The URL to be processed.</param>
        private static void HandleUrl(string url)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    FileName = FileConfiguration.NonIfeoPath,
                    Arguments = url
                }
            };

            // Copilot Taskbar
            if (url.Contains("microsoft-edge://?ux=copilot&tcp=1&source=taskbar") ||
                url.Contains("microsoft-edge:///?ux=copilot&tcp=1&source=taskbar"))
            {
                if (Configuration.AiProvider != AiProvider.Copilot)
                {
                    DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening AI Provider '{Configuration.AiProvider}' (Taskbar) ...");
                    UserInterface.CopilotDock.InterfaceManager.ShowDock();
                    return;
                }
                DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening Windows Copilot (Taskbar) with following url:\n{url}");
            }
            // Copilot Hotkey
            else if (url.Contains("microsoft-edge://?ux=copilot&tcp=1&source=hotkey") ||
                     url.Contains("microsoft-edge:///?ux=copilot&tcp=1&source=hotkey"))
            {
                if (Configuration.AiProvider != AiProvider.Copilot)
                {
                    DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening AI Provider '{Configuration.AiProvider}' (Hotkey) ...");
                    UserInterface.CopilotDock.InterfaceManager.ShowDock();
                    return;
                }
                DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening Windows Copilot (Hotkey) with following url:\n{url}");
            }
            // Weather Provider
            else if (url.Contains("%3Floc%3D") && url.Contains("msn.com")) // ?loc=
            {
                var parsedUrl = UrlParse.Parse(url);
                var parsedWeather = Weather.ParseUrl(parsedUrl);
                DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening Weather URL in default browser:\n\n{parsedWeather}");
                p.StartInfo.FileName = parsedWeather;
                p.StartInfo.Arguments = string.Empty;
            }
            // Default
            else
            {
                var parsedUrl = UrlParse.Parse(url);
                DebugMessage.DisplayDebugMessage("GoAwayEdge", $"Opening URL in default browser:\n\n{parsedUrl}");
                p.StartInfo.FileName = parsedUrl;
                p.StartInfo.Arguments = string.Empty;
            }

            p.Start();
        }

        /// <summary>
        /// Starts a process with the given file name and arguments.
        /// </summary>
        /// <param name="fileName">The file name to start.</param>
        /// <param name="arguments">The arguments to pass to the process.</param>
        /// <param name="debugMessage">The debug message to display.</param>
        private static void StartProcess(string fileName, string arguments, string debugMessage)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    FileName = fileName,
                    Arguments = arguments
                }
            };

            DebugMessage.DisplayDebugMessage("GoAwayEdge", debugMessage);
            p.Start();
        }

        public static EdgeChannel ParseEdgeChannel(string argument)
        {
            return argument.ToLower() switch
            {
                "stable" => EdgeChannel.Stable,
                "beta" => EdgeChannel.Beta,
                "dev" => EdgeChannel.Dev,
                "canary" => EdgeChannel.Canary,
                _ => EdgeChannel.Stable // Fallback channel
            };
        }

        public static AiProvider ParseAiProvider(string argument)
        {
            var arg = argument;
            if (argument.StartsWith("-a:"))
                arg = argument.Remove(0, 3);

            return arg.ToLower() switch
            {
                "default" => AiProvider.Default,
                "copilot" => AiProvider.Copilot,
                "chatgpt" => AiProvider.ChatGPT,
                "gemini" => AiProvider.Gemini,
                "github_copilot" => AiProvider.GitHub_Copilot,
                "grok" => AiProvider.Grok,
                "custom" => AiProvider.Custom,
                _ => AiProvider.Default // Fallback provider
            };
        }

        public static SearchEngine ParseSearchEngine(string argument)
        {
            var arg = argument;
            if (argument.StartsWith("-e:"))
                arg = argument.Remove(0, 3);

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

        public static WeatherProvider ParseWeatherProvider(string argument)
        {
            var arg = argument;
            if (argument.StartsWith("-w:"))
                arg = argument.Remove(0, 3);

            return arg.ToLower() switch
            {
                "default" => WeatherProvider.Default,
                "weathercom" => WeatherProvider.WeatherCom,
                "accuweather" => WeatherProvider.AccuWeather,
                "custom" => WeatherProvider.Custom,
                _ => WeatherProvider.Default // Fallback provider
            };
        }
    }
}
