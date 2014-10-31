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

namespace ShowAutoRenamer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) { episodeNaming.IsEnabled = false; episodeNaming.IsChecked = false; }

            dispatcherTimer.Tick += new EventHandler(LessTimeLeft);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".*";
            //dlg.Filter = "AVI (*.avi)|*.avi|MKV (*.mkv)|*.mkv|MP4 (*.mp4)|*.mp4";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true) {
                // Open document 
                string filename = dlg.FileName;
                textBox.Text = filename;

                UpdatePreview();

            }
        }

        private void begin_Click(object sender, RoutedEventArgs e) {
            Rename();
        }

        void RecursiveFolderRenamer(string path) {
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);
            string[] directories = Directory.GetDirectories(path);

            for (int i = 0; i < directories.Length; i++) {RecursiveFolderRenamer(directories[i]); }

            if ((bool)episodeNaming.IsChecked) SmartRename(Directory.GetFiles(path));
            else ClassicRename(Directory.GetFiles(path));
        }

        void Rename() {
            string path = Path.GetDirectoryName(textBox.Text);
            if (!Directory.Exists(path)) return;

            if ((bool)recursive.IsChecked && (bool)useFolder.IsChecked) RecursiveFolderRenamer(path);
            else {
                string[] files;
                if ((bool)useFolder.IsChecked) files = Directory.GetFiles(path);
                else files = new string[] { textBox.Text };

                if ((bool)episodeNaming.IsChecked) SmartRename(files);
                else ClassicRename(files);
            }

        }

        void ClassicRename(string[] files) {
            string name = Path.GetFileNameWithoutExtension(textBox.Text);
            int season = GetSE(name, true);

            for (int i = 0; i < files.Length; i++) {
                name = Path.GetFileNameWithoutExtension(files[i]);
                int episode = GetSE(name, false);
                name = normalizeName(CreateFileName(name, season, episode));
                string newPath = Path.GetDirectoryName(files[i]) + "/" + name + Path.GetExtension(files[i]);

                if (!File.Exists(newPath)) System.IO.File.Move(files[i], newPath);
                else { statusText.Text = "There is already " + Path.GetFileName(newPath); return; }
            }
        }
        void SmartRename(string[] files) {
            string name = Search(showName.Text).Title;
            Season season = GetSeason(name, GetSE(Path.GetFileNameWithoutExtension(textBox.Text), true));

            List<Episode> episodes = GetEpisodes(name, season.season);
            if (season.season < 0) return;
            for (int i = 0; i < files.Length; i++) {
                name = Path.GetFileNameWithoutExtension(files[i]);
                int episode = GetSE(name, false);
                if (episode >= episodes.Count) name = normalizeName(CreateFileName(name, season.season, episode));
                else name = normalizeName(CreateFileName(season.season, episode, episodes[episode - 1].title));
                string newPath = Path.GetDirectoryName(files[i]) + "/" + name + Path.GetExtension(files[i]);

                if (!File.Exists(newPath)) System.IO.File.Move(files[i], newPath);
                else { statusText.Text = "There is already " + Path.GetFileName(newPath);}
            }
        }

        string normalizeName(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }

        string Request(string adress) {
            
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(adress);
            // Get the response.
            WebResponse response = request.GetResponse();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Clean up the streams and the response.
            reader.Close();
            response.Close();

            return responseFromServer;
        }


        Show Search(string forWhat) {
            List<Show> episodes = JsonConvert.DeserializeObject<List<Show>>(Request("http://api.trakt.tv/search/shows.json/c01ff5475f1333863127ffd8816fb776?query=" + forWhat));
            if (episodes.Count == 0) { statusText.Text = "Couldn't find any shows"; return new Show(); }
            statusText.Text = "Found " + episodes[0].Title;
            return episodes[0];
        }

        List<Episode> GetEpisodes(string showName, int season) {
            string title = Search(showName).Title.Replace(" ", "-").Replace("(","").Replace(")","");
            List<Episode> episodes = JsonConvert.DeserializeObject<List<Episode>>(Request("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season));
            return episodes;
        }

        /// <summary>
        /// Recommended to use since it uses caching
        /// </summary>
        /// <param name="showName"></param>
        /// <param name="season"></param>
        /// <returns></returns>
        Episode GetEpisode(string showName, int season, int episode) {
            episode--;
            string title = Search(showName).Title;
            if (title == null) return new Episode("Show not found!");
            title = title.Replace(" ", "-").Replace("(", "").Replace(")", "");
            List<Episode> episodes = JsonConvert.DeserializeObject<List<Episode>>(Request("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season));
            if (episodes.Count == 0) return new Episode("Show not found");
            else if (episode >= episodes.Count) return new Episode("Found " + title + " which in season " + ++season + " has less episodes");
            return episodes[episode];
        }

        int GetEpisodeCount(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName))[season].episodes;
        }

        Season GetSeason(string showName, int season) {
            return JsonConvert.DeserializeObject<List<Season>>(Request("http://api.trakt.tv/show/seasons.json/c01ff5475f1333863127ffd8816fb776/" + showName)).Find(x => x.season == season);
        }


        string CreateFileName(string n, int s, int e) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();

            n = ((bool)episodeNaming.IsChecked && showName.Text != "") ? GetEpisode(showName.Text, s,e).title : FormatName(n, season, episode) ;

            //string final = (n.Length == 0) ? "S" + s + "E" + e : "S" + s + "E" + e + " - " + n;
            if (n == null) n = "Error during name creation";

            string final = (n.Length == 0) ? "S" + season + "E" + episode : "S" + season + "E" + episode + " - " + n;
            final = ((bool)displayName.IsChecked && showName.Text.Trim() != "") ? showName.Text + " " + final : final;

            return final;
        }

        string CreateFileName(int s, int e, string episodeName) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();

            string final = "S" + season + "E" + episode + " - " + episodeName;
            final = ((bool)displayName.IsChecked && showName.Text.Trim() != "") ? showName.Text + " " + final : final;

            return final;
        }



        string FormatName(string n, string s, string e) {
            if (n.Contains("(" + s + "x" + e + ")")) n = n.Replace("(" + s + "x" + e + ")", "");

            if (n.ToUpper().Contains(e)) n = Regex.Split(n, e, RegexOptions.IgnoreCase)[1];
            else if (n.ToUpper().Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if (int.TryParse(x[0].Last().ToString(), out result) && int.TryParse(x[1].First().ToString(), out result)) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) x[0] = x[0].Remove(x[0].Length - 2, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) x[0] = x[0].Remove(x[0].Length - 1, 1);

                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) x[1] = x[1].Remove(0, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) x[1] = x[1].Remove(0, 1); ;

                    n = string.Join("", x);
                }
            }

            n = TestForEndings(n);
            n = n.Replace('.', ' ');

            if ((bool)removeMinus.IsChecked) n = Regex.Replace(n, "-", " ", RegexOptions.IgnoreCase);
            if ((bool)remove_.IsChecked) n = Regex.Replace(n, "_", " ", RegexOptions.IgnoreCase);
            if ((bool)displayName.IsChecked && showName.Text.Trim() != "") n = Regex.Replace(n, showName.Text, " ", RegexOptions.IgnoreCase);

            n = Regex.Replace(n, @"\s+", " ");

            while (true) {
                bool changed = false;
                n = n.Trim();
                if (n.StartsWith("-")) { n = n.TrimStart('-'); changed = true; }

                if (!changed) break;
            }
       
            return n;
        }

        string TestForEndings(string n) {

            string[] splitters = new string[] { "1080", "720", "DVD", "PROPER", "HD", "REPACK", "DVB", "CZ", "EN", "ENG", "HDTV" };

            for (int i = 0; i < splitters.Length; i++) {
                n = Contains(n, splitters[i]);
            }
            return n;
        }

        string Contains(string who, string what) {
            string tempString = Regex.Replace(who, "-", " ", RegexOptions.IgnoreCase);
            tempString = Regex.Replace(tempString, "_", " ", RegexOptions.IgnoreCase);
            tempString = tempString.ToUpper();

            if (tempString.Contains(what)) {
                string[] split = Regex.Split(tempString, what, RegexOptions.IgnoreCase);
                if ((split[split.Length - 2].EndsWith(" ") || split[split.Length - 2].EndsWith(".") || split[split.Length - 2].Length < 1) &&
                    (split[split.Length - 1].StartsWith(" ") || split[split.Length - 1].StartsWith(".") || split[split.Length - 1].Length < 1)) {
                    if (who.Length - 3 > split[split.Length - 1].Length) who = who.Remove(who.Length - 3 - split[split.Length - 1].Length);
                    Contains(who, what);
                }
            }

            who = who.Trim();
            if (who.EndsWith("-")) who.Remove(who.Length - 2);

            return who;
        }

        int GetSE(string n, bool season) {
            n = n.ToUpper();
            if (n.Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if (int.TryParse(x[0].Last().ToString(), out result) && int.TryParse(x[1].First().ToString(), out result)) {
                    if (season) {
                        if (!((x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) || (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)))) result = 1;
                    }
                    else if (!((x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) || (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)))) result = 1;

                    return result;
                }
            }
            
            string lookFor = (season) ? "S" : "E";

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (n.Contains(lookFor + i + y)) return i * 10 + y;
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (i == 0 && n.Contains(lookFor + y)) return y;
                }
            }

            if (n.Contains("PILOT")) return 0;

             if(!season) {
                int result;
                if (int.TryParse(n.Substring(0, 2), out result) || int.TryParse(n.Substring(0, 1), out result)) return result;
            }

            return 1;
        }

        private void Update(object sender, RoutedEventArgs e) {
            RecursiveFolderRenamer(textBox.Text);
            UpdatePreview();
        }

        void UpdatePreview() {
            if (!File.Exists(textBox.Text)) { Preview.Content = "Path doesn't exist"; return; }
            string name = Path.GetFileNameWithoutExtension(textBox.Text);
            Preview.Content = CreateFileName(name, GetSE(name, true), GetSE(name, false));
        }

        private void showName_TextChanged(object sender, TextChangedEventArgs e) {
            //UpdatePreview();
            PlannedUpdate();
            if (showName.Text == "") showNameOverText.Visibility = System.Windows.Visibility.Visible;
            else showNameOverText.Visibility = System.Windows.Visibility.Hidden;
        }

        float timeLeft;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        void PlannedUpdate() {
            if (!dispatcherTimer.IsEnabled) dispatcherTimer.Start();
            timeLeft = 1;         
        }

        void LessTimeLeft(object sender, EventArgs e) {
            timeLeft -= 0.25f;
            if (timeLeft < 0) { dispatcherTimer.Stop(); UpdatePreview(); timeLeft = 0;}
        }

        private void drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                textBox.Text = files[0];
            }
            dragdropOverlay.Visibility = System.Windows.Visibility.Hidden;
            UpdatePreview();
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

    }
}