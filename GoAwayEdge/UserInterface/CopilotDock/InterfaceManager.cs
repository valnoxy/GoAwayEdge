using System.IO.Pipes;
using System.IO;
using System.Windows;
using GoAwayEdge.Common;
using GoAwayEdge.Common.Debugging;
using ManagedShell.AppBar;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    public class InterfaceManager
    {
        private static AppBarWindow? _dockWindow;
        private static NamedPipeServerStream? _pipeServer;
        private static Thread? _pipeThread;
        private static bool _closed = false;

        public static void ShowDock()
        {
            using var mutex = new Mutex(true, "GoAwayEdge_CopilotDock", out var createdNew);
            if (createdNew)
            {
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

                _dockWindow = new CopilotDock(
                    Configuration.ShellManager,
                    AppBarScreen.FromPrimaryScreen(),
                    AppBarEdge.Right,
                    500, // temporary size
                    mode);

                _dockWindow.Closed += (_, _) =>
                {
                    _closed = true;
                    StopNamedPipeServer(); // Stop the pipe when the dock is closed
                };
                _dockWindow.ShowDialog();

                // Dock is inactive, going now into loop...
                while (!_closed)
                {
                    StartNamedPipeServer(); // Start pipe server if the dock is inactive
                    Thread.Sleep(1000);
                }

                // Dock was closed
                Logging.Log("Closed CopilotDock");
                Environment.Exit(0);
            }
            else
            {
                // check if dock is in background
                BringToFront();
            }
        }

        private static void StartNamedPipeServer()
        {
            if (_pipeServer != null) return;

            _pipeThread = new Thread(() =>
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream("CopilotDockPipe", PipeDirection.InOut);
                    Logging.Log("Named Pipe Server opened...");

                    while (true)
                    {
                        _pipeServer.WaitForConnection();

                        using (var reader = new StreamReader(_pipeServer))
                        {
                            var message = reader.ReadLine();
                            if (message == "BringToFront")
                            {
                                Logging.Log("Received 'BringToFront' command via Named Pipe.");

                                try
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        CopilotDock.Instance!.Visibility = Visibility.Visible;

                                        try
                                        {
                                            _dockWindow.Visibility = Visibility.Visible;
                                            _dockWindow.Activate();
                                        }
                                        catch (Exception ex)
                                        {
                                            Logging.Log("Stage 2: Failed to bring CopilotDock to the front: " + ex.Message, Logging.LogLevel.ERROR);
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Logging.Log("Stage 1: Failed to bring CopilotDock to the front: " + ex.Message, Logging.LogLevel.ERROR);
                                }
                            }
                        }

                        _pipeServer.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log("Error in Named Pipe Server: " + ex.Message, Logging.LogLevel.ERROR);
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            });

            _pipeThread.IsBackground = true;
            _pipeThread.Start();
        }


        private static void StopNamedPipeServer()
        {
            if (_pipeServer != null)
            {
                _pipeServer.Disconnect();
                _pipeServer.Dispose();
                _pipeServer = null;
            }

            if (_pipeThread is not { IsAlive: true }) return;
            _pipeThread.Join();
            _pipeThread = null;
        }

        private static void BringToFront()
        {
            using var pipeClient = new NamedPipeClientStream(".", "CopilotDockPipe", PipeDirection.Out);
            try
            {
                pipeClient.Connect(1000);
                using var writer = new StreamWriter(pipeClient);
                writer.WriteLine("BringToFront");
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logging.Log("Failed to connect to CopilotDock pipe: " + ex.Message, Logging.LogLevel.ERROR);
            }
        }
    }
}
