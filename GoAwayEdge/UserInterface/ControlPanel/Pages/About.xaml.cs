using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GoAwayEdge.UserInterface.ControlPanel.Pages
{
    /// <summary>
    /// Interaktionslogik für About.xaml
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();

            string versionText;
            var assembly = Assembly.GetExecutingAssembly();
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            try
            {
                var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(informationVersion) && version.ToString() != informationVersion)
                {
                    versionText = $"{version}-{informationVersion}";
                }
                else
                {
                    versionText = $"{version}";
                }
            }
            catch
            {
                versionText = $"{version}";
            }
            var appDirectory = AppContext.BaseDirectory;
            var assemblyPath = Path.Combine(appDirectory, $"{assembly.GetName().Name}.exe");
            var fvi = FileVersionInfo.GetVersionInfo(assemblyPath);
            ValueVersion.Content = $"{fvi.ProductName} v{versionText}";
            ValueCopyright.Content = fvi.LegalCopyright;
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
