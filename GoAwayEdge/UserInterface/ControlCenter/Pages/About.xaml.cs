using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace GoAwayEdge.UserInterface.ControlCenter.Pages
{
    /// <summary>
    /// Interaktionslogik für About.xaml
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            var product = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;
            var copyright = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright;
            ValueVersion.Content = $"{product} v{version}";
            ValueCopyright.Content = copyright;
        }

        private void Homepage_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://valnoxy.dev", UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void Donate_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://paypal.me/valnoxy", UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void SourceCode_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/valnoxy/GoAwayEdge", UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }
    }
}
