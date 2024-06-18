// Copyright (c) 2024 valnoxy
// Copied from Dive: https://github.com/valnoxy/Dive/blob/main/Dive/Dive.UI/MessageUI.xaml.cs

using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace GoAwayEdge
{
    /// <summary>
    /// Interaktionslogik für MessageUI.xaml
    /// </summary>
    public partial class MessageUi
    {
        public virtual string? Summary => _buttonPressed;

        private static string? _buttonPressed;
        private static bool _mainThread;

        public MessageUi(string title, string message, string? btn1 = null, string? btn2 = null, string? btn3 = null, bool isMainThread = false)
        {
            InitializeComponent();

            MessageTitle.Text = title;
            MessageText.Text = message;
            this.Btn1.Content = btn1;
            this.Btn2.Content = btn2;
            this.Btn3.Content = btn3;

            if (btn1 is null or "")
                this.Btn1.Visibility = Visibility.Collapsed;
            if (btn2 is null or "")
                this.Btn2.Visibility = Visibility.Collapsed;
            if (btn3 is null or "")
                this.Btn3.Visibility = Visibility.Collapsed;
            
            VersionLbl.Content = $"Version {Assembly.GetExecutingAssembly().GetName().Version!}";

            _mainThread = isMainThread;
        }

        private void Btn1_OnClick(object sender, RoutedEventArgs e)
        {
            _buttonPressed = "Btn1";
            if (_mainThread) this.Hide();
            else this.Close();
        }

        private void Btn2_OnClick(object sender, RoutedEventArgs e)
        {
            _buttonPressed = "Btn2";
            if (_mainThread) this.Hide();
            else this.Close();
        }

        private void Btn3_OnClick(object sender, RoutedEventArgs e)
        {
            _buttonPressed = "Btn3";
            if (_mainThread) this.Hide();
            else this.Close();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.C || Keyboard.Modifiers != ModifierKeys.Control) return;

            var clipboardString = $"{MessageTitle.Text}\n-----\n{MessageText.Text}";
            Clipboard.SetDataObject(clipboardString);
        }
    }
}