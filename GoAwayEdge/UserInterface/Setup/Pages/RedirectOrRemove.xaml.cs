using System.Windows;
using GoAwayEdge.Common;
using GoAwayEdge.Common.Debugging;
using Wpf.Ui;
using Wpf.Ui.Controls;
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
            Configuration.UninstallEdge = false;
            Installer.SettingPage = new Settings();
            Installer.ContentWindow!.FrameWindow.Content = Installer.SettingPage;
            Installer.ContentWindow!.NextBtn.IsEnabled = true;
        }

        private async void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(Installer.ContentWindow!.RootContentDialogPresenter);
            var warningTitle = LocalizationManager.LocalizeValue("Warning");
            var cancelBtn = LocalizationManager.LocalizeValue("Cancel");
            var removeBtn = LocalizationManager.LocalizeValue("SettingsUninstallEdgeBtn");
            var contentValue = LocalizationManager.LocalizeValue("SettingsUninstallEdgeWarningDescription");


            var result = await contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions
                {
                    Title = warningTitle,
                    Content = contentValue,
                    PrimaryButtonText = removeBtn,
                    CloseButtonText = cancelBtn
                }
            );
            if (result != ContentDialogResult.Primary) return;
            
            Logging.Log("User pressed 'Remove Microsoft Edge'");
            Configuration.UninstallEdge = true;
            Installer.SettingPage = new Settings();
            Installer.ContentWindow!.FrameWindow.Content = Installer.SettingPage;
        }
    }
}
