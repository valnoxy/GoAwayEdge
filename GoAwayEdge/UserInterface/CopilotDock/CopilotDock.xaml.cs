using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using GoAwayEdge.Common;
using GoAwayEdge.Common.Debugging;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    /// <summary>
    /// Interaktionslogik für CopilotDock.xaml
    /// </summary>
    public partial class CopilotDock
    {
        private static bool _isDocked;
        public CopilotDock()
        {
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
                    case AiProvider.ChatGPT:
                        WebView.Source = new Uri("https://chatgpt.com/");
                        break;
                    case AiProvider.Custom:
                        if (Configuration.CustomProviderUrl != null)
                            WebView.Source = new Uri(Configuration.CustomProviderUrl);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Window settings
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                Visibility = Visibility.Visible;
                Width = screenWidth * 0.3;
                Height = screenHeight;
                MinWidth = 200;
                ResizeMode = ResizeMode.CanResizeWithGrip;

                // Set window position
                Left = screenWidth - Width;
                Top = 0;

                // Last state
                try
                {
                    var lastState = RegistryConfig.GetKey("CopilotDockState", userSetting: true);
                    if (string.IsNullOrEmpty(lastState))
                    {
                        DockWindowToRight();
                        _isDocked = true;
                        DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                        RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
                    }
                    else if (lastState == "Docked")
                    {
                        DockWindowToRight();
                        _isDocked = true;
                        DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                    }
                    else
                    {
                        _isDocked = false;
                        DockButton.Icon = new SymbolIcon(SymbolRegular.Pin28);
                    }
                }
                catch
                {
                    DockWindowToRight();
                    _isDocked = true;
                    DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                    RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to load WebView2 (Copilot replacement): {ex.Message}", Logging.LogLevel.ERROR);
            }
        }

        // Testing
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #region Docking

        private void DockWindowToRight()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            Visibility = Visibility.Visible;
            Width = screenWidth * 0.3;
            Height = screenHeight;
            MinWidth = 200;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            // Set window position
            Left = screenWidth - Width;
            Top = 0;

            // Register App Bar
            RegisterAppBar();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_SETPOS = 0x00000003;
        private const int ABE_RIGHT = 2;

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        private void RegisterAppBar()
        {
            APPBARDATA appBarData = new APPBARDATA();
            appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);
            appBarData.hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            appBarData.uEdge = ABE_RIGHT;
            appBarData.rc.left = (int)(SystemParameters.PrimaryScreenWidth - this.Width);
            appBarData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
            appBarData.rc.top = 0;
            appBarData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;

            SHAppBarMessage(ABM_NEW, ref appBarData);
            SHAppBarMessage(ABM_SETPOS, ref appBarData);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            AdjustWindowPosition();
        }

        private void AdjustWindowPosition()
        {
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDocked) RegisterAppBar();
        }

        private void UnregisterAppBar()
        {
            APPBARDATA appBarData = new APPBARDATA();
            appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);
            appBarData.hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            SHAppBarMessage(ABM_REMOVE, ref appBarData);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnregisterAppBar();
        }

        #endregion

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DockButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isDocked)
            {
                UnregisterAppBar();
                _isDocked = false;
                DockButton.Icon = new SymbolIcon(SymbolRegular.Pin28);
                RegistryConfig.SetKey("CopilotDockState", "Detached", userSetting: true);
            }
            else
            {
                DockWindowToRight();
                _isDocked = true;
                DockButton.Icon = new SymbolIcon(SymbolRegular.PinOff28);
                RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
            }
        }

        private void CopilotDock_OnDeactivated(object? sender, EventArgs e)
        {
            if (_isDocked) return;
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
