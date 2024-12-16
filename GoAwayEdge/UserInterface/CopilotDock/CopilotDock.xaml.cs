using System.Diagnostics;
using System.IO;
using System.Windows;
using GoAwayEdge.Common;
using GoAwayEdge.Common.Debugging;
using ManagedShell;
using ManagedShell.AppBar;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;
using static GoAwayEdge.Common.AiProvider;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    /// <summary>
    /// Interaktionslogik für CopilotDock.xaml
    /// </summary>
    public partial class CopilotDock
    {
        public static CopilotDock? Instance;
        public CopilotDock(ShellManager shellManager, AppBarScreen screen, AppBarEdge edge, double desiredHeight, AppBarMode mode)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, mode, desiredHeight)
        {
            MaxHeight = SystemParameters.WorkArea.Height;
            MinHeight = SystemParameters.WorkArea.Height;
            Configuration.AppBarIsAttached = mode != AppBarMode.None;
            Instance = this;

            InitializeComponent();
            _ = InitializeWebViewAsync();

            DockButton.Icon = Configuration.AppBarIsAttached ? new SymbolIcon(SymbolRegular.PinOff28) : new SymbolIcon(SymbolRegular.Pin28);
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                var userProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "valnoxy", "GoAwayEdge");
                Directory.CreateDirectory(userProfilePath);
                
                var webView2Environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userProfilePath);
                await WebView.EnsureCoreWebView2Async(webView2Environment);

                switch (Configuration.Provider)
                {
                    case ChatGPT:
                        WebView.Source = new Uri("https://chatgpt.com/");
                        break;
                    case Gemini:
                        WebView.Source = new Uri("https://gemini.google.com/");
                        break;
                    case GitHub_Copilot:
                        WebView.Source = new Uri("https://github.com/copilot");
                        break;
                    case Grok:
                        WebView.Source = new Uri("https://x.com/i/grok");
                        break;
                    case Custom:
                        if (Configuration.CustomProviderUrl != null)
                            WebView.Source = new Uri(Configuration.CustomProviderUrl);
                        break;
                    case Copilot:
                    default:
                        Logging.Log($"Failed to load Provider! Provider Value '{Configuration.Provider}' in invalid!");
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to load WebView2 (Copilot replacement): {ex.Message}", Logging.LogLevel.ERROR);
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration.ShellManager.AppBarManager.SignalGracefulShutdown();
            Configuration.ShellManager.Dispose();
            Environment.Exit(0);
        }

        private void DockButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Configuration.AppBarIsAttached)
            {
                DockButton.Icon = new SymbolIcon(SymbolRegular.Pin28);
                RegistryConfig.SetKey("CopilotDockState", "Detached", userSetting: true);
                this.AppBarMode = AppBarMode.None;
            }
            else
            {
                DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
                this.AppBarMode = AppBarMode.Normal;
            }
            Configuration.AppBarIsAttached = !Configuration.AppBarIsAttached;
        }

        private void CopilotDock_OnDeactivated(object? sender, EventArgs e)
        {
            if (Configuration.AppBarIsAttached) return;
            var currentProcess = Process.GetCurrentProcess();
            var currentId = currentProcess.Id;
            Logging.Log($"Deactivated CopilotDock (PID: {currentId})");
            WindowManager.HideCopilotDock();
        }
    }
}
