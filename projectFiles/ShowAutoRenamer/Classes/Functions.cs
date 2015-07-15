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
        public static bool useFolder, smartRename, recursive, displayName, remove_, removeDash, insideInput;

        public static List<Show> shows;

        public static async Task<string[]> GetFilesInDirectory(string path) {
            path = Path.GetDirectoryName(path);
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f)
                                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

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
                    preview += showName.Trim() + " ";
                else if (!string.IsNullOrWhiteSpace(e.showName)) {
                    preview += e.showName.Trim() + " ";
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

        public static string CutFromJson(string source, string lookForObjectName) {
            string result = source.Substring(source.IndexOf(@"""" + lookForObjectName + @""":{") + lookForObjectName.Length + 3);
            int nestedDepth = 0;
            for (int i = 0; i < result.Length; i++) {
                if (result[i] == '}') {
                    if (nestedDepth == 0) {
                        result = result.Substring(0, i);
                        Debug.WriteLine("found");
                        break;
                    }
                    else
                        nestedDepth--;
                }
                else if (result[i] == '{') nestedDepth++;
            }
            return result;
        }

        public async static Task<Show> PrepareShow(string refFile, string showName = "") {
            if (string.IsNullOrWhiteSpace(refFile)) return null;
            if (!File.Exists(refFile)) {
                refFile = Path.GetDirectoryName(refFile);
                refFile = Directory.GetFiles(refFile)[0];
            }

            Show show;

            if (smartRename) {
                string tempName = string.IsNullOrWhiteSpace(showName) ? GetShowName(refFile) : showName;
                if (!string.IsNullOrWhiteSpace(tempName))
                    show = await Network.Search(tempName);
                else
                    show = new Show(GetShowName(refFile));

                if (show == null) { return null; }
                show.seasonList.Add(await PrepareSeasonNetwork(refFile, show));
            }
            else {
                show = new Show((string.IsNullOrWhiteSpace(showName)) ? GetShowName(refFile) : showName);
                show.seasonList.Add(await PrepareSeason(refFile));
            }
            //if(recursive) await RecursivePrepareSeason(refFile);


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
            string splitter = "S(\\d{1,2})E(\\d{1,2})";
            if (Regex.Match(n.ToUpper(), splitter).Success) {
                string[] a = Regex.Split(n, splitter, RegexOptions.IgnoreCase);
                showName = a[0];
            }
            return showName;
        }

        static async Task<Season> PrepareSeason(string refFile) {
            Season season = new Season(GetSE(refFile, true));
            if (useFolder) {
                string[] files = await GetFilesInDirectory(refFile);

                for (int i = 0; i < files.Length; i++) {
                    Episode e;
                    e = PrepareEpisode(files[i], season.season);
                    if (e != null) season.episodeList.Add(e);
                }
            }
            else season.episodeList.Add(PrepareEpisode(refFile, season.season));

            return season;
        }

        static async Task<Season> PrepareSeasonNetwork(string refFile, Show show) {
            Season seasonReference;
            if ((seasonReference = await Network.GetSeason(show.title, GetSE(Path.GetFileNameWithoutExtension(refFile), true))) == null)
                return await PrepareSeason(refFile);
            else {
                Season season = new Season(seasonReference.season);
                season.episodes = seasonReference.episodes;
                season.url = seasonReference.url;
                if (useFolder) {
                    string[] files = await GetFilesInDirectory(refFile);

                    for (int i = 0; i < files.Length; i++) {
                        Episode e;
                        e = PrepareEpisodeNetwork(files[i], seasonReference);
                        if (e != null) season.episodeList.Add(e);
                    }
                }
                else {
                    season.episodeList.Add(PrepareEpisodeNetwork(refFile, seasonReference));
                }


                return season;
            }
        }

        public static Episode PrepareEpisodeNetwork(string filePath, Season s) {
            int episode = GetSE(Path.GetFileNameWithoutExtension(filePath), false);
            Episode e = s.episodeList.FirstOrDefault(x => x.episode == episode);
            if (e == null) return PrepareEpisode(filePath, s.season);
            return new Episode(e.title, e.season, e.episode, filePath, e.showName);
        }

        public static Episode PrepareEpisode(string filePath, int season) {
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



        public static async Task Rename(string path, string showName) {
            if (string.IsNullOrWhiteSpace(path)) return;

            Show s = await PrepareShow(path, showName);

            for (int i = 0; i < s.seasonList.Count; i++) {
                for (int y = 0; y < s.seasonList[i].episodeList.Count; y++) {
                    Episode e = s.seasonList[i].episodeList[y];
                    if (!File.Exists(e.path))
                        NotificationManager.AddNotification("File not found", "File not found at path " + e.path + ". If the path is obviously incorrect, please report this as a bug.");
                    else {
                        string newPath = Path.GetDirectoryName(e.path) + "/" + ConstructName(e, s.title) + Path.GetExtension(e.path);
                        if (!File.Exists(newPath)) System.IO.File.Move(e.path, newPath);
                        else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
                    }

                }
            }
        }

        public static async Task ClassicRename(string refFile) {

            string name = Path.GetFileNameWithoutExtension(refFile);
            int season = GetSE(name, true);
            Show show = await PrepareShow(refFile);

            for (int i = 0; i < show.seasons; i++) {
                for (int y = 0; y < show.seasonList[i].episodeList.Count; y++) {
                    Episode e = show.seasonList[i].episodeList[y];


                    string newPath = Path.GetDirectoryName(e.path) + "/" + ConstructName(e, show.title) + Path.GetExtension(e.path);
                }

                //if (!preview && !File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                //else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
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
    }
}
