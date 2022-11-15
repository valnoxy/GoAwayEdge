using System.Windows;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für Installer.xaml
    /// </summary>
    public partial class Installer : Wpf.Ui.Controls.UiWindow
    {
        internal static Installer? ContentWindow;
        private static Pages.License _licensePage;
        private static Pages.Settings _settingPage;
        
        public Installer()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.Watcher.Watch(
                    this,
                    Wpf.Ui.Appearance.BackgroundType.Mica,
                    true
                );
            };

            _licensePage = new Pages.License();
            FrameWindow.Content = _licensePage;
            ContentWindow = this;
        }

        internal void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case Pages.Settings:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = new Pages.Installation();
                    break;
                case Pages.License:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = true;
                    _settingPage = new Pages.Settings();
                    FrameWindow.Content = _settingPage;
                    break;
                default:
                    break;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case Pages.Settings:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = _licensePage;
                    break;
                default:
                    break;
            }
        }
    }
}
