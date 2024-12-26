using System.Reflection;
using System.Windows;
using GoAwayEdge.Common;
using GoAwayEdge.UserInterface.Setup.Pages;

namespace GoAwayEdge.UserInterface.Setup
{
    /// <summary>
    /// Interaktionslogik für Installer.xaml
    /// </summary>
    public partial class Installer
    {
        internal static Installer? ContentWindow;
        public static License? LicensePage;
        public static Settings? SettingPage;
        private static Welcome? _welcomePage;
        private static RedirectOrRemove? _redirectOrRemovePage;

        public Installer()
        {
            InitializeComponent();

            string versionText;
            var assembly = Assembly.GetExecutingAssembly();
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            try
            {
                var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(informationVersion) && version.ToString() != informationVersion)
                {
                    versionText = $"Version {version}-{informationVersion}";
                }
                else
                {
                    versionText = $"Version {version}";
                }
            }
            catch
            {
                versionText = $"Version {version}";
            }

            VersionLbl.Content = versionText;
            Configuration.InitialEnvironment(setupRunning: true);

            _welcomePage = new Welcome();
            _redirectOrRemovePage = new RedirectOrRemove();
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
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = true;
                    FrameWindow.Content = _redirectOrRemovePage;
                    break;
            }
        }

        private void BackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case RedirectOrRemove:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = true;
                    LicensePage = new License();
                    FrameWindow.Content = LicensePage;
                    break;
                case Settings:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = true;
                    FrameWindow.Content = _redirectOrRemovePage;
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
