using System.ComponentModel;
using GoAwayEdge.Common;
using InstallRoutine = GoAwayEdge.Common.Installation.InstallRoutine;

namespace GoAwayEdge.UserInterface.Setup.Pages
{
    /// <summary>
    /// Interaktionslogik für Installation.xaml
    /// </summary>
    public partial class Installation
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
