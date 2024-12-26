using System.Windows;
using GoAwayEdge.Common;
using Wpf.Ui.Controls;

namespace GoAwayEdge.UserInterface.Setup.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class InstallationSuccess
    {
        public InstallationSuccess()
        {
            InitializeComponent();

            var exitResource = (string)Application.Current.MainWindow!.FindResource("Exit");
            Installer.ContentWindow!.NextBtn.IsEnabled = true;
            Installer.ContentWindow!.NextBtn.Icon = new SymbolIcon(SymbolRegular.ArrowExit20);
            Installer.ContentWindow!.NextBtn.Content = !string.IsNullOrEmpty(exitResource)
                ? exitResource : "Exit";

            var setupDescriptionResource = Configuration.InstallControlPanel ?
                (string)Application.Current.MainWindow!.FindResource("SetupFinishedDescriptionWithControlPanel") : (string)Application.Current.MainWindow!.FindResource("SetupFinishedDescription");
            SetupDescription.Text = setupDescriptionResource;

            if (Configuration.Uninstall)
            {
                Dispatcher.Invoke(() =>
                {
                    var titleResource = (string)Application.Current.MainWindow!.FindResource("UninstallFinishedTitle");
                    var descriptionResource = (string)Application.Current.MainWindow!.FindResource("UninstallFinishedDescription");
                    SetupTitle.Text = !string.IsNullOrEmpty(titleResource) 
                        ? titleResource : "Uninstallation completed!";
                    SetupDescription.Text = !string.IsNullOrEmpty(descriptionResource) 
                        ? descriptionResource : "GoAwayEdge has been successfully removed from the system.";
                });
            }
        }
    }
}
