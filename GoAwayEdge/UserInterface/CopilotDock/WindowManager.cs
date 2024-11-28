using System.Windows;
using System.Windows.Threading;
using GoAwayEdge.Common.Debugging;
using ManagedShell;
using ManagedShell.AppBar;

namespace GoAwayEdge.UserInterface.CopilotDock;

public static class WindowManager
{
    private static CopilotDock? _copilotDockInstance;
    private static TaskCompletionSource<bool>? _closeCompletionSource;

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
            _copilotDockInstance.Show();
            _copilotDockInstance.Activate();
        }
    }

    private static void OnCopilotDockClosed(object sender, System.EventArgs e)
    {
        _closeCompletionSource?.TrySetResult(true);
        if (_copilotDockInstance != null) _copilotDockInstance.Closed -= OnCopilotDockClosed!;
        _copilotDockInstance = null;
    }

    public static void HideCopilotDock()
    {
        _copilotDockInstance?.Hide();
    }

    public static void ShowHiddenCopilotDock()
    {
        if (_copilotDockInstance is not { IsVisible: false }) return;
        _copilotDockInstance.Show();
        _copilotDockInstance.Activate();
    }
}