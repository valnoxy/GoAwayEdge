using System.ComponentModel;
using System.Windows.Controls;
using GoAwayEdge.Common;

namespace GoAwayEdge.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class Installation : UserControl
    {
        public Installation()
        {
            InitializeComponent();

            // Background worker for deployment
            var applyBackgroundWorker = new BackgroundWorker();
            applyBackgroundWorker.WorkerReportsProgress = true;
            applyBackgroundWorker.WorkerSupportsCancellation = true;
            if (Configuration.Uninstall)
            {
                applyBackgroundWorker.DoWork += InstallRoutine.Uninstall;
            }
            else
            {
                applyBackgroundWorker.DoWork += InstallRoutine.Install;
            }
            applyBackgroundWorker.ProgressChanged += ApplyBackgroundWorker_ProgressChanged;
            applyBackgroundWorker.RunWorkerAsync();
        }

        private static void ApplyBackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 100)
            {
                Installer.ContentWindow?.FrameWindow.NavigationService.Navigate(new InstallationSuccess());
            }
        }
    }
}
