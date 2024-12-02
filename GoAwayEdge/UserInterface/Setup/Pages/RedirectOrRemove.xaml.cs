using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace GoAwayEdge.UserInterface.Setup.Pages
{
    /// <summary>
    /// Interaktionslogik für RedirectOrRemove.xaml
    /// </summary>
    public partial class RedirectOrRemove
    {
        public RedirectOrRemove()
        {
            InitializeComponent();
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            Installer.ContentWindow!.FrameWindow.Content = Installer.SettingPage;
            Installer.ContentWindow!.NextBtn.IsEnabled = true;
        }

        private void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(Installer.ContentWindow!.RootContentDialogPresenter);

            contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "Warning",
                    Content = "Removing Microsoft Edge can cause serious system issues, as it’s deeply integrated into Windows and essential for many features, including updates, help files, and some apps. Deleting it could result in instability or broken functionality.\n\nOnly proceed if you fully understand the risks and have a reliable backup or restore point in place.",
                    PrimaryButtonText = "Remove Microsoft Edge",
                    CloseButtonText = "Cancel"
                }
            );
        }
    }
}
