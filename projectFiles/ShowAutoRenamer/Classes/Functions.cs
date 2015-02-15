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
            path = Path.GetDirectoryName(path);
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f)
                                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

        //public static async Task<IList<Episode>> ProcessFilesInFolder(string[] files, string showName, int season) {
        //    List<Episode> foundEpisodes = new List<Episode>();
        //    List<Episode> episodes;

        //    if (smartRename) episodes = await Network.GetEpisodes(showName, season);
        //    else episodes = new List<Episode>();

        //    for (int i = 0; i < files.Length; i++) {
        //        string name = Path.GetFileNameWithoutExtension(files[i]);
        //        int s = GetSE(name, true);
        //        int e = GetSE(name, false);

        //        if (s == season) {
        //            if (smartRename) {
        //                if (e - 1 >= episodes.Count) NotificationManager.AddNotification(new Notification("Skipping " + name, "Smart-Rename didn't find episode " + e + " in season " + s + " for show " + showName));
        //                else name = ConstructName(episodes[i], showName);
        //            }
        //            else name = Functions.BeautifyName(name, s, e, showName);
        //            foundEpisodes.Add(new Episode(name, s, e, files[i]));
        //        }
        //        else NotificationManager.AddNotification(new Notification("Skipping " + files[i], "Season doesn't correspond with season of the selected file."));
        //    }

        //    for (int i = 0; i < foundEpisodes.Count - 1; i++) {
        //        for (int y = 0; y < foundEpisodes.Count; y++) {
        //            if (i != y && foundEpisodes[i].episode == foundEpisodes[y].episode) {
        //                NotificationManager.AddNotification(new Notification("Found duplicite episode (S" + foundEpisodes[i].season + "E" + foundEpisodes[i].episode + ")", foundEpisodes[i].path + " AND " + foundEpisodes[y].path));
        //            }
        //        }
        //    }

        //    return foundEpisodes;
        //}

        public static string NameCleanup(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }

        public static string ConstructName(Episode e, string showName = "") {
            string preview = "";
            if (displayName) {
                if (!string.IsNullOrWhiteSpace(showName))
                    preview += showName + " ";
                else if (!string.IsNullOrWhiteSpace(e.showName)) {
                    preview += e.showName + " ";          
                }
                else {
                    try {
                        string dir = Path.GetDirectoryName(e.path);
                        string dirName = dir.Split('\\').Last();
                        int result;
                        if ((dirName.Length > 1 && dirName.StartsWith("S") && int.TryParse(dirName.Substring(1), out result)) || dirName.StartsWith("Season"))
                            preview += Directory.GetParent(dir).Name + " ";
                        else
                            preview += dirName + " ";
                    }
                    catch { }
                }
            }

            preview += e.GetNameForFile();
            return NameCleanup(preview);
        }

        public async static Task<Show> PrepareShow(string refFile) {
            if (string.IsNullOrWhiteSpace(refFile)) return null;
            if (!File.Exists(refFile)) {
                refFile = Path.GetDirectoryName(refFile);
                refFile = Directory.GetFiles(refFile)[0];
            }
            Show show = new Show(GetShowName(refFile));
            //if(recursive) await RecursivePrepareSeason(refFile);
            show.seasonList.Add(await PrepareSeason(refFile));


            return show;
        }

        static string GetShowName(string refFile) {
            refFile = Path.GetFileNameWithoutExtension(refFile);

            int season = GetSE(refFile, true);
            int episode = GetSE(refFile, false);
            string showName = "";
            string n = SmartDotReplace(refFile);
            n = TestForEndings(n);
            n = n.Replace("(" + season + "x" + episode + ")", "");
            string splitter = "S" + season + "E" + episode;
            if (n.ToUpper().Contains(splitter)) {
                string[] a = Regex.Split(n, splitter, RegexOptions.IgnoreCase);
                showName = a[0];
            }
            return showName;
        }

        static async Task<Season> PrepareSeason(string refFile) {
            Season season = new Season(GetSE(refFile, true));
            if (useFolder) {
                string[] files = GetFilesInDirectory(refFile);

                for (int i = 0; i < files.Length; i++) {
                    Episode e = await PrepareEpisode(files[i], season.season);
                    if (e != null) season.episodeList.Add(e);
                }
            }
            else season.episodeList.Add(await PrepareEpisode(refFile, season.season));

            return season;
        }

        public static async Task<Episode> PrepareEpisode(string filePath, int season) {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            int episode = GetSE(fileName, false);
            string episodeName = "";
            string showName = "";
            string n = SmartDotReplace(fileName);
            n = TestForEndings(n);

            n = n.Replace("(" + season + "x" + episode + ")", "");
            string splitter = "S(\\d{1,2})E(\\d{1,2})";
            if (Regex.Match(n.ToUpper(), splitter).Success) {
                string[] a = Regex.Split(n, splitter, RegexOptions.IgnoreCase);
                episodeName = a[a.Length - 1];
                //episodeName = string.Join(" ", a.Skip((season == episode) ? 4 : 3));
                showName = a[0];
            }
            else if (n.ToUpper().Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if ((x[0].Length > 0 && int.TryParse(x[0].Last().ToString(), out result)) && (x[1].Length > 0 && int.TryParse(x[1].First().ToString(), out result))) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) x[0] = x[0].Remove(x[0].Length - 2, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) x[0] = x[0].Remove(x[0].Length - 1, 1);

                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) x[1] = x[1].Remove(0, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) x[1] = x[1].Remove(0, 1);

                    
                    episodeName = string.Join("", x);
                }
            }

            if (removeDash) n = Regex.Replace(n, "-", " ", RegexOptions.IgnoreCase);
            if (remove_) n = Regex.Replace(n, "_", " ", RegexOptions.IgnoreCase);

            //Trim unwanted characters at the beginning and end
            char[] trimChars = { ' ', '-' };
            showName = showName.Trim(trimChars);
            showName = showName.Trim();
            episodeName = episodeName.Trim(trimChars);

            return new Episode(episodeName, season, episode, filePath, showName);
        }

       

        public static async Task Rename(string path) {
            if (string.IsNullOrWhiteSpace(path)) return;

            Show s = await PrepareShow(path);
            for (int i = 0; i < s.seasonList.Count; i++) {
                for (int y = 0; y < s.seasonList[i].episodeList.Count; y++) {
                    Episode e = s.seasonList[i].episodeList[y];
                    if (!File.Exists(s.seasonList[i].episodeList[y].path)) {
                        NotificationManager.AddNotification("File not found", "File not found at path " + s.seasonList[i].episodeList[y].path + ". If the path is obviously incorrect, please report this as a bug.");
                        string newPath = Path.GetDirectoryName(e.path) + "/" + (displayName ? s.title + " " : "") + e.GetNameForFile() + Path.GetExtension(e.path);
                        if (!File.Exists(newPath)) System.IO.File.Move(e.path, newPath);
                        else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
                    }
                }
            }

            //return s.seasons[0].episodeList[0].title;

            //if (Functions.recursive && useFolder) await RecursiveFolderRename(path, showName);
            //else {
            //    string[] files;
            //    if (useFolder) files = Functions.GetFilesInDirectory(path);
            //    else {
            //        if (!File.Exists(path)) {
            //            NotificationManager.AddNotification(new Notification("File doesn't exists", "Are you sure you haven't renamed it already?"));
            //            return;
            //        }
            //        files = new string[] { path };
            //    }

            //    if (smartRename) await SmartRename(files);
            //    else await ClassicRename(files);
            //}
        }

        public static async Task ClassicRename(string refFile) {

            string name = Path.GetFileNameWithoutExtension(refFile);
            int season = GetSE(name, true);
            Show show = await PrepareShow(refFile);

            for (int i = 0; i < show.seasons; i++) {
                for (int y = 0; y < show.seasonList[i].episodeList.Count; y++) {
                    Episode e = show.seasonList[i].episodeList[y];


                    string newPath = Path.GetDirectoryName(e.path) + "/" + ConstructName(e,show.title) + Path.GetExtension(e.path);
                }

                //if (!preview && !File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                //else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }

        /// <summary>
        /// Smart rename uses internet sources to get episode names
        /// </summary>
        /// <param name="files">All files you want to rename</param>
        /// <param name="showName">Name of the show</param>
        /// <returns></returns>
        public static async Task SmartRename(Show show) {
            if (show.title == "") NotificationManager.AddNotification("Enter show name", "Please enter showname or uncheck Smart-Rename");
            show = await Network.Search(show.title);

            if (show.title == null) { NotificationManager.AddNotification("Show was not found", "Are you sure you typed in the correct name in english?"); return; }

            //Season season = await Network.GetSeason(show.title, GetSE(Path.GetFileNameWithoutExtension(files[0]), true));
            //if (season == null || season.season < 0) return;

            //IList<Episode> foundEpisodes = await ProcessFilesInFolder(files, name, season.season);

            //for (int i = 0; i < foundEpisodes.Count; i++) {
            //    string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);
            //    if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
            //    else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            //}
        }

        public static string BeautifyName(string n, int s, int e, string fileName) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();
            string episodeName = "";
            string showName = "";
            n = SmartDotReplace(fileName);
            n = TestForEndings(n);
            n = n.Replace("(" + s + "x" + e + ")", "");
            string splitter = "S" + season + "E" + episode;
            if (n.ToUpper().Contains(splitter)) {
                string[] a = Regex.Split(n, splitter, RegexOptions.IgnoreCase);
                showName = a[0];
                episodeName = string.Join(" ", a.Skip((s == e) ? 2 : 1));
            }
            else if (n.ToUpper().Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);
                int result;
                if ((x[0].Length > 0 && int.TryParse(x[0].Last().ToString(), out result)) && (x[1].Length > 0 && int.TryParse(x[1].First().ToString(), out result))) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) x[0] = x[0].Remove(x[0].Length - 2, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) x[0] = x[0].Remove(x[0].Length - 1, 1);

                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) x[1] = x[1].Remove(0, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) x[1] = x[1].Remove(0, 1);

                    episodeName = string.Join("", x);
                }
            }

            if (removeDash) n = Regex.Replace(n, "-", " ", RegexOptions.IgnoreCase);
            if (remove_) n = Regex.Replace(n, "_", " ", RegexOptions.IgnoreCase);

            //Trim unwanted characters at the beginning and end
            char[] trimChars = { ' ', '-' };
            showName = showName.Trim(trimChars);
            showName = showName.Trim();
            episodeName = episodeName.Trim(trimChars);

            string final = (displayName && showName != "") ? showName + " " : "";
            final += "S" + season + "E" + episode;
            final += (episodeName.Trim() != "") ? " - " + episodeName.Trim() : "";
            return NameCleanup(final);
        }


        public static string SmartDotReplace(string n) {
            string[] r = n.Split('.');
            n = "";
            for (int i = 0; i < r.Length; i++) {
                if (r[i].Length == 1) n += r[i] + ".";
                else n += r[i] + " ";
            }
            //n.Any(c => char.IsUpper(c));
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

            if (n.Contains("PILOT")) return 0;

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

            int result = -1;
            if (n.Contains("X")) {
                string[] x = Regex.Split(n, "X", RegexOptions.IgnoreCase);

                if ((x[0].Length > 0 && int.TryParse(x[0].Last().ToString(), out result)) && (x[1].Length > 0 && int.TryParse(x[1].First().ToString(), out result))) {
                    if (season) {
                        if (!((x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) || (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result))))
                            result = -1;
                    }
                    else {
                        if (!((x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) || (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result))))
                            result = -1;
                    }
                }
            }

            return result;
        }

        //public static async Task RecursiveFolderRename(string path, string showName) {
        //    if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);
        //    string[] directories = Directory.GetDirectories(path);

        //    for (int i = 0; i < directories.Length; i++) { await RecursiveFolderRename(directories[i], showName); }

        //    string[] files = Functions.GetFilesInDirectory(path);
        //    if (files.Length == 0) return;
        //    if (Functions.smartRename) await Functions.SmartRename(files, showName);
        //    else await Functions.ClassicRename(files);
        //}
    }
}
