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

namespace ShowAutoRenamer {


    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            NotificationManager.Initialize(notification, nTitle, nText);
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) { smartRename.IsEnabled = false; smartRename.IsChecked = false; }

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            if (!CheckForInternetConnection()) { smartRename.IsChecked = false; smartRename.IsEnabled = false; }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".*";
            //dlg.Filter = "AVI (*.avi)|*.avi|MKV (*.mkv)|*.mkv|MP4 (*.mp4)|*.mp4";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true) {
                filePath.Text = dlg.FileName;
                UpdatePreview();
            }
        }

        private void begin_Click(object sender, RoutedEventArgs e) {
            Functions.Rename(filePath.Text, showName.Text);
        }

        private void Update(object sender, RoutedEventArgs e) {
            Functions.useFolder = (bool)useFolder.IsChecked;
            Functions.smartRename = (bool)smartRename.IsChecked;
            Functions.recursive = (bool)recursive.IsChecked;
            Functions.displayName = (bool)displayName.IsChecked;
            Functions.remove_ = (bool)remove_.IsChecked;
            Functions.removeDash = (bool)removeDash.IsChecked;
            UpdatePreview();
            
        }

        void UpdatePreview() {
            if (!File.Exists(filePath.Text)) { Preview.Content = "Path doesn't exist"; return; }
            string name = Path.GetFileNameWithoutExtension(filePath.Text);
            int s = Functions.GetSE(name, true);
            int e = Functions.GetSE(name, false);

            if ((bool)smartRename.IsChecked) {
                NotificationManager.DeleteSearchRelated();
                if (showName.Text == "") { NotificationManager.AddNotification(new Notification("Enter show name", "Please enter showname or uncheck Smart-Rename", true)); return; }
                string searched = Network.Search(showName.Text).Title;
                if (searched == "" || searched == null) { NotificationManager.AddNotification(new Notification("Couldn't find your show", "Unable to find show. You searched for (" + showName.Text + ")", true)); return; }
                else NotificationManager.AddNotification(new Notification("Found " + searched, "Smart-Rename will use this show to rename your files", true));

                Preview.Content = Functions.CreateFileName(Network.GetEpisode(searched, s, e).title, Functions.GetSE(name, true), Functions.GetSE(name, false), showName.Text);

            }
            else {
                NotificationManager.DeleteSearchRelated();

                //Debug.WriteLine(ProcessFilesInFolder(new string[] { filePath.Text }, name, s)[0].title);
                Preview.Content = Functions.ProcessFilesInFolder(new string[] { filePath.Text }, name, s)[0].title;
                //Preview.Content = CreateFileName(FormatName(name, s.ToString(), e.ToString()), s, e);
            }

        }

        float timeLeft;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        void PlannedUpdate() {
            if (!dispatcherTimer.IsEnabled) dispatcherTimer.Start();
            timeLeft = 1;
        }

        void LessTimeLeft(object sender, EventArgs e) {
            timeLeft -= 0.25f;
            if (timeLeft < 0) { dispatcherTimer.Stop(); UpdatePreview(); timeLeft = 0; }
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
            String host = "api.trakt.tv";
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

        private void showName_TextChanged(object sender, TextChangedEventArgs e) {
            //UpdatePreview();
            NotificationManager.DeleteSearchRelated();
            PlannedUpdate();
            if (showName.Text == "") showNameOverText.Visibility = System.Windows.Visibility.Visible;
            else showNameOverText.Visibility = System.Windows.Visibility.Hidden;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdatePreview();
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

    }
}