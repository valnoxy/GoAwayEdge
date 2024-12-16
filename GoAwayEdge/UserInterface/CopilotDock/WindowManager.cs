using System.Windows;
using System.Windows.Threading;
using GoAwayEdge.Common.Debugging;
using ManagedShell;
using ManagedShell.AppBar;

namespace GoAwayEdge.UserInterface.CopilotDock;

public static class WindowManager
{
    private static CopilotDock? _copilotDockInstance;

    public static void ShowCopilotDockAsync(ShellManager shellManager, AppBarScreen screen, AppBarEdge edge, double desiredHeight, AppBarMode mode)
    {
        if (_copilotDockInstance == null)
        {
            _copilotDockInstance = new CopilotDock(shellManager, screen, edge, desiredHeight, mode);

            var frame = new DispatcherFrame();
            _copilotDockInstance.Closed += (s, e) =>
            {
                _copilotDockInstance = null;
                frame.Continue = false;
            };

            _copilotDockInstance.Show();
            Dispatcher.PushFrame(frame);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _copilotDockInstance.Show();
                _copilotDockInstance.Activate();
            });
        }
    }

    public static void HideCopilotDock()
    {
        try
        {
            _copilotDockInstance?.Hide();
        }
        catch (Exception ex)
        {
            Logging.Log("Failed to hide Copilot Dock: " + ex.Message);
        }
    }
}