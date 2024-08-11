using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;
using Wpf.Ui.Controls;

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

            if (Configuration.Search == SearchEngine.Custom)
            {
                SearchEngineBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomSearchPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomQueryUrl != null) QueryUrlTextBox.Text = Configuration.CustomQueryUrl;
                CustomUrlStatus.Symbol = Uri.TryCreate(QueryUrlTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }
            else
            {
                SearchEngineBox.SelectedItem = Configuration.Search.ToString();
            }

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

        private void FlushSettings()
        {
            try
            {
                RegistryConfig.SetKey("EdgeChannel", Configuration.Channel.ToString());
                RegistryConfig.SetKey("SearchEngine", Configuration.Search);
                if (Configuration.Search == SearchEngine.Custom)
                {
                    if (Configuration.CustomQueryUrl != null)
                        RegistryConfig.SetKey("CustomQueryUrl", Configuration.CustomQueryUrl);
                }
                else
                {
                    RegistryConfig.RemoveKey("CustomQueryUrl");
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var errorMessage = LocalizationManager.LocalizeValue("FailedFlushSetting", ex.Message);
                    var messageUi = new MessageUi("GoAwayEdge", errorMessage, "OK");
                    messageUi.ShowDialog();
                });
            }
        }

        private void SearchEngineBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchEngineBox.SelectedIndex)
            {
                case 0:
                    Configuration.Search = SearchEngine.Google;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 1:
                    Configuration.Search = SearchEngine.Bing;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 2:
                    Configuration.Search = SearchEngine.DuckDuckGo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 3:
                    Configuration.Search = SearchEngine.Yahoo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 4:
                    Configuration.Search = SearchEngine.Yandex;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 5:
                    Configuration.Search = SearchEngine.Ecosia;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 6:
                    Configuration.Search = SearchEngine.Ask;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 7:
                    Configuration.Search = SearchEngine.Qwant;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 8:
                    Configuration.Search = SearchEngine.Perplexity;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 9:
                    Configuration.Search = SearchEngine.Custom;
                    CustomSearchPanel.Visibility = Visibility.Visible;
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
            FlushSettings();
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

        private void QueryUrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryUrlTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomQueryUrl = QueryUrlTextBox.Text;
                FlushSettings();
            }
            else
            {
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }
    }
}
