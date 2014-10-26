using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

namespace ShowAutoRenamer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
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
            if(Rename()) MessageBox.Show("Successfully renamed");
            else MessageBox.Show("There was an error, nothing was renamed");
        }

        bool Rename() {
            string[] files;
            string path = Path.GetDirectoryName(textBox.Text);
            if (!Directory.Exists(path)) return false;
            if ((bool)useFolder.IsChecked) files = Directory.GetFiles(path);
            else files = new string[] {textBox.Text};
            if(files.Length < 1) return false;

            int season = Math.Abs(GetSeason(Path.GetFileNameWithoutExtension(files[0])));
            for (int i = 0; i < files.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                int episode = GetEpisode(name);

                name = FormatName(name, season, episode);
                string newPath = Path.GetDirectoryName(files[i]) + "/" + name + Path.GetExtension(files[i]);
                if(!File.Exists(newPath)) System.IO.File.Move(files[i], newPath);

            }

            if (season == -1) return false;
            return true;
        }

        string FormatName(string n, int s, int e) {
            n.Replace('.', ' ');
            string ep = (e < 10) ? "E0" + e.ToString() : "E" + e.ToString();
            if (n.ToUpper().Contains(ep)) n = Regex.Split(n, ep, RegexOptions.IgnoreCase)[1];
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
            

            //while (n.ToUpper().Contains(ep)) n = Regex.Replace(n, ep, "", RegexOptions.IgnoreCase);
            n = TestForEndings(n);

            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();
            if (n.Contains(season + "x" + episode)) n = n.Replace(season + "x" + episode, "");

            n = n.Replace('.', ' ');

            if ((bool)removeMinus.IsChecked) n = Regex.Replace(n, "-", " ", RegexOptions.IgnoreCase);
            if ((bool)remove_.IsChecked) n = Regex.Replace(n, "_", " ", RegexOptions.IgnoreCase);
            if ((bool)removeName.IsChecked && showName.Text.Trim() != "") n = Regex.Replace(n, showName.Text, " ", RegexOptions.IgnoreCase);

            n = Regex.Replace(n, @"\s+", " ");

            while (true) {
                bool changed = false;
                n = n.Trim();
                if (n.StartsWith("-")) { n = n.TrimStart('-'); changed = true; }

                if (!changed) break;
            }

            //Future feature - if name is null, it will ask you to fill that name
            //if (n == "") {
            //    EnterName edit = new EnterName(n, season, episode);
            //    if ((bool)edit.ShowDialog()) {
            //        n = edit.Name;
            //        season = edit.Season;
            //        episode = edit.Episode;
            //    }
            //}

            string final = (n.Length == 0) ? "S" + season + "E" + episode : "S" + season + "E" + episode + " - " + n;

            final = (showName.Text.Trim() != "") ? showName.Text + " " + final : final;
            return final;
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
                //Debug.WriteLine("split0 " + split[split.Length - 2] + "split1 " + split[split.Length - 1]);
                if ((split[split.Length - 2].EndsWith(" ") || split[split.Length - 2].EndsWith(".") || split[split.Length - 2].Length < 1) &&
                    (split[split.Length - 1].StartsWith(" ") || split[split.Length - 1].StartsWith(".") || split[split.Length - 1].Length < 1)) 
                {
                    if (who.Length - 3 > split[split.Length - 1].Length) who = who.Remove(who.Length - 3 - split[split.Length - 1].Length);
                    Contains(who, what);
                }
            }

            who = who.Trim();
            if (who.EndsWith("-")) who.Remove(who.Length - 2);

            return who;
        }

        bool ContainsFromEnd(string who, string what, int index = 0) {
            if (who.Length < what.Length) return false;

            if (who.Length - index - 2 < 0) return false;
            if (who.Substring(who.Length - index - 2, 2) == what) return true;
            else ContainsFromEnd(who, what, index++);
            return false;
        }

        int GetSeason(string n) {
            n = n.ToUpper();
            if (n.Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if (int.TryParse(x[0].Last().ToString(), out result) && int.TryParse(x[1].First().ToString(), out result)) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result));
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result));
                    else result = -1;
                    return result;
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (n.Contains("S" + i + y)) return i * 10 + y;
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (i == 0 && n.Contains("S" + y)) return y;
                }
            }

            return -1;
        }

        int GetEpisode(string n) {
            n = n.ToUpper();
            if (n.Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if (int.TryParse(x[0].Last().ToString(), out result) && int.TryParse(x[1].First().ToString(), out result)) {
                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) ;
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) ;
                    else result = -1;
                    return result;
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (n.Contains("E" + i + y)) return i * 10 + y;
                }
            }

            for (int i = 0; i < 10; i++) {
                for (int y = 0; y < 10; y++) {
                    if (i == 0 && n.Contains("E" + y)) return y;
                }
            }

            return -1;
        }

        private void removeMinus_Click(object sender, RoutedEventArgs e) {
            UpdatePreview();
        }

        void UpdatePreview() {
            if (!File.Exists(textBox.Text)) { Preview.Content = "Path doesn't exist"; return; }
            string path = Directory.GetFiles(Path.GetDirectoryName(textBox.Text))[0];
            string name = Path.GetFileNameWithoutExtension(path);
            Preview.Content = FormatName(name, GetSeason(name), GetEpisode(name));
        }

        private void showName_TextChanged(object sender, TextChangedEventArgs e) {
            UpdatePreview();
        }


    }
}
