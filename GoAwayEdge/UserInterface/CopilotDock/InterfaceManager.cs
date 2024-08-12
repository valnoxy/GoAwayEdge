using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GoAwayEdge.Common.Debugging;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    public class InterfaceManager
    {
        public static void ShowDock()
        {
            using var mutex = new Mutex(true, "GoAwayEdge_CopilotDock", out var createdNew);
            if (createdNew)
            {
                var closed = false;
                var copilotDock = new CopilotDock();
                copilotDock.Closed += (sender, args) => closed = true;
                copilotDock.ShowDialog();

                while (!closed)
                {
                    Thread.Sleep(1000);
                }
                Logging.Log("Closed CopilotDock", Logging.LogLevel.INFO);
            }
            else
            {
                // check if dock is in background
                BringToFront();
            }
        }

        private static void BringToFront()
        {
            var currentProcess = Process.GetCurrentProcess();
            var currentTitle = currentProcess.MainWindowTitle;
            var currentId = currentProcess.Id;
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (var process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        var handle = GetWindowHandle(process.Id, process.MainWindowTitle);
                        if (handle != IntPtr.Zero)
                        {
                            // show window
                            ShowWindow(handle, 5);
                            // send WM_SHOWWINDOW message to toggle the visibility flag
                            SendMessage(handle, WM_SHOWWINDOW, IntPtr.Zero, new IntPtr(SW_PARENTOPENING));
                        }
                    }
                }
            }
        }

        // "stolen" from StackOverflow >:)
        // https://stackoverflow.com/questions/21154693/activate-a-hidden-wpf-application-when-trying-to-run-a-second-instance
        const int GWL_EXSTYLE = (-20);
        const uint WS_EX_APPWINDOW = 0x40000;

        const uint WM_SHOWWINDOW = 0x0018;
        const int SW_PARENTOPENING = 3;

        [DllImport("user32.dll")]
        private static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc ewp, int lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern uint GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowText(IntPtr hWnd, StringBuilder lpString, uint nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        static bool IsApplicationWindow(IntPtr hWnd)
        {
            return (GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_APPWINDOW) != 0;
        }

        static IntPtr GetWindowHandle(int pid, string title)
        {
            var result = IntPtr.Zero;

            EnumWindowsProc enumerateHandle = delegate (IntPtr hWnd, int lParam)
            {
                int id;
                GetWindowThreadProcessId(hWnd, out id);

                if (pid == id)
                {
                    var clsName = new StringBuilder(256);
                    var hasClass = GetClassName(hWnd, clsName, 256);
                    if (hasClass)
                    {

                        var maxLength = (int)GetWindowTextLength(hWnd);
                        var builder = new StringBuilder(maxLength + 1);
                        GetWindowText(hWnd, builder, (uint)builder.Capacity);

                        var text = builder.ToString();
                        var className = clsName.ToString();

                        // There could be multiple handle associated with our pid, 
                        // so we return the first handle that satisfy:
                        // 1) the handle title/ caption matches our window title,
                        // 2) the window class name starts with HwndWrapper (WPF specific)
                        // 3) the window has WS_EX_APPWINDOW style

                        if (title == text && className.StartsWith("HwndWrapper") && IsApplicationWindow(hWnd))
                        {
                            result = hWnd;
                            return false;
                        }
                    }
                }
                return true;
            };

            EnumDesktopWindows(IntPtr.Zero, enumerateHandle, 0);

            return result;
        }
    }
}
