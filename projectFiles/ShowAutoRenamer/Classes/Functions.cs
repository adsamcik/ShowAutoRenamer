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
        public static bool useFolder, smartRename, recursive;

        public static string[] GetFilesInDirectory(string path) {
            FileInfo[] fileList;
            DirectoryInfo directory = new DirectoryInfo(path);
            fileList = directory.GetFiles();

            var filtered = fileList.Select(f => f)
                                .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

            return filtered.Select(f => f.FullName).ToArray();
        }

        public static IList<Episode> ProcessFilesInFolder(string[] files, string show, int season) {
            List<Episode> foundEpisodes = new List<Episode>();
            List<Episode> episodes;
            if (smartRename) episodes = GetEpisodes(show, season);
            else episodes = new List<Episode>();

            for (int i = 0; i < files.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                int s = GetSE(name, true);
                int e = GetSE(name, false);



                if (s == season) {
                    if (smartRename) {
                        if (e - 1 >= episodes.Count) nm.AddNotification(new Notification("Skipping " + name, "Smart-Rename didn't find episode " + e + " in season " + s + " for show " + show));
                        else name = CreateFileName(episodes[e - 1].title, s, e);
                    }
                    else { Debug.WriteLine("!" + name); name = CreateFileName(FormatName(name, s, e), s, e); }
                    foundEpisodes.Add(new Episode(name, s, e, files[i]));
                }
                else nm.AddNotification(new Notification("Skipping " + files[i], "Season doesn't correspond with season of the selected file."));
            }

            for (int i = 0; i < foundEpisodes.Count - 1; i++) {
                for (int y = 0; y < foundEpisodes.Count; y++) {
                    if (i != y && foundEpisodes[i].episode == foundEpisodes[y].episode) {
                        nm.AddNotification(new Notification("Found duplicite episode (S" + foundEpisodes[i].season + "E" + foundEpisodes[i].episode + ")", foundEpisodes[i].path + " AND " + foundEpisodes[y].path));
                        if (++i > foundEpisodes.Count) break;
                    }
                }
            }

            return foundEpisodes;
        }

        public static void RecursiveFolderRenamer(string path) {
            if (!Directory.Exists(path)) path = Path.GetDirectoryName(path);
            string[] directories = Directory.GetDirectories(path);

            for (int i = 0; i < directories.Length; i++) { RecursiveFolderRenamer(directories[i]); }

            string[] files = Functions.GetFilesInDirectory(path);
            if (files.Length == 0) return;
            if (smartRename) SmartRename(files);
            else ClassicRename(files);
        }

        public static string normalizeName(string name) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            return name;
        }
    }
}
