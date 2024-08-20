using System.Windows;
using System.Windows.Controls;
using GoAwayEdge.Common;
using Wpf.Ui.Controls;

namespace GoAwayEdge.UserInterface.ControlPanel.Pages
{
    /// <summary>
    /// Interaktionslogik für CopilotSettings.xaml
    /// </summary>
    public partial class CopilotSettings
    {
        public CopilotSettings()
        {
            InitializeComponent();

            foreach (var aiProvider in Configuration.GetAiProviders())
            {
                CopilotProviderBox.Items.Add(aiProvider);
            }
            CopilotProviderBox.SelectedItem = Configuration.Provider.ToString();

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
        }

        private void FlushSettings()
        {
            try
            {
                RegistryConfig.SetKey("AiProvider", Configuration.Provider.ToString(), userSetting: true);
                if (Configuration.Provider == AiProvider.Custom)
                {
                    if (Configuration.CustomProviderUrl != null)
                        RegistryConfig.SetKey("CustomProviderUrl", Configuration.CustomProviderUrl, userSetting: true);
                }
                else
                {
                    RegistryConfig.RemoveKey("CustomProviderUrl", userSetting: true);
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

        private void CopilotProviderBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CopilotProviderBox.SelectedIndex)
            {
                case 0:
                    Configuration.Provider = AiProvider.Copilot;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 1:
                    Configuration.Provider = AiProvider.ChatGPT;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 2:
                    Configuration.Provider = AiProvider.Gemini;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    FlushSettings();
                    break;
                case 3:
                    Configuration.Provider = AiProvider.Custom;
                    CustomSearchPanel.Visibility = Visibility.Visible;
                    break;
            }
            FlushSettings();
        }

        private void QueryProviderTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the URL is valid
            if (Uri.TryCreate(QueryProviderTextBox.Text, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                CustomUrlStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CustomProviderUrl = QueryProviderTextBox.Text;
                FlushSettings();
            }
            else
            {
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }
    }
}
