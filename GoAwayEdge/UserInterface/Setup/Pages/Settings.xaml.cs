using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;
using Wpf.Ui.Controls;

namespace GoAwayEdge.UserInterface.Setup.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings()
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

            foreach (var aiProvider in Configuration.GetAiProviders())
            {
                CopilotProviderBox.Items.Add(aiProvider);
            }
            CopilotProviderBox.SelectedItem = Configuration.Provider.ToString().Replace("_", " ");

            if (Configuration.Provider == AiProvider.Custom)
            {
                CopilotProviderBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomSearchPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomProviderUrl != null) QueryProviderTextBox.Text = Configuration.CustomProviderUrl;
                CustomUrlStatus.Symbol = Uri.TryCreate(QueryProviderTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }
            else
            {
                CopilotProviderBox.SelectedItem = Configuration.Search.ToString();
            }

            if (Configuration.NoEdgeInstalled)
            {
                CopilotStackPanel.IsEnabled = false;
                EdgeStackPanel.IsEnabled = false;
            }

            Configuration.Uninstall = false;
            Configuration.InstallControlPanel = true;
            ControlPanelSwitch.IsChecked = true;
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
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 1:
                    Configuration.Search = SearchEngine.Bing;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 2:
                    Configuration.Search = SearchEngine.DuckDuckGo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 3:
                    Configuration.Search = SearchEngine.Yahoo;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 4:
                    Configuration.Search = SearchEngine.Yandex;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 5:
                    Configuration.Search = SearchEngine.Ecosia;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 6:
                    Configuration.Search = SearchEngine.Ask;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 7:
                    Configuration.Search = SearchEngine.Qwant;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 8:
                    Configuration.Search = SearchEngine.Perplexity;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    Installer.ContentWindow!.NextBtn.IsEnabled = true;
                    break;
                case 9:
                    Configuration.Search = SearchEngine.Custom;
                    CustomSearchPanel.Visibility = Visibility.Visible;
                    if (string.IsNullOrEmpty(Configuration.CustomQueryUrl))
                        Installer.ContentWindow!.NextBtn.IsEnabled = false;
                    break;
            }
        }
        
        private void QueryUrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Uri.TryCreate(QueryUrlTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                Installer.ContentWindow!.NextBtn.IsEnabled = true;
                CustomUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomQueryUrl = QueryUrlTextBox.Text;
            }
            else
            {
                Installer.ContentWindow!.NextBtn.IsEnabled = false;
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }

        private void ControlPanelSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration.InstallControlPanel = ControlPanelSwitch.IsChecked!.Value;
        }

        private void CopilotProviderBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CopilotProviderBox.SelectedIndex)
            {
                case 0:
                    Configuration.Provider = AiProvider.Copilot;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.Provider = AiProvider.ChatGPT;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.Provider = AiProvider.Gemini;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.Provider = AiProvider.GitHub_Copilot;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.Provider = AiProvider.Grok;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.Provider = AiProvider.Custom;
                    CustomAiPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void QueryProviderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryProviderTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomProviderUrl = QueryProviderTextBox.Text;
            }
            else
            {
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }
    }
}
