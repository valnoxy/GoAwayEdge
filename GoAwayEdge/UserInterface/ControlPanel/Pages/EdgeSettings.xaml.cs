﻿using System.Diagnostics;
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

            // Search Engine
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

            if (RegistryConfig.GetKey("Enabled") == "True")
            {
                _appIsEnabled = true;
                PowerToggle.Content = LocalizationManager.LocalizeValue("ControlPanelEdgePowerDisable"); ;
            }
            else
            {
                _appIsEnabled = false;
                PowerToggle.Content = LocalizationManager.LocalizeValue("ControlPanelEdgePowerEnable"); ;
            }
        }

        private static void FlushSettings()
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

                RegistryConfig.SetKey("WeatherProvider", Configuration.WeatherProvider, userSetting: true);
                if (Configuration.WeatherProvider == WeatherProvider.Custom)
                {
                    if (Configuration.CustomWeatherProviderUrl != null)
                        RegistryConfig.SetKey("CustomWeatherProviderUrl", Configuration.CustomWeatherProviderUrl, userSetting: true);
                }
                else
                {
                    RegistryConfig.RemoveKey("CustomWeatherProviderUrl", userSetting: true);
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
                    break;
            }
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
            FlushSettings();
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

        private void QueryWeatherProviderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryWeatherProviderTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomWeatherUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomWeatherProviderUrl = QueryWeatherProviderTextBox.Text;
                FlushSettings();
            }
            else
            {
                CustomWeatherUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }
    }
}
