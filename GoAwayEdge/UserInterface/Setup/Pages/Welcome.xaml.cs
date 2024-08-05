using System.IO;
using System.Windows;
using GoAwayEdge.Common;

namespace GoAwayEdge.UserInterface.Setup.Pages
{
    /// <summary>
    /// Interaktionslogik für Welcome.xaml
    /// </summary>
    public partial class Welcome
    {
        public Welcome()
        {
            InitializeComponent();

            if (!Path.Exists(Configuration.InstallDir))
                UninstallBtn.IsEnabled = false;
            if (Path.GetDirectoryName(Environment.ProcessPath) != Configuration.InstallDir) return;
            UninstallBtn.IsEnabled = false;
            Dispatcher.Invoke(() =>
            {
                var resourceValue = (string)Application.Current.MainWindow!.FindResource("SettingsUninstallUseInstaller");
                EdgeUninstallNote.Text = !string.IsNullOrEmpty(resourceValue) ? resourceValue : "Please use the Installer in order to uninstall GoAwayEdge.";
            });
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            Installer.ContentWindow!.FrameWindow.Content = Installer.LicensePage;
            Installer.ContentWindow!.BackBtn.IsEnabled = true;
        }

        private void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            Configuration.Uninstall = true;
            Installer.ContentWindow!.FrameWindow.Content = new Installation();
        }
    }
}
