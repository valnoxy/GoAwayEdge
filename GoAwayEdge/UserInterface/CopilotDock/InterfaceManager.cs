using GoAwayEdge.Common;
using GoAwayEdge.Common.Debugging;
using GoAwayEdge.Common.Runtime;
using ManagedShell.AppBar;
using System.Windows;
using ManagedShell;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    public class InterfaceManager
    {
        private const string MutexName = "GoAwayEdge_CopilotDock";
        private const string PipeName = "GoAwayEdge_CopilotDockPipe";
        private static readonly NamedPipeManager PipeManager = new(PipeName);

        public static void ShowDock()
        {
            using var mutex = new Mutex(true, MutexName, out var createdNew);
            if (createdNew)
            {
                PipeManager.StartServer();
                var mode = AppBarMode.Normal;

                // Last state
                try
                {
                    var lastState = RegistryConfig.GetKey("CopilotDockState", userSetting: true);
                    if (string.IsNullOrEmpty(lastState))
                    {
                        // Never set; leave Mode as Normal
                        RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
                    }
                    else if (lastState != "Docked")
                    {
                        // Undocked or unknown state; set to None
                        mode = AppBarMode.None;
                    }
                }
                catch
                {
                    // Error reading last state; set to Docked
                    RegistryConfig.SetKey("CopilotDockState", "Docked", userSetting: true);
                }

                // PipeManager configuration
                PipeManager.MessageReceived += (message) =>
                {
                    Logging.Log($"Message received: {message}");
                    if (message.Contains("BringToFront"))
                    {
                        WindowManager.ShowCopilotDockAsync(Common.Configuration.ShellManager, AppBarScreen.FromPrimaryScreen(), AppBarEdge.Right, 500, mode); // Dummy data
                    }
                };

                PipeManager.ErrorOccurred += (ex) =>
                {
                    Logging.Log($"Error occurred: {ex.Message}", Logging.LogLevel.ERROR);
                };

                WindowManager.ShowCopilotDockAsync(Common.Configuration.ShellManager,
                    AppBarScreen.FromPrimaryScreen(),
                    AppBarEdge.Right,
                    500, // temporary size
                    mode);


                // Dock was closed
                Logging.Log("Closed CopilotDock");
                Environment.Exit(0);
            }
            else
            {
                PipeManager.SendMessage("BringToFront");
                Environment.Exit(0); // Exit the second instance
            }
        }
    }
}
