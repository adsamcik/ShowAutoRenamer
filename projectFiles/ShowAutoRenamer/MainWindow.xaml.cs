using System;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ShowAutoRenamer {

    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            Network.Initialize(NetworkActivity);
            NotificationManager.Initialize(notification, nTitle, nText, System.Windows.Threading.Dispatcher.CurrentDispatcher);

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            if (!CheckForInternetConnection()) { SmartRenameToggle.IsChecked = false; SmartRenameToggle.IsEnabled = false; }

            Functions.useFolder = (bool)UseFolderToggle.IsChecked;
            Functions.smartRename = (bool)SmartRenameToggle.IsChecked;
            Functions.recursive = (bool)RecursiveToggle.IsChecked;
            Functions.displayName = (bool)DisplayNameField.IsChecked;
            Functions.removeUnderscore = (bool)RemoveUnderscoreToggle.IsChecked;
            Functions.removeDash = (bool)RemoveDashToggle.IsChecked;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "ALL|*.*|VIDEO FILES | *.mp4;*.avi;*.mkv";
            dlg.Multiselect = true;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true) {
                Functions.fileQueue = dlg.FileNames;
                UpdatePreview();
            }
        }

        private async void RenameButtonClick(object sender, RoutedEventArgs e) {
            if (Functions.fileQueue == null || Functions.fileQueue.Length == 0) {
                NotificationManager.AddNotification("No file added", "You can't rename void. Sorry.");
                return;
            }
            await Functions.Rename(ShowNameInput.Text);
        }

        void Update(object sender, RoutedEventArgs e) {
            Functions.useFolder = (bool)UseFolderToggle.IsChecked;
            Functions.smartRename = (bool)SmartRenameToggle.IsChecked;
            Functions.recursive = (bool)RecursiveToggle.IsChecked;
            Functions.displayName = (bool)DisplayNameField.IsChecked;
            Functions.removeUnderscore = (bool)RemoveUnderscoreToggle.IsChecked;
            Functions.removeDash = (bool)RemoveDashToggle.IsChecked;
            UpdatePreview();
        }

        async void UpdatePreview() {
            Show s = await Functions.PrepareShow(Functions.fileQueue, ShowNameInput.Text);
            if (s != null) {
                if (string.IsNullOrWhiteSpace(ShowNameInput.Text)) {
                    ignoreTextChange = true;
                    ShowNameInput.Text = s.title;
                }

                if (s.seasonList.Count > 0 && s.seasonList[0] != null && s.seasonList[0].episodeList.Count > 0 && s.seasonList[0].episodeList[0] != null)
                    Preview.Content = Functions.ConstructName(
                        s.seasonList[0].episodeList[0],
                        ShowNameInput.Text);
            }
        }

        float timeLeft;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        void PlannedUpdate() {
            if (!dispatcherTimer.IsEnabled) dispatcherTimer.Start();
            timeLeft = 1;
        }

        void LessTimeLeft(object sender, EventArgs e) {
            timeLeft -= 0.25f;
            if (timeLeft < 0) {
                dispatcherTimer.Stop();
                UpdatePreview();
                timeLeft = 0;
            }
        }

        private void drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                Functions.fileQueue = (string[])e.Data.GetData(DataFormats.FileDrop);

            dragdropOverlay.Visibility = System.Windows.Visibility.Hidden;
            UpdatePreview();
        }

        public static bool CheckForInternetConnection() {
            Ping myPing = new Ping();
            String host = "api-v2launch.trakt.tv";
            byte[] buffer = new byte[32];
            int timeout = 1000;
            PingOptions pingOptions = new PingOptions();
            try {
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                if (reply.Status == IPStatus.Success) {
                    return true;
                }
            }
            catch { return false; }

            return false;
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            dragdropOverlay.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_DragLeave(object sender, DragEventArgs e) {
            dragdropOverlay.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        private void recursive_Checked(object sender, RoutedEventArgs e) {
            UseFolderToggle.IsChecked = true;
        }

        private void useFolder_Unchecked(object sender, RoutedEventArgs e) {
            RecursiveToggle.IsChecked = false;
        }

        private void nClose_Click(object sender, RoutedEventArgs e) {
            NotificationManager.RemoveNotification();
        }

        bool ignoreTextChange;
        private void TextChanged() {
            NotificationManager.DeleteSearchRelated();
            if (ShowNameInput.Text == "") showNameOverText.Visibility = System.Windows.Visibility.Visible;
            else showNameOverText.Visibility = System.Windows.Visibility.Hidden;

            if (!ignoreTextChange)
                PlannedUpdate();
            else
                ignoreTextChange = false;
        }

        private void showName_TextChanged(object sender, TextChangedEventArgs e) {
            if (!Functions.insideInput) TextChanged();
            else Functions.insideInput = false;
        }

    }
}