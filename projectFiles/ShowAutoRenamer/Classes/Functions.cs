using ShowAutoRenamer.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public static class Functions {
        public static bool smartRename, remove_, removeDash;

        public static List<Show> shows = new List<Show>();

        public static System.Windows.Controls.ListBox listBox;
        public static EpisodeFile[] fileQueue;


        public static void RenameInQueue(string path, string newName) {
            for (int i = 0; i < fileQueue.Length; i++) {
                if (fileQueue[i].path == path) {
                    fileQueue[i].path = newName;
                    if (listBox != null)
                        listBox.Items.Refresh();
                    break;
                }
            }
        }

        public async static Task<Show> PrepareShow(EpisodeFile[] refFile, string showName = "") {
            if (refFile.Length == 0) return null;
            Show show;
            string tempName = string.IsNullOrWhiteSpace(showName) ? GetEpisodeFromName(refFile[0].path).show.title : showName;
            if (smartRename && !string.IsNullOrWhiteSpace(tempName)) {
                show = await Network.Search(tempName);
                if (show == null) return null;
            }
            else
                show = new Show(tempName);

            for (int i = 0; i < refFile.Length; i++) {
                if (smartRename) {
                    Season s = await PrepareSeasonNetwork(refFile[i].path, show);
                    if (s != null)
                        show.seasonList.Add(s);
                }
                else {
                    Season s = PrepareSeason(refFile[i].path, show);
                    if (s != null)
                        show.seasonList.Add(s);
                }
            }

            return show;
        }

        static Season PrepareSeason(string refFile, Show show) {
            Episode e = GetEpisodeFromName(refFile);
            bool found = true;
            Season season = show.seasonList.First(x => x.season == e.season);
            if (season == null) {
                found = false;
                season = new Season(e.season, show);
            }

            season.episodeList.Add(e);
            return found ? null : season;
        }

        static async Task<Season> PrepareSeasonNetwork(string refFile, Show show) {
            Season season;
            bool found = true;
            int number = GetSeason(Path.GetFileNameWithoutExtension(refFile)) + RenameData.seasonAdd;
            if (show.seasonList.Count == 0 || (season = show.seasonList.First(x => x.season == number)) == null) {
                found = false;
                season = new Season(number, show);
            }

            Episode e = await PrepareEpisodeNetwork(refFile, season);
            if (e != null)
                season.episodeList.Add(e);

            return found ? null : season;
        }

        public async static Task<Episode> PrepareEpisodeNetwork(string filePath, Season s) {
            int episode = GetEpisode(Path.GetFileNameWithoutExtension(filePath)) + RenameData.episodeAdd;
            Episode e = await Network.GetEpisode(s.show, s.season, episode);
            if (e != null) {
                e.show = s.show;
                e.path = filePath;
            }
            return e;
        }

        public static async Task Rename(string showName) {
            Show s = await PrepareShow(fileQueue.ToArray(), showName);

            for (int i = 0; i < s.seasonList.Count; i++) {
                for (int y = 0; y < s.seasonList[i].episodeList.Count; y++) {
                    Episode e = s.seasonList[i].episodeList[y];
                    if (!File.Exists(e.path))
                        NotificationManager.AddNotification("File not found", "File not found at path:" + e.path + ". If the path is obviously incorrect, please report this as a bug.");
                    else {
                        string newPath = Path.GetDirectoryName(e.path) + "/" + RegexTitle(RenameData.regex, s.seasonList[i].episodeList[y]) + Path.GetExtension(e.path);
                        if (!File.Exists(newPath)) {
                            File.Move(e.path, newPath);
                            RenameInQueue(e.path, newPath);
                            e.path = newPath;
                        }
                        else NotificationManager.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
                    }

                }
            }
        }

        public static string SmartDotReplace(string n) {
            string[] r = n.Split('.');
            n = "";
            for (int i = 0; i < r.Length; i++) {
                if (r[i].Length == 1) n += r[i] + ".";
                else n += r[i] + " ";
            }
            return n;
        }

        public static string TestForEndings(string n) {
            Match m;
            string[] splitters = new string[] { "1080P", "720P", "DVD", "PROPER", "REPACK", "DVB", "CZ", "EN", "ENG", "HDTV", "HD", "webrip" };

            for (int i = 0; i < splitters.Length; i++) {
                m = Regex.Match(n, splitters[i], RegexOptions.IgnoreCase);
                if (m.Success)
                    n = n.Substring(0, m.Index);
            }
            return n;
        }

        /// <summary>
        /// Function to get Season or Episode number from string name
        /// </summary>
        /// <param name="n">File name</param>
        /// <param name="season">IsSeason</param>
        /// <returns>Value of Season or Episode</returns>
        public static Episode GetEpisodeFromName(string n) {
            n = Path.GetFileNameWithoutExtension(n);
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
                e.title = TestForEndings(SmartDotReplace(Regex.Replace(e.title, "^[\\.\\- ]*", ""))).Trim();

                e.show = new Show(SmartDotReplace(n.Substring(0, m.Index)).Trim());

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
                e.title = TestForEndings(SmartDotReplace(Regex.Replace(e.title, "^[.- ]*", ""))).Trim();
                e.show = new Show(n.Substring(0, m.Index).Trim());

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
            regex = Regex.Replace(regex, "{title}", NameCleanup(e.title), RegexOptions.IgnoreCase);
            regex = Regex.Replace(regex, "{showname}", NameCleanup(e.show.title), RegexOptions.IgnoreCase);
            if (!DetectSubAddRegex(ref regex, "season", e.season))
                regex = ResolveZeroFormat("season", regex, e.season);
            if (!DetectSubAddRegex(ref regex, "episode", e.number))
                regex = ResolveZeroFormat("episode", regex, e.number);

            return regex;
        }


        public static bool DetectSubAddRegex(ref string text, string before, int beforeVal) {
            Match m;
            if ((m = Regex.Match(text, "{.?" + before + ".*?}")).Success) {
                int startIndex = 1 + (text[m.Index + 1] == '0' ? 1 : 0) + m.Index + before.Length;
                if (startIndex < text.Length && (text[startIndex] == '-' || text[startIndex] == '+')) {
                    int startAt = startIndex + 1;
                    int index = text.IndexOf('}', startAt);
                    if (index >= 0) {
                        int result;
                        if (int.TryParse(text.Substring(startAt, index - startAt), out result)) {
                            text = ResolveZeroFormat(before, text, beforeVal);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static string ToString(this int source, int MinCharacterCount) {
            StringBuilder sb = new StringBuilder();
            int length = source.ToString().Length;
            for (int i = length; i < MinCharacterCount; i++)
                sb.Append('0');
            sb.Append(source.ToString());
            return sb.ToString();
        }

        public static string ResolveZeroFormat(string regexName, string input, int value) {
            regexName = "{.?" + regexName;
            Match m;
            if ((m = Regex.Match(input, regexName)).Success) {
                bool isZero = input[m.Index + 1] == '0';
                return Regex.Replace(input, regexName + ".*?}", value.ToString(isZero ? 2 : 1), RegexOptions.IgnoreCase);
            }
            return input;
        }

        public static string NameCleanup(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }
    }
}
