using ShowAutoRenamer.Classes;
using System;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShowAutoRenamer {

    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            NotificationManager.Initialize(notification, nTitle, nText, System.Windows.Threading.Dispatcher.CurrentDispatcher);

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            if (!CheckForInternetConnection()) { ToggleSmartRename.IsChecked = false; ToggleSmartRename.IsEnabled = false; }

            Functions.smartRename = (bool)ToggleSmartRename.IsChecked;
            Functions.displayName = (bool)FieldDisplayName.IsChecked;
            Functions.remove_ = (bool)ToggleRemoveUnderscore.IsChecked;
            Functions.removeDash = (bool)ToggleRemoveDash.IsChecked;

            LabelPreviewTitle.Content = "";
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "ALL|*.*|VIDEO FILES | *.mp4;*.avi;*.mkv";
            dlg.Multiselect = true;

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true) {
                Functions.fileQueue = dlg.FileNames;
                FileListBox.ItemsSource = Functions.fileQueue;
                UpdatePreview();
            }
        }

        private async void RenameButtonClick(object sender, RoutedEventArgs e) {
            if (Functions.fileQueue == null || Functions.fileQueue.Length == 0) {
                NotificationManager.AddNotification("No file added", "You can't rename void. Sorry.");
                return;
            }
            await Functions.Rename(InputShowName.Text);
        }

        void Update(object sender, RoutedEventArgs e) {
            Functions.smartRename = (bool)ToggleSmartRename.IsChecked;
            Functions.displayName = (bool)FieldDisplayName.IsChecked;
            Functions.remove_ = (bool)ToggleRemoveUnderscore.IsChecked;
            Functions.removeDash = (bool)ToggleRemoveDash.IsChecked;
            UpdatePreview();
        }

        async void UpdatePreview() {
            if (Functions.fileQueue == null || Functions.fileQueue.Length == 0) return;
            string name = string.IsNullOrWhiteSpace(InputShowName.Text) ? Functions.GetShowName(Functions.fileQueue[0]) : InputShowName.Text;
            Show s;
            if (Functions.smartRename)
                s = await Network.Search(name);
            else
                s = new Show(name);

            if (s != null) {
                if (string.IsNullOrWhiteSpace(InputShowName.Text)) {
                    ignoreTextChange = true;
                    InputShowName.Text = s.title;
                }

                Season season = new Season(Functions.GetSeason(Functions.fileQueue[0]), s);
                s.seasonList.Add(season);

                if (Functions.smartRename) {
                    s.seasonList[0].episodeList.Add(await Network.GetEpisode(s, season.season, Functions.GetEpisode(Functions.fileQueue[0])));

                    if (s.seasonList[0].episodeList.Count > 0 && s.seasonList[0].episodeList[0] != null)
                        LabelPreviewTitle.Content = RenameData.isRegexSet ?
                            Functions.RegexTitle(RenameData.regex, s.seasonList[0].episodeList[0]) :
                            Functions.ConstructName(s.seasonList[0].episodeList[0]);
                    else
                        LabelPreviewTitle.Content = "Episode not found";
                }
                else
                    LabelPreviewTitle.Content = RenameData.isRegexSet ?
                        Functions.RegexTitle(RenameData.regex, Functions.GetEpisodeFromName(Functions.fileQueue[0])) :
                        Functions.BeautifyName(s.seasonList[0].season, Functions.GetEpisode(Functions.fileQueue[0]), Functions.fileQueue[0]);
            }
            else {
                LabelPreviewTitle.Content = "Show could not be found";
            }

            //Show s = await Functions.PrepareShow(Functions.fileQueue, ShowNameInput.Text);
            /*if (s != null) {
                if (string.IsNullOrWhiteSpace(ShowNameInput.Text)) {
                    ignoreTextChange = true;
                    ShowNameInput.Text = s.title;
                }

                if (s.seasonList.Count > 0 && s.seasonList[0] != null && s.seasonList[0].episodeList.Count > 0 && s.seasonList[0].episodeList[0] != null)
                    LabelPreviewTitle.Content = Functions.ConstructName(
                        s.seasonList[0].episodeList[0],
                        ShowNameInput.Text);
            }*/
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
                Functions.fileQueue = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileListBox.ItemsSource = Functions.fileQueue;
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
            Close();
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

        private void nClose_Click(object sender, RoutedEventArgs e) {
            NotificationManager.RemoveNotification();
        }

        bool ignoreTextChange;
        private void TextChanged() {
            NotificationManager.DeleteSearchRelated();
            if (InputShowName.Text == "") showNameOverText.Visibility = System.Windows.Visibility.Visible;
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

        private void image_MouseEnter(object sender, MouseEventArgs e) {
            advancedTitleIcon.Source = new BitmapImage(new Uri("Icons/ic_settings_applications_bluish_24dp.png", UriKind.Relative)); ;
        }

        private void image_MouseLeave(object sender, MouseEventArgs e) {
            advancedTitleIcon.Source = new BitmapImage(new Uri("Icons/ic_settings_applications_whitish_24dp.png", UriKind.Relative)); ;
        }

        BitmapImage getIcon(string iconName) {
            BitmapImage icon = new BitmapImage();
            icon.BeginInit();
            icon.UriSource = new Uri("pack://application:,,,/AssemblyName;component/Icons/" + iconName);
            icon.EndInit();
            return icon;
        }

        private void advancedTitleIcon_MouseUp(object sender, MouseButtonEventArgs e) {
            TitleRegexWindow trw = new TitleRegexWindow();
            if (Functions.fileQueue != null && Functions.fileQueue.Length > 0) {
                Episode ep = Functions.GetEpisodeFromName(Functions.fileQueue[0]);
                if (!string.IsNullOrEmpty(InputShowName.Text))
                    ep.show = new Show(InputShowName.Text);
                trw.Initialize(ep);
            }

            if (trw.ShowDialog() == true) {
                trw.GetResults(out RenameData.regex, out RenameData.episodeAdd, out RenameData.seasonAdd);
                UpdatePreview();
            }
        }
    }
}