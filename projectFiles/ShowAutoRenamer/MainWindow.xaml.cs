using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows.Input;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;

namespace ShowAutoRenamer {

    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            Network.Initialize(NetworkActivity);
            NotificationManager.Initialize(notification, nTitle, nText, System.Windows.Threading.Dispatcher.CurrentDispatcher);

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            if (!CheckForInternetConnection()) { smartRename.IsChecked = false; smartRename.IsEnabled = false; }

            Functions.useFolder = (bool)useFolder.IsChecked;
            Functions.smartRename = (bool)smartRename.IsChecked;
            Functions.recursive = (bool)recursive.IsChecked;
            Functions.displayName = (bool)displayName.IsChecked;
            Functions.remove_ = (bool)remove_.IsChecked;
            Functions.removeDash = (bool)removeDash.IsChecked;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "ALL|*.*|VIDEO FILES | *.mp4;*.avi;*.mkv";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true) {
                filePath.Text = dlg.FileName;
                UpdatePreview();
            }
        }

        private async void RenameButtonClick(object sender, RoutedEventArgs e) {
            await Functions.Rename(filePath.Text, showName.Text);
        }

        void Update(object sender, RoutedEventArgs e) {
            Functions.useFolder = (bool)useFolder.IsChecked;
            Functions.smartRename = (bool)smartRename.IsChecked;
            Functions.recursive = (bool)recursive.IsChecked;
            Functions.displayName = (bool)displayName.IsChecked;
            Functions.remove_ = (bool)remove_.IsChecked;
            Functions.removeDash = (bool)removeDash.IsChecked;
            UpdatePreview();
        }

        async void UpdatePreview() {
            Show s = await Functions.PrepareShow(filePath.Text, showName.Text);
            if (s != null) {
                if (string.IsNullOrWhiteSpace(showName.Text)) showName.Text = s.title;

                if (s.seasonList.Count > 0 && s.seasonList[0] != null && s.seasonList[0].episodeList.Count > 0 && s.seasonList[0].episodeList[0] != null)
                    Preview.Content = Functions.ConstructName(
                        s.seasonList[0].episodeList[0],
                        showName.Text);
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {

                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // We will use only one
                filePath.Text = files[0];
            }
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
            useFolder.IsChecked = true;
        }

        private void useFolder_Unchecked(object sender, RoutedEventArgs e) {
            recursive.IsChecked = false;
        }

        private void nClose_Click(object sender, RoutedEventArgs e) {
            NotificationManager.RemoveNotification();
        }

        private void TextChanged() {
            NotificationManager.DeleteSearchRelated();
            if (showName.Text == "") showNameOverText.Visibility = System.Windows.Visibility.Visible;
            else showNameOverText.Visibility = System.Windows.Visibility.Hidden;
            PlannedUpdate();
        }

        private void showName_TextChanged(object sender, TextChangedEventArgs e) {
            if (!Functions.insideInput) TextChanged();
            else Functions.insideInput = false;
        }

    }
}