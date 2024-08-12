using System.Windows;

namespace GoAwayEdge.UserInterface.ControlPanel.Pages
{
    /// <summary>
    /// Interaktionslogik für HomeScreen.xaml
    /// </summary>
    public partial class HomeScreen
    {
        public HomeScreen()
        {
            InitializeComponent();
        }

        private void EdgeSettings_OnClick(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as ControlPanel)?.RootNavigation.Navigate(typeof(EdgeSettings));
        }

        private void CopilotSetting_OnClick(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as ControlPanel)?.RootNavigation.Navigate(typeof(CopilotSettings));
        }
    }
}
