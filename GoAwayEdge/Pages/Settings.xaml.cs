using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;
using Path = System.IO.Path;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings()
        {
            InitializeComponent();

            EdgeChannelBox.Items.Add("Edge Stable");
            EdgeChannelBox.Items.Add("Edge Beta");
            EdgeChannelBox.Items.Add("Edge Dev");
            EdgeChannelBox.Items.Add("Edge Canary");
            EdgeChannelBox.SelectedIndex = 0;
            Configuration.Channel = EdgeChannel.Stable;

            SearchEngineBox.Items.Add("Google");
            SearchEngineBox.Items.Add("Bing");
            SearchEngineBox.Items.Add("DuckDuckGo");
            SearchEngineBox.Items.Add("Yahoo");
            SearchEngineBox.Items.Add("Yandex");
            SearchEngineBox.Items.Add("Ecosia");
            SearchEngineBox.Items.Add("Ask");
            SearchEngineBox.Items.Add("Qwant");

            try
            {
                Dispatcher.Invoke(() =>
                {
                    var resourceValue = (string)Application.Current.MainWindow!.FindResource("SettingsSearchEngineCustomItem");
                    SearchEngineBox.Items.Add(!string.IsNullOrEmpty(resourceValue) ? resourceValue : "Custom");
                });
            }
            catch
            {
                SearchEngineBox.Items.Add("Custom");
            }

            SearchEngineBox.SelectedIndex = 0;
            Configuration.Search = SearchEngine.Google;

            Configuration.Uninstall = false;

            var instDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");
            
            if (Path.GetDirectoryName(Environment.ProcessPath) != instDir) return;
            UninstallSwitch.IsEnabled = false;
            Dispatcher.Invoke(() =>
            {
                var resourceValue = (string)Application.Current.MainWindow.FindResource("SettingsUninstallUseInstaller");
                EdgeUninstallNote.Text = !string.IsNullOrEmpty(resourceValue) ? resourceValue : "Please use the Installer in order to uninstall GoAwayEdge.";
            });
        }

        private void EdgeChannelBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Configuration.Channel = EdgeChannelBox.SelectedIndex switch
            {
                0 => EdgeChannel.Stable,
                1 => EdgeChannel.Beta,
                2 => EdgeChannel.Dev,
                3 => EdgeChannel.Canary,
                _ => EdgeChannel.Stable
            };
        }

        private void SearchEngineBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchEngineBox.SelectedIndex)
            {
                case 0:
                    Configuration.Search = SearchEngine.Google;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.Search = SearchEngine.Bing;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.Search = SearchEngine.DuckDuckGo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.Search = SearchEngine.Yahoo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.Search = SearchEngine.Yandex;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.Search = SearchEngine.Ecosia;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 6:
                    Configuration.Search = SearchEngine.Ask;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 7:
                    Configuration.Search = SearchEngine.Qwant;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 8:
                    Configuration.Search = SearchEngine.Custom;
                    CustomSearchPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UninstallSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration.Uninstall = UninstallSwitch.IsChecked!.Value;
            if (UninstallSwitch.IsChecked.Value)
            {
                SearchEngineBox.IsEnabled = false;
                EdgeChannelBox.IsEnabled = false;
            }
            else 
            {
                SearchEngineBox.IsEnabled = true;
                EdgeChannelBox.IsEnabled = true;
            }
        }

        private void QueryUrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Configuration.CustomQueryUrl = QueryUrlTextBox.Text;
        }
    }
}
