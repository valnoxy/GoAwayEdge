using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        public CopilotDock(ShellManager shellManager, AppBarScreen screen, AppBarEdge edge, double desiredHeight, AppBarMode mode)
            : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen, edge, mode, desiredHeight)
        {
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MinHeight = SystemParameters.WorkArea.Height;
            Configuration.AppBarIsAttached = mode != AppBarMode.None;

            InitializeComponent();
            _ = InitializeWebViewAsync();
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
                    case Custom:
                        if (Configuration.CustomProviderUrl != null)
                            WebView.Source = new Uri(Configuration.CustomProviderUrl);
                        break;
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
            this.Close();
        }

        private void DockButton_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration.ShellManager.AppBarManager.RegisterBar(this, Width, Height, AppBarEdge.Right);

            if (Configuration.AppBarIsAttached)
            {
                DockButton.Icon = new SymbolIcon(SymbolRegular.Pin28);
                RegistryConfig.SetKey("CopilotDockState", "Detached", userSetting: true);
            }
            else
            {
                DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
            }
            Configuration.AppBarIsAttached = !Configuration.AppBarIsAttached;
        }

        private void CopilotDock_OnDeactivated(object? sender, EventArgs e)
        {
            if (Configuration.AppBarIsAttached) return;
            var currentProcess = Process.GetCurrentProcess();
            var currentTitle = currentProcess.MainWindowTitle;
            var currentId = currentProcess.Id;
            Logging.Log($"Deactivated CopilotDock (ID: {currentId}, Title: {currentTitle})", Logging.LogLevel.INFO);
            Hide();
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;
    }
}
