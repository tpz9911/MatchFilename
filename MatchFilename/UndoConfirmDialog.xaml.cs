using System;
using System.Windows;
using System.Windows.Threading;

namespace MatchFilename
{
    public partial class UndoConfirmDialog : Window
    {
        private readonly DispatcherTimer _countdownTimer;
        private int _remainingSeconds;

        public UndoConfirmDialog()
        {
            InitializeComponent();

            _remainingSeconds = AppConstants.UndoCountdownSeconds;
            UpdateConfirmButtonText();

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                BtnConfirm.IsEnabled = true;
                BtnConfirm.Content = "确定";
            }
            else
            {
                UpdateConfirmButtonText();
            }
        }

        private void UpdateConfirmButtonText()
        {
            BtnConfirm.Content = $"确定 ({_remainingSeconds})";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _countdownTimer.Stop();
            base.OnClosed(e);
        }
    }
}