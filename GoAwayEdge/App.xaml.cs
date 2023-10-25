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
using Microsoft.Win32;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für App.xaml
    /// </summary>
    public partial class App
    {
        private static string? _url;
        private static SearchEngine _engine = SearchEngine.Google; // Fallback

        public void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0) // Opens Installer
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
                    Application.Current.Shutdown();
                    return;
                }
                
                var installer = new Installer();
                installer.ShowDialog();
                Environment.Exit(0);
            }
            
            string[] args = e.Args;
            Output.WriteLine("Please go away Edge!");
            Output.WriteLine("Hooked into process via IFEO successfully.");
            var argumentJoin = string.Join(",", args);

            Configuration.InitialEnvironment();

#if DEBUG
            var w = new MessageUi("GoAwayEdge",
                $"The following args are redirected (CTRL+C to copy):\n\n{argumentJoin}", "OK", null, true);
            w.ShowDialog();
#endif
            Output.WriteLine("Command line args:\n\n" + argumentJoin + "\n", ConsoleColor.Gray);

            // Filter command line args
            foreach (var arg in args)
            {
                if (arg.Contains("-ToastActivated"))
                {
                    // Clicked on Toast notification, ignore it.
                    Environment.Exit(0);
                }
                if (arg.Contains("--update"))
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

                    // Validate IFEO bínary
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
                                    Application.Current.Shutdown();
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
                                    Application.Current.Shutdown();
                                    return;
                                }
                                Environment.Exit(0);
                            }
                            Updater.ModifyIfeoBinary(ModifyAction.Create);
                            break;
                    }
                    Environment.Exit(0);
                }
                if (arg.Contains("microsoft-edge:"))
                {
                    _url = arg;
                }
                if (arg.Contains("-se"))
                {
                    var argParsed = arg.Remove(0,3);
                    _engine = argParsed switch
                    {
                        "Google" => SearchEngine.Google,
                        "Bing" => SearchEngine.Bing,
                        "DuckDuckGo" => SearchEngine.DuckDuckGo,
                        "Yahoo" => SearchEngine.Yahoo,
                        "Yandex" => SearchEngine.Yandex,
                        "Ecosia" => SearchEngine.Ecosia,
                        "Ask" => SearchEngine.Ask,
                        "Qwant" => SearchEngine.Qwant,
                        "Custom" => SearchEngine.Custom,
                        _ => SearchEngine.Google // Fallback search engine
                    };
                }
                if (!args.Contains("--profile-directory") && !ContainsParsedData(args) && args.Length != 2) continue; // Start Edge (default browser on this system)

#if DEBUG
                var messageUi = new MessageUi("GoAwayEdge",
                    "Microsoft Edge will now start normally via IFEO application.", "OK", null, true);
                messageUi.ShowDialog();
#endif

                var parsedArgs = args.Skip(2);
                var p = new Process();
                p.StartInfo.FileName = FileConfiguration.IfeoPath;
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
                    p.StartInfo.FileName = FileConfiguration.IfeoPath;
                    p.StartInfo.Arguments = "microsoft-edge://?ux=copilot&tcp=1&source=taskbar";
                    Output.WriteLine("Opening Windows Copilot", ConsoleColor.Gray);
#if DEBUG
                    var copilotMessageUi = new MessageUi("GoAwayEdge",
                        "Opening Windows Copilot ...", "OK", null, true);
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
            
            Environment.Exit(0);
        }

        private static bool ContainsParsedData(IEnumerable<string> args)
        {
            var contains = false;
            var engineUrl = DefineEngine(_engine);

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
            encodedUrl = encodedUrl.Replace("https://www.bing.com/search?q=", DefineEngine(_engine));

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
            string customQueryUrl = null!;
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0", false);
                customQueryUrl = (string)key?.GetValue("CustomQueryUrl")!;
            }
            catch
            {
                // ignored
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
