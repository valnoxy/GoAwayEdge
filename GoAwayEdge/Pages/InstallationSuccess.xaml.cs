using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class InstallationSuccess : UserControl
    {
        public InstallationSuccess()
        {
            InitializeComponent();

            if (Configuration.Uninstall)
            {
                Dispatcher.Invoke(() =>
                {
                    var titleResource = (string)Application.Current.MainWindow!.FindResource("UninstallFinishedTitle");
                    var descriptionResource = (string)Application.Current.MainWindow!.FindResource("UninstallFinishedDescription");
                    SetupTitle.Content = !string.IsNullOrEmpty(titleResource) 
                        ? titleResource : "Uninstallation completed!";
                    SetupDescription.Content = !string.IsNullOrEmpty(descriptionResource) 
                        ? descriptionResource : "GoAwayEdge has been successfully removed from the system.";
                });
            }
        }
    }
}
