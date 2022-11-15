using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();

            EdgeChannelBox.Items.Add("Edge Stable");
            EdgeChannelBox.Items.Add("Edge Beta");
            EdgeChannelBox.Items.Add("Edge Dev");
            EdgeChannelBox.Items.Add("Edge Canary");
            EdgeChannelBox.SelectedIndex = 0;
            Common.Configuration.Channel = Common.EdgeChannel.Stable;

            SearchEngineBox.Items.Add("Google");
            SearchEngineBox.Items.Add("Bing");
            SearchEngineBox.Items.Add("DuckDuckGo");
            SearchEngineBox.Items.Add("Yahoo");
            SearchEngineBox.Items.Add("Yandex");
            SearchEngineBox.Items.Add("Ecosia");
            SearchEngineBox.Items.Add("Ask");
            SearchEngineBox.SelectedIndex = 0;
            Common.Configuration.Search = Common.SearchEngine.Google;

            Common.Configuration.Uninstall = false;

            var InstDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "valnoxy",
                "GoAwayEdge");
            if (Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) == InstDir)
            {
                UninstallSwitch.IsEnabled = false;
                EdgeUninstallNote.Text = "Please use the Installer in order to uninstall GoAwayEdge.";
            }
        }

        private void EdgeChannelBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (EdgeChannelBox.SelectedIndex)
            {
                case 0:
                    Common.Configuration.Channel = Common.EdgeChannel.Stable;
                    break;
                case 1:
                    Common.Configuration.Channel = Common.EdgeChannel.Beta;
                    break;
                case 2:
                    Common.Configuration.Channel = Common.EdgeChannel.Dev;
                    break;
                case 3:
                    Common.Configuration.Channel = Common.EdgeChannel.Canary;
                    break;
            }
        }

        private void SearchEngineBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchEngineBox.SelectedIndex)
            {
                case 0:
                    Common.Configuration.Search = Common.SearchEngine.Google;
                    break;
                case 1:
                    Common.Configuration.Search = Common.SearchEngine.Bing;
                    break;
                case 2:
                    Common.Configuration.Search = Common.SearchEngine.DuckDuckGo;
                    break;
                case 3:
                    Common.Configuration.Search = Common.SearchEngine.Yahoo;
                    break;
                case 4:
                    Common.Configuration.Search = Common.SearchEngine.Yandex;
                    break;
                case 5:
                    Common.Configuration.Search = Common.SearchEngine.Ecosia;
                    break;
                case 6:
                    Common.Configuration.Search = Common.SearchEngine.Ask;
                    break;
            }
        }

        private void UninstallSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            Common.Configuration.Uninstall = UninstallSwitch.IsChecked.Value;
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
    }
}
