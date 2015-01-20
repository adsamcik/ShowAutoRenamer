using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public static class Functions {
        public static bool useFolder, smartRename, recursive, displayName, remove_, removeDash;

        public static string[] GetFilesInDirectory(string path) {
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f)
                                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

        public static async Task<IList<Episode>> ProcessFilesInFolder(string[] files, string showName, int season) {
            List<Episode> foundEpisodes = new List<Episode>();
            List<Episode> episodes;
            if (smartRename) episodes = await Network.GetEpisodes(showName, season);
            else episodes = new List<Episode>();

            for (int i = 0; i < files.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                int s = GetSE(name, true);
                int e = GetSE(name, false);



                if (s == season) {
                    if (smartRename) {
                        if (e - 1 >= episodes.Count) NotificationManager.AddNotification(new Notification("Skipping " + name, "Smart-Rename didn't find episode " + e + " in season " + s + " for show " + showName));
                        else name = CreateFileName(episodes[e - 1].title, s, e, showName);
                    }
                    else { name = CreateFileName(Functions.FormatName(name, s, e, showName), s, e, showName); }
                    foundEpisodes.Add(new Episode(name, s, e, files[i]));
                }
                else NotificationManager.AddNotification(new Notification("Skipping " + files[i], "Season doesn't correspond with season of the selected file."));
            }

            for (int i = 0; i < foundEpisodes.Count - 1; i++) {
                for (int y = 0; y < foundEpisodes.Count; y++) {
                    if (i != y && foundEpisodes[i].episode == foundEpisodes[y].episode) {
                        NotificationManager.AddNotification(new Notification("Found duplicite episode (S" + foundEpisodes[i].season + "E" + foundEpisodes[i].episode + ")", foundEpisodes[i].path + " AND " + foundEpisodes[y].path));
                    }
                }
            }

            return foundEpisodes;
        }

        public static string normalizeName(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }

        public static string CreateFileName(string n, int s, int e, string showName) {
            Debug.WriteLine("heh");
            if (n == null) return "";
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();



            string final = (n.Length == 0) ? "S" + season + "E" + episode : "S" + season + "E" + episode + " - " + n;
            final = (displayName && showName.Trim() != "") ? showName + " " + final : final;

            return normalizeName(final);
        }

        public static async Task Rename(string filePath, string showName) {
            string path;
            if (Directory.Exists(filePath)) path = filePath;
            else path = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(path)) return;

            if (Functions.recursive && Functions.useFolder) await RecursiveFolderRename(path, showName);
            else {
                string[] files;
                if (Functions.useFolder) files = Functions.GetFilesInDirectory(path);
                else {
                    if (!File.Exists(filePath)) {
                        NotificationManager.AddNotification(new Notification("File doesn't exists", "Are you sure you haven't renamed it already?"));
                        return;
                    }
                    files = new string[] { filePath };
                }

                if (smartRename) await SmartRename(files, showName);
                else await ClassicRename(files);
            }
        }

        public static async Task ClassicRename(string[] files) {
            string name = Path.GetFileNameWithoutExtension(files[0]);
            int season = GetSE(name, true);
            IList<Episode> foundEpisodes = await ProcessFilesInFolder(files, name, season);

            for (int i = 0; i < foundEpisodes.Count; i++) {
                string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);

                if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }

        public static async Task SmartRename(string[] files, string showName) {
            if (showName == "") NotificationManager.AddNotification("Enter show name", "Please enter showname or uncheck Smart-Rename");
            string name = (await Network.Search(showName)).Title;

            if (name == null) { NotificationManager.AddNotification("Show was not found", "Are you sure you typed in the correct name in english?"); return; }

            Season season = await Network.GetSeason(name, GetSE(Path.GetFileNameWithoutExtension(files[0]), true));
            if (season == null || season.season < 0) return;

            IList<Episode> foundEpisodes = await ProcessFilesInFolder(files, name, season.season);

            for (int i = 0; i < foundEpisodes.Count; i++) {
                string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);
                if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }

        public static string FormatName(string n, int s, int e, string showName) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();
            Debug.WriteLine(n + " after " + n.Replace("(" + s + "x" + e + ")", ""));
            n = n.Replace("(" + s + "x" + e + ")", "");
            Debug.WriteLine(n);
            if (n.ToUpper().Contains(episode)) {
                string[] a = Regex.Split(n, episode, RegexOptions.IgnoreCase);
                //a[0] = "";
                n = string.Join(" ", a.Skip((s == e) ? 2 : 1));

                Debug.WriteLine(n);
            }
            else if (n.ToUpper().Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if (int.TryParse(x[0].Last().ToString(), out result) && int.TryParse(x[1].First().ToString(), out result)) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) x[0] = x[0].Remove(x[0].Length - 2, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) x[0] = x[0].Remove(x[0].Length - 1, 1);

                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) x[1] = x[1].Remove(0, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) x[1] = x[1].Remove(0, 1);

                    n = string.Join("", x);
                }
            }
            n = TestForEndings(n);
            n = n.Replace('.', ' ');
            if (removeDash) n = Regex.Replace(n, "-", " ", RegexOptions.IgnoreCase);
            if (remove_) n = Regex.Replace(n, "_", " ", RegexOptions.IgnoreCase);
            if (displayName && showName.Trim() != "") n = Regex.Replace(n, showName, " ", RegexOptions.IgnoreCase);

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

        public static string TestForEndings(string n) {

            string[] splitters = new string[] { "1080P", "720P", "DVD", "PROPER", "REPACK", "DVB", "CZ", "EN", "ENG", "HDTV", "HD" };

            for (int i = 0; i < splitters.Length; i++) {
                n = Contains(n, splitters[i]);
            }
            return n;
        }

        public static string Contains(this string who, string what) {
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

        public static int GetSE(string n, bool season) {
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

            if (!season) {
                int result;
                if (int.TryParse(n.Substring(0, 2), out result) || int.TryParse(n.Substring(0, 1), out result)) return result;
            }

            return 1;
        }
        public static async Task RecursiveFolderRename(string path, string showName) {
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);
            string[] directories = Directory.GetDirectories(path);

            for (int i = 0; i < directories.Length; i++) { await RecursiveFolderRename(directories[i], showName); }

            string[] files = Functions.GetFilesInDirectory(path);
            if (files.Length == 0) return;
            if (Functions.smartRename) await Functions.SmartRename(files, showName);
            else Functions.ClassicRename(files);
        }
    }
}
