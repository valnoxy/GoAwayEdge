// Copyright (c) 2023 valnoxy
// Copied from Dive: https://github.com/valnoxy/Dive/blob/main/Dive/Dive.UI/MessageUI.xaml.cs

using System;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
        private readonly DispatcherTimer _timer;

        public MessageUi(string title, string message, string btn1 = null, string btn2 = null, bool isMainThread = false, int timer = 0)
        {
            InitializeComponent();

            MessageTitle.Text = title;
            MessageText.Text = message;
            this.Btn1.Content = btn1;
            this.Btn2.Content = btn2;

            if (btn1 is null or "")
                this.Btn1.Visibility = Visibility.Hidden;
            if (btn2 is null or "")
                this.Btn2.Visibility = Visibility.Hidden;

            if (timer != 0)
            {
                var time = TimeSpan.FromSeconds(timer);

                _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                {
                    LbTimer.Text = $"{time.ToString("%s")}s before auto-selecting '{this.Btn2.Content}'.";
                    if (time == TimeSpan.Zero)
                    {
                        _timer?.Stop();
                        _buttonPressed = "Btn2";
                        if (_mainThread) this.Hide();
                        else this.Close();
                    }
                    time = time.Add(TimeSpan.FromSeconds(-1));
                }, Application.Current.Dispatcher);

                _timer.Start();
            }
            else LbTimer.Visibility = Visibility.Hidden;

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

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.C || Keyboard.Modifiers != ModifierKeys.Control) return;

            var clipboardString = $"{MessageTitle.Text}\n-----\n{MessageText.Text}";
            Clipboard.SetDataObject(clipboardString);
        }
    }
}