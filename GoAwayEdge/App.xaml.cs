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
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Windows;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für App.xaml
    /// </summary>
    public partial class App
    {
        private static string? _url;

        public void Application_Startup(object sender, StartupEventArgs e)
        {
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
                    Current.Shutdown();
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
                    if (!string.IsNullOrEmpty(updateAvailable))
                    {
                        var updateDialog = new MessageUi("GoAwayEdge",
                            $"A new version is available: Version {updateAvailable}\nShould the update be performed now?", "No", "Yes", true);
                        updateDialog.ShowDialog();
                        if (updateDialog.Summary == "Btn2")
                        {
                            var updateResult = Updater.UpdateClient();
                            if (updateResult == 0) Environment.Exit(0);
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
                                var ifeoMessageUi = new MessageUi("GoAwayEdge",
                                    "The IFEO exclusion file needs to be updated. Update now?", "No", "Yes");
                                ifeoMessageUi.ShowDialog();

                                if (ifeoMessageUi.Summary == "Btn2")
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
                                    Current.Shutdown();
                                    return;
                                }
                                Environment.Exit(0);
                            }
                            Updater.ModifyIfeoBinary(ModifyAction.Update);
                            break;
                        case 2: // missing
                            if (IsAdministrator() == false)
                            {
                                var ifeoMessageUi = new MessageUi("GoAwayEdge",
                                        "The IFEO exclusion file is missing and need to be copied. Copy now?", "No", "Yes");
                                ifeoMessageUi.ShowDialog();

                                if (ifeoMessageUi.Summary == "Btn2")
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
                                    Current.Shutdown();
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
            RunParser(args);

            Environment.Exit(0);
        }

        public static void RunParser(string[] args)
        {
            var argumentJoin = string.Join(" ", args);
#if DEBUG
            var w = new MessageUi("GoAwayEdge",
                $"The following args are redirected (CTRL+C to copy):\n\n{argumentJoin}", "OK", null, true);
            w.ShowDialog();
#endif
            Output.WriteLine("Command line args:\n\n" + argumentJoin + "\n", ConsoleColor.Gray);

            // Filter command line args
            foreach (var arg in args)
            {
                if (arg.Contains("microsoft-edge:"))
                {
                    _url = arg;
                }
                if (!args.Contains("--profile-directory") && !ContainsParsedData(args) && args.Length != 1) continue; // Start Edge (default browser on this system)

#if DEBUG
                var messageUi = new MessageUi("GoAwayEdge",
                    "Microsoft Edge will now start normally via IFEO application.", "OK", null, true);
                messageUi.ShowDialog();
#endif
                var parsedArgs = args.Skip(2);
                var p = new Process();
                p.StartInfo.FileName = FileConfiguration.NonIfeoPath;
                p.StartInfo.Arguments = string.Join(" ", parsedArgs);
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.RedirectStandardOutput = false;
                p.Start();
                Environment.Exit(0);
            }

            // Open URL in default browser
            if (_url != null)
            {
                var p = new Process();

                // Windows Copilot
                if (_url.Contains("microsoft-edge://?ux=copilot&tcp=1&source=taskbar")
                    || _url.Contains("microsoft-edge:///?ux=copilot&tcp=1&source=taskbar"))
                {
                    p.StartInfo.FileName = FileConfiguration.NonIfeoPath;
                    p.StartInfo.Arguments = _url;
                    Output.WriteLine($"Opening Windows Copilot with following url:\n{_url}", ConsoleColor.Gray);
#if DEBUG
                    var copilotMessageUi = new MessageUi("GoAwayEdge",
                        $"Opening Windows Copilot with following url:\n{_url}", "OK", null, true);
                    copilotMessageUi.ShowDialog();
#endif
                }
                else
                {
                    var parsed = ParseUrl(_url);
                    Output.WriteLine("Opening URL in default browser:\n\n" + parsed + "\n", ConsoleColor.Gray);
#if DEBUG
                    var defaultUrlMessageUi = new MessageUi("GoAwayEdge",
                        "Opening URL in default browser:\n\n" + parsed + "\n", "OK", null, true);
                    defaultUrlMessageUi.ShowDialog();
#endif
                    p.StartInfo.FileName = parsed;
                    p.StartInfo.Arguments = "";
                }
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.RedirectStandardOutput = false;
                p.Start();
            }
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

        private static bool ContainsParsedData(IEnumerable<string> args)
        {
            var contains = false;
            var engineUrl = DefineEngine(Configuration.Search);

            foreach (var arg in args)
            {
                if (arg.Contains(engineUrl))
                    contains = true;
            }
            return contains;
        }

        private static string ParseUrl(string encodedUrl)
        {
            // Remove URI handler with url argument prefix
            encodedUrl = encodedUrl[encodedUrl.IndexOf("http", StringComparison.Ordinal)..];

            // Remove junk after search term
            if (encodedUrl.Contains("https%3A%2F%2Fwww.bing.com%2Fsearch%3Fq%3D") && !encodedUrl.Contains("redirect"))
                encodedUrl = encodedUrl.Substring(encodedUrl.IndexOf("http", StringComparison.Ordinal), encodedUrl.IndexOf("%26", StringComparison.Ordinal));

            // Alternative url form
            if (encodedUrl.Contains("https%3A%2F%2Fwww.bing.com%2Fsearch%3Fform%3D"))
            {
                encodedUrl = encodedUrl.Substring(encodedUrl.IndexOf("26q%3D", StringComparison.Ordinal) + 6, encodedUrl.Length - (encodedUrl.IndexOf("26q%3D", StringComparison.Ordinal) + 6));
                encodedUrl = "https://www.bing.com/search?q=" + encodedUrl;
            }

            // Decode Url
            encodedUrl = encodedUrl.Contains("redirect") ? DotSlash(encodedUrl) : DecodeUrlString(encodedUrl);

            // Replace Search Engine
            encodedUrl = encodedUrl.Replace("https://www.bing.com/search?q=", DefineEngine(Configuration.Search));

#if DEBUG
            var uriMessageUi = new MessageUi("GoAwayEdge",
                "New Uri: " + encodedUrl, "OK", null, true);
            uriMessageUi.ShowDialog();
#endif
            var uri = new Uri(encodedUrl);
            return uri.ToString();
        }

        private static string DefineEngine(SearchEngine engine)
        {
            var customQueryUrl = string.Empty;
            try
            {
                customQueryUrl = RegistryConfig.GetKey("CustomQueryUrl");
            }
            catch
            {
                // ignore; not an valid object
            }

            return engine switch
            {
                SearchEngine.Google => "https://www.google.com/search?q=",
                SearchEngine.Bing => "https://www.bing.com/search?q=",
                SearchEngine.DuckDuckGo => "https://duckduckgo.com/?q=}",
                SearchEngine.Yahoo => "https://search.yahoo.com/search?p=",
                SearchEngine.Yandex => "https://yandex.com/search/?text=",
                SearchEngine.Ecosia => "https://www.ecosia.org/search?q=",
                SearchEngine.Ask => "https://www.ask.com/web?q=",
                SearchEngine.Qwant => "https://qwant.com/?q=",
                SearchEngine.Perplexity => "https://www.perplexity.ai/search?copilot=false&q=",
                SearchEngine.Custom => customQueryUrl,
                _ => "https://www.google.com/search?q="
            };
        }
        
        private static string DecodeUrlString(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;
            return newUrl;
        }

        private static string DotSlash(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;

            try // Decode base64 string from url
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query).Get("url");
                if (query != null)
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(query));
                    return decoded;
                }
            }
            catch
            {
                // ignored
            }

            return url;
        }
        
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
