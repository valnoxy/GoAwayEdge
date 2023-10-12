using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GoAwayEdge.Pages;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für Installer.xaml
    /// </summary>
    public partial class Installer
    {
        internal static Installer? ContentWindow;
        private static License? _licensePage;
        private static Settings? _settingPage;
        
        public Installer()
        {
            InitializeComponent();

            // Set current language model
            var language = Thread.CurrentThread.CurrentCulture.ToString();
            var dict = new ResourceDictionary();
            Debug.WriteLine("Trying to load language: " + language);
            dict.Source = language switch
            {
                "en-US" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative),
                "de-DE" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.de-DE.xaml", UriKind.Relative),
                "es-ES" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.es-ES.xaml", UriKind.Relative),
                "fr-FR" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.fr-FR.xaml", UriKind.Relative),
                "it-IT" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.it-IT.xaml", UriKind.Relative),
                "pl-PL" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.pl-PL.xaml", UriKind.Relative),
                _ => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative)
            };
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
                VersionLbl.Content = $"Version {Assembly.GetExecutingAssembly().GetName().Version!}";
            }
            catch (Exception ex)
            {
                // TODO: Implement message box
                // Failed to load language: {ex.Message}
                return;
            }
            
            _licensePage = new License();
            FrameWindow.Content = _licensePage;
            ContentWindow = this;
        }

        internal void NextBtn_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case Settings:
                    NextBtn.IsEnabled = false;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = new Installation();
                    break;
                case License:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = true;
                    _settingPage = new Settings();
                    FrameWindow.Content = _settingPage;
                    break;
            }
        }

        private void BackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            switch (FrameWindow.Content)
            {
                case Settings:
                    NextBtn.IsEnabled = true;
                    BackBtn.IsEnabled = false;
                    FrameWindow.Content = _licensePage;
                    break;
            }
        }
    }
}
