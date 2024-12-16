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

            // Edge Channel
            foreach (var edgeChannels in Configuration.GetEdgeChannels())
            {
                EdgeChannelBox.Items.Add(edgeChannels);
            }
            EdgeChannelBox.SelectedItem = Configuration.Channel.ToString();

            // Search Engine
            foreach (var searchEngine in Configuration.GetSearchEngines())
            {
                SearchEngineBox.Items.Add(searchEngine);
            }
            SearchEngineBox.SelectedItem = Configuration.Search.ToString().Replace("_", " ");

            if (Configuration.Search == SearchEngine.Custom)
            {
                SearchEngineBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomSearchPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomQueryUrl != null) QueryUrlTextBox.Text = Configuration.CustomQueryUrl;
                CustomUrlStatus.Symbol = Uri.TryCreate(QueryUrlTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }

            // AI Provider
            foreach (var aiProvider in Configuration.GetAiProviders())
            {
                CopilotProviderBox.Items.Add(aiProvider);
            }
            CopilotProviderBox.SelectedItem = Configuration.AiProvider.ToString().Replace("_", " ");

            if (Configuration.AiProvider == AiProvider.Custom)
            {
                CopilotProviderBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomAiPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomAiProviderUrl != null) QueryAiProviderTextBox.Text = Configuration.CustomAiProviderUrl;
                CustomUrlStatus.Symbol = Uri.TryCreate(QueryAiProviderTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }

            // Weather Provider
            foreach (var weatherProvider in Configuration.GetWeatherProviders())
            {
                WeatherProviderBox.Items.Add(weatherProvider);
            }
            WeatherProviderBox.SelectedItem = Configuration.WeatherProvider.ToString().Replace("_", " ");

            if (Configuration.WeatherProvider == WeatherProvider.Custom)
            {
                WeatherProviderBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomWeatherPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomWeatherProviderUrl != null) QueryWeatherProviderTextBox.Text = Configuration.CustomWeatherProviderUrl;
                CustomWeatherUrlStatus.Symbol = Uri.TryCreate(QueryWeatherProviderTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }

            if (Configuration.WeatherProvider == WeatherProvider.Default)
                WeatherProviderBox.SelectedItem = LocalizationManager.LocalizeValue("Default");

            // Others
            if (Configuration.NoEdgeInstalled)
            {
                CopilotStackPanel.IsEnabled = false;
                WeatherStackPanel.IsEnabled = false;
                EdgeStackPanel.IsEnabled = false;
            }

            Configuration.Uninstall = false;
            Configuration.InstallControlPanel = true;
            ControlPanelSwitch.IsChecked = true;
        }

        private void EnableNextBtnValidation()
        {
            var validation = true;

            if (Configuration.Search == SearchEngine.Custom)
                validation = !string.IsNullOrEmpty(Configuration.CustomQueryUrl) && CustomUrlStatus.Symbol == SymbolRegular.CheckmarkCircle24;
            if (Configuration.AiProvider == AiProvider.Custom)
                validation = !string.IsNullOrEmpty(Configuration.CustomAiProviderUrl) && CustomAiUrlStatus.Symbol == SymbolRegular.CheckmarkCircle24;
            if (Configuration.WeatherProvider == WeatherProvider.Custom)
                validation = !string.IsNullOrEmpty(Configuration.CustomWeatherProviderUrl) && CustomWeatherUrlStatus.Symbol == SymbolRegular.CheckmarkCircle24;

            Installer.ContentWindow!.NextBtn.IsEnabled = validation;
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
                    Configuration.Search = SearchEngine.Perplexity;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 9:
                    Configuration.Search = SearchEngine.Custom;
                    CustomSearchPanel.Visibility = Visibility.Visible;
                    break;
            }
            EnableNextBtnValidation();
        }

        private void QueryUrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Uri.TryCreate(QueryUrlTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomQueryUrl = QueryUrlTextBox.Text;
            }
            else
            {
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
            EnableNextBtnValidation();
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
                    Configuration.AiProvider = AiProvider.Copilot;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.AiProvider = AiProvider.ChatGPT;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.AiProvider = AiProvider.Gemini;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.AiProvider = AiProvider.GitHub_Copilot;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.AiProvider = AiProvider.Grok;
                    CustomAiPanel.Visibility = Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.AiProvider = AiProvider.Custom;
                    CustomAiPanel.Visibility = Visibility.Visible;
                    break;
            }
            EnableNextBtnValidation();
        }

        private void WeatherProviderBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (WeatherProviderBox.SelectedIndex)
            {
                case 0:
                    Configuration.WeatherProvider = WeatherProvider.Default;
                    CustomWeatherPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.WeatherProvider = WeatherProvider.WeatherCom;
                    CustomWeatherPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.WeatherProvider = WeatherProvider.AccuWeather;
                    CustomWeatherPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.WeatherProvider = WeatherProvider.Custom;
                    CustomWeatherPanel.Visibility = Visibility.Visible;
                    break;
            }
            EnableNextBtnValidation();
        }

        private void QueryAiProviderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryAiProviderTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomAiUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomAiProviderUrl = QueryAiProviderTextBox.Text;
            }
            else
            {
                CustomAiUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
            EnableNextBtnValidation();
        }

        private void QueryWeatherProviderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryWeatherProviderTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomWeatherUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomWeatherProviderUrl = QueryWeatherProviderTextBox.Text;
            }
            else
            {
                CustomWeatherUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
            EnableNextBtnValidation();
        }
    }
}
