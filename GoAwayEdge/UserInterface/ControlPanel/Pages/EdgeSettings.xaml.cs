using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;

namespace GoAwayEdge.UserInterface.ControlPanel.Pages
{
    /// <summary>
    /// Interaktionslogik für EdgeSettings.xaml
    /// </summary>
    public partial class EdgeSettings
    {
        private bool _appIsEnabled;

        public EdgeSettings()
        {
            InitializeComponent();

            foreach (var edgeChannels in Configuration.GetEdgeChannels())
            {
                EdgeChannelBox.Items.Add(edgeChannels);
            }
            EdgeChannelBox.SelectedItem = Configuration.Channel.ToString();

            foreach (var searchEngine in Configuration.GetSearchEngines())
            {
                SearchEngineBox.Items.Add(searchEngine);
            }

            SearchEngineBox.SelectedItem = Configuration.Search.ToString();
            if (Configuration.CustomQueryUrl != null) QueryUrlTextBox.Text = Configuration.CustomQueryUrl;

            if (RegistryConfig.GetKey("Enabled") == "True")
            {
                _appIsEnabled = true;
                PowerToggle.Content = "Disable GoAwayEdge";
            }
            else
            {
                _appIsEnabled = false;
                PowerToggle.Content = "Enable GoAwayEdge";
            }
        }

        private void SearchEngineBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchEngineBox.SelectedIndex)
            {
                case 0:
                    Configuration.Search = SearchEngine.Google;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.Search = SearchEngine.Bing;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.Search = SearchEngine.DuckDuckGo;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.Search = SearchEngine.Yahoo;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.Search = SearchEngine.Yandex;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.Search = SearchEngine.Ecosia;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 6:
                    Configuration.Search = SearchEngine.Ask;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 7:
                    Configuration.Search = SearchEngine.Qwant;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 8:
                    Configuration.Search = SearchEngine.Perplexity;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 9:
                    Configuration.Search = SearchEngine.Custom;
                    CustomSearchPanel.Visibility = System.Windows.Visibility.Visible;
                    if (string.IsNullOrEmpty(Configuration.CustomQueryUrl))
                        Debug.WriteLine("placeholder");
                    break;
            }
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

        private void PowerToggle_OnClick(object sender, RoutedEventArgs e)
        {
            if (_appIsEnabled)
            {
                try
                {
                    RegistryConfig.SetKey("Enabled", "False");
                    _appIsEnabled = false;
                    PowerToggle.Content = LocalizationManager.LocalizeValue("ControlPanelEdgePowerEnable");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("MessageGoAwayEdgeDisabled");
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedSetAppDisabled", ex.Message);
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                }
            }
            else
            {
                try
                {
                    RegistryConfig.SetKey("Enabled", "True");
                    _appIsEnabled = true;
                    PowerToggle.Content = LocalizationManager.LocalizeValue("ControlPanelEdgePowerDisable");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("MessageGoAwayEdgeEnabled");
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var errorMessage = LocalizationManager.LocalizeValue("FailedSetAppEnabled", ex.Message);
                        var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                }
            }
        }
    }
}
