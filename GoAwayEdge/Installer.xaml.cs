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
                "en-US" => new Uri(@"/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative),
                "de-DE" => new Uri(@"/GoAwayEdge;component/Localization/ResourceDictionary.de-DE.xaml", UriKind.Relative),
                _ => new Uri(@"/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(dict);
            VersionLbl.Content = $"Version {Assembly.GetExecutingAssembly().GetName().Version!}";

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
