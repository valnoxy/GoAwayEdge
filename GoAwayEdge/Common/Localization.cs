using System.Windows;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge.Common
{
    internal class LocalizationManager
    {
        public static void LoadLanguage()
        {
            // Set current language model
            var language = Thread.CurrentThread.CurrentCulture.ToString();
            var dict = new ResourceDictionary();
            Logging.Log($"Trying to load language: " + language);
            System.Diagnostics.Debug.WriteLine("Trying to load language: " + language);
            dict.Source = language switch
            {
                "en-US" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative),
                "de-DE" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.de-DE.xaml", UriKind.Relative),
                "es-ES" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.es-ES.xaml", UriKind.Relative),
                "fr-FR" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.fr-FR.xaml", UriKind.Relative),
                "it-IT" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.it-IT.xaml", UriKind.Relative),
                "pl-PL" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.pl-PL.xaml", UriKind.Relative),
                "ko-KR" => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.ko-KR.xaml", UriKind.Relative),
                _ => new Uri("/GoAwayEdge;component/Localization/ResourceDictionary.xaml", UriKind.Relative)
            };
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to load language: {ex}", Logging.LogLevel.ERROR);
                var messageUi = new MessageUi("GoAwayEdge",
                    $"Failed to load language: {ex.Message}", "OK", isMainThread: true);
                messageUi.ShowDialog();
                Environment.Exit(1);
            }
        }

        public static string LocalizeValue(string value)
        {
            try
            {
                var localizedValue = (string)Application.Current.Resources[value]!;
                return string.IsNullOrEmpty(localizedValue) ? value : localizedValue;
            }
            catch (Exception ex)
            {
                Logging.Log($"Failed to localize value: {ex}", Logging.LogLevel.ERROR);
                return value;
            }
        }

        public static string LocalizeValue(string value, params object[]? args)
        {
            var localizedValue = LocalizeValue(value);

            if (args is not { Length: > 0 }) return localizedValue;
            
            try
            {
                localizedValue = string.Format(localizedValue, args);
            }
            catch (FormatException ex)
            {
                Logging.Log($"Failed to format localized value: {ex}", Logging.LogLevel.ERROR);
                return value;
            }

            return localizedValue;
        }
    }
}