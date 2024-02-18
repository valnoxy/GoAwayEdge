using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GoAwayEdge.Common;
using GoAwayEdge.Pages;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für Installer.xaml
    /// </summary>
    public partial class Installer
    {
        internal static Installer? ContentWindow;
        public static License? LicensePage;
        private static Welcome? _welcomePage;
        private static Settings? _settingPage;

        public Installer()
        {
            InitializeComponent();

            VersionLbl.Content = $"Version {Assembly.GetExecutingAssembly().GetName().Version!}";
            Configuration.InitialEnvironment();

            _welcomePage = new Welcome();
            LicensePage = new License();
            FrameWindow.Content = _welcomePage;
            ContentWindow = this;
        }

        internal void NextBtn_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case InstallationSuccess:
                    Environment.Exit(0);
                    break;
                case Settings:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = new Installation();
                    break;
                case License:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = true;
                    _settingPage = new Settings();
                    FrameWindow.Content = _settingPage;
                    break;
            }
        }

        private void BackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case Settings:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = true;
                    FrameWindow.Content = LicensePage;
                    break;
                case License:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = _welcomePage;
                    break;
            }
        }
    }
}
