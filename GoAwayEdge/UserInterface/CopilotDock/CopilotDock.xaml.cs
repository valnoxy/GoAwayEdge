using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace GoAwayEdge.UserInterface.CopilotDock
{
    /// <summary>
    /// Interaktionslogik für CopilotDock.xaml
    /// </summary>
    public partial class CopilotDock
    {
        private static bool _isDocked;
        public CopilotDock()
        {
            InitializeComponent();
            DockWindowToRight();
        }

        // Testing
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #region Docking

        private void DockWindowToRight()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            Width = screenWidth * 0.3;
            Height = screenHeight;
            MinWidth = 200;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            // Set window position
            Left = screenWidth - Width;
            Top = 0;

            // Register App Bar
            RegisterAppBar();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_SETPOS = 0x00000003;
        private const int ABE_RIGHT = 2;

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        private void RegisterAppBar()
        {
            APPBARDATA appBarData = new APPBARDATA();
            appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);
            appBarData.hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            appBarData.uEdge = ABE_RIGHT;
            appBarData.rc.left = (int)(SystemParameters.PrimaryScreenWidth - this.Width);
            appBarData.rc.right = (int)SystemParameters.PrimaryScreenWidth;
            appBarData.rc.top = 0;
            appBarData.rc.bottom = (int)SystemParameters.PrimaryScreenHeight;

            SHAppBarMessage(ABM_NEW, ref appBarData);
            SHAppBarMessage(ABM_SETPOS, ref appBarData);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            AdjustWindowPosition();
        }

        private void AdjustWindowPosition()
        {
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isDocked) RegisterAppBar();
        }

        private void UnregisterAppBar()
        {
            APPBARDATA appBarData = new APPBARDATA();
            appBarData.cbSize = (uint)Marshal.SizeOf(appBarData);
            appBarData.hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            SHAppBarMessage(ABM_REMOVE, ref appBarData);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnregisterAppBar();
        }

        #endregion

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DockButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isDocked)
            {
                UnregisterAppBar();
                _isDocked = false;
            }
            else
            {
                DockWindowToRight();
                _isDocked = true;
            }
        }
    }
}
