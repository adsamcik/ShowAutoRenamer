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
        public static string regex;
        public static bool useFolder, smartRename, recursive, displayName, remove_, removeDash, insideInput;

        public static List<Show> shows = new List<Show>();
        public static string[] fileQueue;

        public static async Task<string[]> GetFilesInDirectory(string path) {
            path = Path.GetDirectoryName(path);
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f).Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

        public static string NameCleanup(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }

        public static string ConstructName(Episode e) {
            string preview = "";
            if (displayName) {
                if (!string.IsNullOrWhiteSpace(e.show.title))
                    preview += e.show.title + " ";
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
                        break;
                    }
                    else
                        nestedDepth--;
                }
                else if (result[i] == '{') nestedDepth++;
            }
            return result;
        }

        public async static Task<Show> PrepareShow(string[] refFile, string showName = "") {
            if (refFile.Length == 0) return null;
            Show show;
            string tempName = string.IsNullOrWhiteSpace(showName) ? GetShowName(refFile[0]) : showName;
            if (smartRename && !string.IsNullOrWhiteSpace(tempName)) {
                show = await Network.Search(tempName);
                if (show == null) return null;
            }
            else
                show = new Show(tempName);

            Debug.WriteLine("So far ok");

            for (int i = 0; i < refFile.Length; i++) {
                if (smartRename)
                    show.seasonList.Add(await PrepareSeasonNetwork(refFile[i], show));
                else
                    show.seasonList.Add(await PrepareSeason(refFile[i], show));
            }

            return show;
        }

        //TODO FIX 
        public static string GetShowName(string refFile) {
            refFile = Path.GetFileNameWithoutExtension(refFile);

            int season = GetSeason(refFile);
            int episode = GetEpisode(refFile);
            string n = SmartDotReplace(refFile);
            n = TestForEndings(n);
            if (Regex.Match(n, "s(\\d{1,2})e(\\d{1,2})", RegexOptions.IgnoreCase).Success) {
                return Regex.Split(n, "s(\\d{1,2})e(\\d{1,2})", RegexOptions.IgnoreCase)[0];
            }
            else if (Regex.Match(n, "(\\d{1,2})x(\\d{1,2})", RegexOptions.IgnoreCase).Success) {
                return Regex.Split(n, "(\\d{1,2})x(\\d{1,2})", RegexOptions.IgnoreCase)[0];
            }
            return "";
        }

        static async Task<Season> PrepareSeason(string refFile, Show show) {
            Season season = new Season(GetSeason(refFile), show);
            if (useFolder) {
                string[] files = await GetFilesInDirectory(refFile);

                for (int i = 0; i < files.Length; i++) {
                    Episode e;
                    e = PrepareEpisode(files[i], season);
                    if (e != null) season.episodeList.Add(e);
                }
            }
            else season.episodeList.Add(PrepareEpisode(refFile, season));

            return season;
        }

        static async Task<Season> PrepareSeasonNetwork(string refFile, Show show) {
            Season seasonReference = new Season(GetSeason(Path.GetFileNameWithoutExtension(refFile)), show);
            if (seasonReference == null) {
                Debug.WriteLine("season null");
                return await PrepareSeason(refFile, show);
            }
            else {
                Season season = new Season(seasonReference.season, show);
                if (useFolder) {
                    string[] files = await GetFilesInDirectory(refFile);

                    for (int i = 0; i < files.Length; i++) {
                        Episode e;
                        e = await PrepareEpisodeNetwork(files[i], seasonReference);
                        if (e != null) season.episodeList.Add(e);
                    }
                }
                else {
                    Episode e = await PrepareEpisodeNetwork(refFile, seasonReference);
                    Debug.WriteLine("episode");
                    if (e != null) {
                        season.episodeList.Add(e);
                        Debug.WriteLine(season.episodeList[season.episodeList.Count - 1].title);
                    }
                }


                return season;
            }
        }

        public async static Task<Episode> PrepareEpisodeNetwork(string filePath, Season s) {
            int episode = GetEpisode(Path.GetFileNameWithoutExtension(filePath));
            Episode e = await Network.GetEpisode(s.show, s.season, episode);
            if (e != null) {
                e.show = s.show;
                e.path = filePath;
            }
            return e;
        }

        public static Episode PrepareEpisode(string filePath, Season season) {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            int episode = GetEpisode(fileName);
            Debug.WriteLine("Episode " + episode);
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

            return new Episode(episodeName, season.season, episode, filePath, season.show);
        }



        public static async Task Rename(string showName) {
            Show s = await PrepareShow(fileQueue.ToArray(), showName);

            for (int i = 0; i < s.seasonList.Count; i++) {
                for (int y = 0; y < s.seasonList[i].episodeList.Count; y++) {
                    Episode e = s.seasonList[i].episodeList[y];
                    if (!File.Exists(e.path))
                        NotificationManager.AddNotification("File not found", "File not found at path:" + e.path + ". If the path is obviously incorrect, please report this as a bug.");
                    else {
                        string newPath = Path.GetDirectoryName(e.path) + "/" + ConstructName(e) + Path.GetExtension(e.path);
                        if (!File.Exists(newPath)) System.IO.File.Move(e.path, newPath);
                        else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
                    }

                }
            }
        }

        public static string BeautifyName(int s, int e, string fileName) {
            string season = (s < 10) ? "0" + s.ToString() : s.ToString();
            string episode = (e < 10) ? "0" + e.ToString() : e.ToString();
            string episodeName = "";
            string showName = "";
            fileName = SmartDotReplace(fileName);
            fileName = TestForEndings(fileName);
            fileName = fileName.Replace("(" + s + "x" + e + ")", "");
            string splitter = "S" + season + "E" + episode;
            if (fileName.ToUpper().Contains(splitter)) {
                string[] a = Regex.Split(fileName, splitter, RegexOptions.IgnoreCase);
                showName = a[0];
                episodeName = string.Join(" ", a.Skip((s == e) ? 2 : 1));
            }
            else if (fileName.ToUpper().Contains("X")) {
                string[] x = Regex.Split(fileName, "X", RegexOptions.IgnoreCase);
                int result;
                if ((x[0].Length > 0 && int.TryParse(x[0].Last().ToString(), out result)) && (x[1].Length > 0 && int.TryParse(x[1].First().ToString(), out result))) {
                    if (x[0].Length > 1 && int.TryParse(x[0].Substring(x[0].Length - 2, 2), out result)) x[0] = x[0].Remove(x[0].Length - 2, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[0].Substring(x[0].Length - 1, 1), out result)) x[0] = x[0].Remove(x[0].Length - 1, 1);

                    if (x[1].Length > 1 && int.TryParse(x[1].Substring(0, 2), out result)) x[1] = x[1].Remove(0, 2);
                    else if (x[0].Length > 0 && int.TryParse(x[1].Substring(0, 1), out result)) x[1] = x[1].Remove(0, 1);

                    episodeName = string.Join("", x);
                }
            }

            if (removeDash) fileName = Regex.Replace(fileName, "-", " ", RegexOptions.IgnoreCase);
            if (remove_) fileName = Regex.Replace(fileName, "_", " ", RegexOptions.IgnoreCase);

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

        /// <summary>
        /// Function to get Season or Episode number from string name
        /// </summary>
        /// <param name="n">File name</param>
        /// <param name="season">IsSeason</param>
        /// <returns>Value of Season or Episode</returns>
        public static Episode GetEpisodeFromName(string n) {
            int result = -1;
            Match m = Regex.Match(n, "s(\\d{1,2})e(\\d{1,2})", RegexOptions.IgnoreCase);
            if (m.Success) {
                int indexof = n.ToLower().IndexOf('e', m.Index);
                Episode e = new Episode();

                if (int.TryParse(n.Substring(m.Index + 1, indexof - m.Index - 1), out result))
                    e.season = result;
                if (int.TryParse(n.Substring(indexof + 1, m.Length - (indexof - m.Index) - 1), out result))
                    e.number = result;

                e.title = n.Substring(m.Index + m.Length, n.Length - m.Index - m.Length);
                indexof = e.title.LastIndexOf('.');
                if (indexof >= 0)
                    e.title = e.title.Substring(0, indexof);
                e.title = Regex.Replace(e.title, "^[\\.\\- ]*", "");

                e.show = new Show(n.Substring(0, m.Index));

                return e;
            }

            m = Regex.Match(n, "(\\d{1,2})x(\\d{1,2})", RegexOptions.IgnoreCase);
            if (m.Success) {
                int indexof = n.ToLower().IndexOf('x', m.Index);
                Episode e = new Episode();

                if (int.TryParse(n.Substring(m.Index, indexof - m.Index), out result))
                    e.season = result;
                if (int.TryParse(n.Substring(indexof + 1, m.Length - (indexof - m.Index) - 1), out result))
                    e.number = result;

                e.title = n.Substring(m.Index + m.Length, n.Length - m.Index - m.Length);
                indexof = e.title.LastIndexOf('.');
                if (indexof >= 0)
                    e.title = e.title.Substring(0, indexof);
                e.title = Regex.Replace(e.title, "^[.- ]*", "");
                e.show = new Show(n.Substring(0, m.Index));

                return e;
            }

            if (result == -1 && n.ToLower().Contains("pilot")) {
                string[] split = Regex.Split(n, "pilot", RegexOptions.IgnoreCase);
                return new Episode(split[0], 1, 0, new Show(Regex.Replace(split[1], "\\..*", "")));
            }
            else return null;
        }

        public static int GetSeason(string n) {
            return GetEpisodeFromName(n).season;
        }

        public static int GetEpisode(string n) {
            return GetEpisodeFromName(n).number;
        }

        public static string RegexTitle(string regex, Episode e) {
            regex = Regex.Replace(regex, "{title}", e.title, RegexOptions.IgnoreCase);
            regex = Regex.Replace(regex, "{showname}", e.show.title, RegexOptions.IgnoreCase);
            if (!DetectSubAddRegex(ref regex, "season", e.season))
                regex = Regex.Replace(regex, "{season}", e.season.ToString(), RegexOptions.IgnoreCase);
            if (!DetectSubAddRegex(ref regex, "episode", e.number))
                regex = Regex.Replace(regex, "{episode}", e.number.ToString(), RegexOptions.IgnoreCase);

            return regex;
        }


        public static bool DetectSubAddRegex(ref string text, string before, int beforeVal) {
            Match m;
            if ((m = Regex.Match(text, "{" + before)).Success) {
                if (text[m.Index + m.Length] == '-' || text[m.Index + m.Length] == '+') {
                    int startAt = m.Index + m.Length;
                    int index = text.IndexOf('}', startAt);
                    if (index >= 0) {
                        int result;
                        if (int.TryParse(text.Substring(startAt, index - startAt), out result)) {
                            text = Regex.Replace(text, "{" + before + "[\\+\\-0-9]+}", (beforeVal + result).ToString(), RegexOptions.IgnoreCase);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
