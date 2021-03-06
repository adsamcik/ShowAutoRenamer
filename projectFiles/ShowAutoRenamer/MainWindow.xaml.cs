﻿using ShowAutoRenamer.Classes;
using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShowAutoRenamer {

    public partial class MainWindow : Window {

        EpisodeFile[] Queue { get { return Functions.fileQueue; } }


        public MainWindow() {
            InitializeComponent();
            Functions.listBox = FileListBox;

            NotificationManager.Initialize(notification, nTitle, nText, Dispatcher.CurrentDispatcher);

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            if (!CheckForInternetConnection()) { ToggleSmartRename.IsChecked = false; ToggleSmartRename.IsEnabled = false; }

            Functions.smartRename = (bool)ToggleSmartRename.IsChecked;
            Functions.remove_ = (bool)ToggleRemoveUnderscore.IsChecked;
            Functions.removeDash = (bool)ToggleRemoveDash.IsChecked;

            LabelPreviewTitle.Content = "";
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "ALL|*.*|VIDEO FILES | *.mp4;*.avi;*.mkv",
                Multiselect = true
            };

            bool? result = dlg.ShowDialog();

            if (result == true && dlg.FileNames.Length > 0) {
                AddToQueue(dlg.FileNames);
                UpdatePreview(Functions.fileQueue[0]);
            }
        }

        private async void RenameButtonClick(object sender, RoutedEventArgs e) {
            if (Functions.fileQueue == null || Functions.fileQueue.Length == 0) {
                NotificationManager.AddNotification("No file added", "But don't worry, everything was renamed.");
                return;
            }
            await Functions.Rename(InputShowName.Text);
        }

        void Update(object sender, RoutedEventArgs e) {
            Functions.smartRename = (bool)ToggleSmartRename.IsChecked;
            Functions.remove_ = (bool)ToggleRemoveUnderscore.IsChecked;
            Functions.removeDash = (bool)ToggleRemoveDash.IsChecked;
            UpdatePreview();
        }

        void UpdatePreview() {
            if (Functions.fileQueue != null && Functions.fileQueue.Length > 0) {
                if (FileListBox.SelectedItem != null)
                    UpdatePreview((EpisodeFile)FileListBox.SelectedItem);
                else
                    UpdatePreview(Functions.fileQueue[0]);
            }
        }

        void UpdatePreview(EpisodeFile file) {
            if (Functions.smartRename) {
                var request = file.RequestEpisode();
                request.Wait();
                var episode = request.Result;
                if (episode != null) {
                    LabelPreviewTitle.Content = Functions.RegexTitle(RenameData.regex, episode);
                    return;
                }
            }
                
            LabelPreviewTitle.Content = Functions.RegexTitle(RenameData.regex, file.Episode);
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

        void AddToQueue(string[] files) {
            Show s = new Show();
            EpisodeFile[] ef = new EpisodeFile[files.Length];
            for (int i = 0; i < files.Length; i++)
                ef[i] = new EpisodeFile(s, files[i]);
            Functions.fileQueue = ef;
            FileListBox.ItemsSource = ef;
        }

        private void DropAction(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                AddToQueue((string[])e.Data.GetData(DataFormats.FileDrop));
                FileListBox.ItemsSource = Functions.fileQueue;
            }
            dragdropOverlay.Visibility = Visibility.Hidden;
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
            } catch { return false; }

            return false;
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            dragdropOverlay.Visibility = Visibility.Visible;
        }

        private void Window_DragLeave(object sender, DragEventArgs e) {
            dragdropOverlay.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        private void NotificationClose_Click(object sender, RoutedEventArgs e) {
            NotificationManager.RemoveNotification();
        }

        bool ignoreTextChange;
        private void TextChanged() {
            NotificationManager.DeleteSearchRelated();
            if (InputShowName.Text == "") showNameOverText.Visibility = Visibility.Visible;
            else showNameOverText.Visibility = Visibility.Hidden;

            if (!ignoreTextChange)
                PlannedUpdate();
            else
                ignoreTextChange = false;
        }

        private void ShowName_TextChanged(object sender, TextChangedEventArgs e) {
            TextChanged();
        }

        private void Image_MouseEnter(object sender, MouseEventArgs e) {
            advancedTitleIcon.Source = new BitmapImage(new Uri("Icons/ic_settings_applications_bluish_24dp.png", UriKind.Relative)); ;
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e) {
            advancedTitleIcon.Source = new BitmapImage(new Uri("Icons/ic_settings_applications_whitish_24dp.png", UriKind.Relative)); ;
        }

        BitmapImage GetIcon(string iconName) {
            BitmapImage icon = new BitmapImage();
            icon.BeginInit();
            icon.UriSource = new Uri("pack://application:,,,/AssemblyName;component/Icons/" + iconName);
            icon.EndInit();
            return icon;
        }

        private void AdvancedTitleIcon_MouseUp(object sender, MouseButtonEventArgs e) {
            TitleRegexWindow trw = new TitleRegexWindow();
            if (Functions.fileQueue != null && Functions.fileQueue.Length > 0) {
                Episode ep = Functions.GetEpisodeFromName(Functions.fileQueue[0].path);
                if (ep == null) {
                    NotificationManager.AddNotification(new Notification("Could not resolve show", "Show name format is invalid or unsupported", false, Importance.important));
                    return;
                }
                if (!string.IsNullOrEmpty(InputShowName.Text))
                    ep.show = new Show(InputShowName.Text);
                trw.Initialize(ep);
            }

            trw.Left = Left + 10;
            trw.Top = Top + (Height - trw.ActualHeight) / 4 - 15;

            if (trw.ShowDialog() == true) {
                trw.GetResults(out RenameData.regex, out RenameData.episodeAdd, out RenameData.seasonAdd);
                UpdatePreview();
            }
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdatePreview();
        }

        private void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileListBox.SelectedItem != null)
                System.Diagnostics.Process.Start("explorer.exe", "/select," + ((EpisodeFile)FileListBox.SelectedItem).path);
            //System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName());
        }
    }
}