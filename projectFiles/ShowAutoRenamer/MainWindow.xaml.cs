﻿using System;
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
        NotificationManager nm;

        public MainWindow() {
            InitializeComponent();

            nm = new NotificationManager(notification, nTitle, nText);
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
            Rename();
        }

        void RecursiveFolderRenamer(string path) {
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);
            string[] directories = Directory.GetDirectories(path);

            for (int i = 0; i < directories.Length; i++) {RecursiveFolderRenamer(directories[i]); }

            string[] files = GetFilesInDirectory(path);
            if (files.Length == 0) return;
            if ((bool)smartRename.IsChecked) SmartRename(files);
            else ClassicRename(files);
        }

        void Rename() {
            string path;
            if (Directory.Exists(filePath.Text)) path = filePath.Text;
            else path = Path.GetDirectoryName(filePath.Text);
            if (!Directory.Exists(path)) return;

            if ((bool)recursive.IsChecked && (bool)useFolder.IsChecked) RecursiveFolderRenamer(path);
            else {
                string[] files;
                if ((bool)useFolder.IsChecked) files = GetFilesInDirectory(path);
                else files = new string[] { filePath.Text };

                if ((bool)smartRename.IsChecked) SmartRename(files);
                else ClassicRename(files);
            }
        }

        string[] GetFilesInDirectory(string path) {
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f)
                                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

        void ClassicRename(string[] files) {
            string name = Path.GetFileNameWithoutExtension(files[0]);
            int season = GetSE(name, true);
            List<Episode> foundEpisodes = ProcessFilesInFolder(files, name, season);

            for (int i = 0; i < foundEpisodes.Count; i++) {
                string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);

                if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                else nm.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }

        void SmartRename(string[] files) {
            if (showName.Text == "") nm.AddNotification("Enter show name", "Please enter showname or uncheck Smart-Rename");
            string name = Search(showName.Text).Title;

            Season season = GetSeason(name, GetSE(Path.GetFileNameWithoutExtension(files[0]), true));
            if (season == null || season.season < 0) return;
            
            List<Episode> foundEpisodes = ProcessFilesInFolder(files, name, season.season);

            for (int i = 0; i < foundEpisodes.Count; i++) {
                string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);
                if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                else nm.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }

        List<Episode> ProcessFilesInFolder(string[] files, string show, int season) {
            List<Episode> foundEpisodes = new List<Episode>();
            List<Episode> episodes;
            if ((bool)smartRename.IsChecked) episodes = GetEpisodes(show, season);
            else episodes = new List<Episode>();

            for (int i = 0; i < files.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                int s = GetSE(name, true);
                int e = GetSE(name, false);

                

                if (s == season) {
                    if ((bool)smartRename.IsChecked) {
                        if (e-1 >= episodes.Count) nm.AddNotification(new Notification("Skipping " + name, "Smart-Rename didn't find episode " + e + " in season " + s + " for show " + show));
                        else name = CreateFileName(episodes[e-1].title,s, e);
                    }
                    else name = CreateFileName(FormatName(name, s, e), s, e);
                    foundEpisodes.Add(new Episode(name, s, e, files[i]));
                }
                else nm.AddNotification(new Notification("Skipping " + files[i], "Season doesn't correspond with season of the selected file."));
            }

            for (int i = 0; i < foundEpisodes.Count - 1; i++) {
                for (int y = 0; y < foundEpisodes.Count; y++) {
                    if (i != y && foundEpisodes[i].episode == foundEpisodes[y].episode) {
                        nm.AddNotification(new Notification("Found duplicite episode (S" + foundEpisodes[i].season + "E" + foundEpisodes[i].episode + ")", foundEpisodes[i].path + " AND " + foundEpisodes[y].path));
                        if(++i > foundEpisodes.Count) break;
                    }
                }
            }

                return foundEpisodes;
        }

        string normalizeName(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }

        string Request(string adress) {
            adress = adress.Replace(" ", "-");
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(adress);
            // Get the response.
            try {
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
            catch {
                nm.AddNotification(new Notification("The sky is falling!", "There was an error with request. If you encounter this often, please report it with show name you typed in", true));
                Debug.WriteLine(adress);
                return "";
            }

            
        }


        Show Search(string forWhat) {
            List<Show> episodes = JsonConvert.DeserializeObject<List<Show>>(Request("http://api.trakt.tv/search/shows.json/c01ff5475f1333863127ffd8816fb776?query=" + forWhat));
            nm.DeleteSearchRelated();
            if (episodes.Count == 0) return new Show();
            return episodes[0];
        }

        List<Episode> GetEpisodes(string showName, int season) {
            string title = showName.Replace(" ", "-").Replace("(","").Replace(")","").Replace("&","and").Replace(".","").Replace("'","");
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
            string title = showName;
            if (title == null) return new Episode("Show not found!");
            title = title.Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("&", "and").Replace(".", "").Replace("'", "");
            List<Episode> episodes = JsonConvert.DeserializeObject<List<Episode>>(Request("http://api.trakt.tv/show/season.json/c01ff5475f1333863127ffd8816fb776/" + title + "/" + season));
            if (episodes == null) return new Episode("Show not found");
            else if (episodes.Count == 0) return new Episode();
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
            if (n == null) return "";
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();

            string final = (n.Length == 0) ? "S" + season + "E" + episode : "S" + season + "E" + episode + " - " + n;
            final = ((bool)displayName.IsChecked && showName.Text.Trim() != "") ? showName.Text + " " + final : final;


            return normalizeName(final);
        }

        string FormatName(string n, int s, int e) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();

            n = n.Replace("(" + s + "x" + e + ")", "");
            if (n.ToUpper().Contains(episode)) n = Regex.Split(n, episode, RegexOptions.IgnoreCase)[1];
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
                if (n.EndsWith("-")) { n = n.TrimEnd('-'); changed = true; }

                if (!changed) break;
            }
            return n;
        }

        string TestForEndings(string n) {

            string[] splitters = new string[] { "1080P", "720P", "DVD", "PROPER", "REPACK", "DVB", "CZ", "EN", "ENG", "HDTV", "HD" };

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
                    if (who.Length - what.Length > split[split.Length - 1].Length) who = who.Remove(who.Length - what.Length - split[split.Length - 1].Length);
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
                        if (!((x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) || (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)))) result = 1;
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
            UpdatePreview();
        }

        void UpdatePreview() {
            if (!File.Exists(filePath.Text)) { Preview.Content = "Path doesn't exist"; return; }
            string name = Path.GetFileNameWithoutExtension(filePath.Text);
            int s = GetSE(name, true);
            int e = GetSE(name, false);

            if ((bool)smartRename.IsChecked) {
                nm.DeleteSearchRelated();
                if (showName.Text == "") { nm.AddNotification(new Notification("Enter show name", "Please enter showname or uncheck Smart-Rename", true)); return; }
                string searched = Search(showName.Text).Title;

                if (searched == "") { nm.AddNotification(new Notification("Couldn't find your show", "Unable to find show you searched for (" + showName.Text + ")", true)); return; }
                else nm.AddNotification(new Notification("Found " + searched, "Smart-Rename will use this show to rename your files", true));

                Preview.Content = CreateFileName(GetEpisode(searched, s, e).title, GetSE(name, true), GetSE(name, false));
                
            }
            else {
                nm.DeleteSearchRelated();

                Debug.WriteLine(ProcessFilesInFolder(new string[] { filePath.Text }, name, s)[0].title);
                Preview.Content = ProcessFilesInFolder(new string[] { filePath.Text }, name, s)[0].title;
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
            if (timeLeft < 0) { dispatcherTimer.Stop(); UpdatePreview(); timeLeft = 0;}
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
            nm.DeleteSearchRelated();
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
            nm.RemoveNotification();
        }

    }
}