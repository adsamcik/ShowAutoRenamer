using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    class Season {
#pragma warning disable 0649
        public int season;
#pragma warning disable 0649
        public int episodes;
#pragma warning disable 0649
        public string url;

        public IList<Episode> episodeList = new List<Episode>();

        void Rename() {
            string path;
            if (Directory.Exists(filePath.Text)) path = filePath.Text;
            else path = Path.GetDirectoryName(filePath.Text);
            if (!Directory.Exists(path)) return;

            if (Functions.recursive && Functions.useFolder) RecursiveFolderRenamer(path);
            else {
                string[] files;
                if (Functions.useFolder) files = Functions.GetFilesInDirectory(path);
                else {
                    if (!File.Exists(filePath.Text)) {
                        nm.AddNotification(new Notification("File doesn't exists", "Are you sure you haven't renamed it already?"));
                        return;
                    }
                    files = new string[] { filePath.Text };
                }

                if ((bool)smartRename.IsChecked) SmartRename(files);
                else ClassicRename(files);
            }
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

            if (name == null) { nm.AddNotification("Show was not found", "Are you sure you typed in the correct name in english?"); return; }

            Season season = GetSeason(name, GetSE(Path.GetFileNameWithoutExtension(files[0]), true));
            if (season == null || season.season < 0) return;

            List<Episode> foundEpisodes = ProcessFilesInFolder(files, name, season.season);

            for (int i = 0; i < foundEpisodes.Count; i++) {
                string newPath = Path.GetDirectoryName(foundEpisodes[i].path) + "/" + foundEpisodes[i].title + Path.GetExtension(foundEpisodes[i].path);
                if (!File.Exists(newPath)) System.IO.File.Move(foundEpisodes[i].path, newPath);
                else nm.AddNotification(new Notification("Could not rename", "There is already " + Path.GetFileName(newPath)));
            }
        }
    }
}
