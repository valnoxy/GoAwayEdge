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
            CopilotProviderBox.SelectedItem = Configuration.AiProvider.ToString().Replace("_", " ");

            if (Configuration.AiProvider == AiProvider.Custom)
            {
                CopilotProviderBox.SelectedItem = LocalizationManager.LocalizeValue("SettingsSearchEngineCustomItem");
                CustomSearchPanel.Visibility = Visibility.Visible;
                if (Configuration.CustomAiProviderUrl != null) QueryProviderTextBox.Text = Configuration.CustomAiProviderUrl;
                CustomUrlStatus.Symbol = Uri.TryCreate(QueryProviderTextBox.Text, UriKind.Absolute, out _)
                    ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24;
            }
            else
            {
                CopilotProviderBox.SelectedItem = Configuration.AiProvider.ToString();
            }
        }

        private void FlushSettings()
        {
            try
            {
                RegistryConfig.SetKey("AiProvider", Configuration.AiProvider.ToString(), userSetting: true);
                if (Configuration.AiProvider == AiProvider.Custom)
                {
                    if (Configuration.CustomAiProviderUrl != null)
                        RegistryConfig.SetKey("CustomAiProviderUrl", Configuration.CustomAiProviderUrl, userSetting: true);
                }
                else
                {
                    RegistryConfig.RemoveKey("CustomAiProviderUrl", userSetting: true);
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
                    Configuration.AiProvider = AiProvider.Copilot;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.AiProvider = AiProvider.ChatGPT;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.AiProvider = AiProvider.Gemini;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.AiProvider = AiProvider.GitHub_Copilot;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.AiProvider = AiProvider.Grok;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.AiProvider = AiProvider.Custom;
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
                Configuration.CustomAiProviderUrl = QueryProviderTextBox.Text;
                FlushSettings();
            }
            else
            {
                CustomUrlStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }
    }
}
