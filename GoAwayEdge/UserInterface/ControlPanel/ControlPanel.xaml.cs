using GoAwayEdge.UserInterface.ControlPanel.Pages;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace GoAwayEdge.UserInterface.ControlPanel
{
    /// <summary>
    /// Interaktionslogik für ControlPanel.xaml
    /// </summary>
    public partial class ControlPanel
    {
        private readonly DispatcherTimer _timer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        public static ControlPanel? ContentWindow;

        public ControlPanel()
        {
            InitializeComponent();

#if DEBUG
            DebugString.Visibility = Visibility.Visible;
#else
            DebugString.Visibility = Visibility.Collapsed;
#endif

            // Get version
            var assembly = Assembly.GetExecutingAssembly();
            var appDirectory = AppContext.BaseDirectory;
            var assemblyPath = Path.Combine(appDirectory, $"{assembly.GetName().Name}.exe");
            var fvi = FileVersionInfo.GetVersionInfo(assemblyPath);
            var version = fvi.FileVersion;
            Version.Text = $"Version {version}";

            // Get User
            var accountToken = WindowsIdentity.GetCurrent().Token;
            var windowsIdentity = new WindowsIdentity(accountToken);
            UserName.Text = windowsIdentity.Name;
            
            // Time & Date
            _timer.Tick += UpdateTimeAndDate_Tick!;
            _timer.Start();

            // Configuration.InitialEnvironment();
        }

        private void ControlCenter_OnLoaded(object? sender, EventArgs eventArgs)
        {
            RootNavigation.Navigate(typeof(Pages.HomeScreen));
        }

        private void UpdateTimeAndDate_Tick(object sender, EventArgs e)
        {
            Time.Text = DateTime.Now.ToShortTimeString();
            Date.Text = DateTime.Now.ToShortDateString();
        }

        private void ThemeSwitch_Click(object sender, RoutedEventArgs e)
        {
            WindowBackdropType = WindowBackdropType == WindowBackdropType.Mica ? WindowBackdropType.Tabbed : WindowBackdropType.Mica;
        }
    }
}
