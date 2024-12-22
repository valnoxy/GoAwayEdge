using System.IO;
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

            if (Configuration.AiProvider == AiProvider.Default)
                CopilotProviderBox.SelectedItem = LocalizationManager.LocalizeValue("Default");

            if (Configuration.CopilotExternalApp != null) ExternalAppTextBox.Text = Configuration.CopilotExternalApp;
            if (Configuration.CopilotExternalAppArgument != null) ExternalAppArgsTextBox.Text = Configuration.CopilotExternalAppArgument;
        }

        private static void FlushSettings()
        {
            try
            {
                // Ai Provider
                if (Configuration.AiProvider == AiProvider.Custom)
                {
                    if (Configuration.CustomAiProviderUrl != null)
                    {
                        RegistryConfig.SetKey("AiProvider", Configuration.AiProvider.ToString(), userSetting: true);
                        RegistryConfig.SetKey("CustomAiProviderUrl", Configuration.CustomAiProviderUrl, userSetting: true);
                    }
                }
                else
                {
                    RegistryConfig.SetKey("AiProvider", Configuration.AiProvider.ToString(), userSetting: true);
                    RegistryConfig.RemoveKey("CustomAiProviderUrl", userSetting: true);
                }

                // External App
                if (Configuration.CopilotExternalApp != null)
                    RegistryConfig.SetKey("ExternalApp", Configuration.CopilotExternalApp, userSetting: true);
                else
                    RegistryConfig.RemoveKey("ExternalApp", userSetting: true);
                
                if (Configuration.CopilotExternalAppArgument != null)
                    RegistryConfig.SetKey("ExternalAppArgs", Configuration.CopilotExternalAppArgument, userSetting: true);
                else
                    RegistryConfig.RemoveKey("ExternalAppArgs", userSetting: true);
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
                    Configuration.AiProvider = AiProvider.Default;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    Configuration.AiProvider = AiProvider.Copilot;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Configuration.AiProvider = AiProvider.ChatGPT;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    Configuration.AiProvider = AiProvider.Gemini;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 4:
                    Configuration.AiProvider = AiProvider.GitHub_Copilot;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 5:
                    Configuration.AiProvider = AiProvider.Grok;
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    break;
                case 6:
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

        private void ExternalAppTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Test if the file exists
            if (File.Exists(ExternalAppTextBox.Text))
            {
                ExternalAppStatus.Symbol = SymbolRegular.CheckmarkCircle24;
                Configuration.CopilotExternalApp = ExternalAppTextBox.Text;
                FlushSettings();
            }
            else
            {
                ExternalAppStatus.Symbol = SymbolRegular.ErrorCircle24;
            }
        }

        private void ExternalAppArgsTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ExternalAppStatus.Symbol != SymbolRegular.CheckmarkCircle24) return;
            
            Configuration.CopilotExternalAppArgument = ExternalAppArgsTextBox.Text;
            FlushSettings();
        }
    }
}
