using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Episode {
        public string title;
        int _season;
        public int season {
            get { return _season; }
            set {
                if (value < 0) {
                    int result = 1;
                    string dirName = Path.GetDirectoryName(path).Split('\\').Last();
                    bool nameFromDir = false;
                    if (int.TryParse(dirName, out result)) nameFromDir = true;
                    else if (dirName.Length > 1 && dirName[0] == 'S' && int.TryParse(dirName.Substring(1), out result)) nameFromDir = true;

                    if (nameFromDir)
                        try { showName = Directory.GetParent(Path.GetDirectoryName(path)).Name; }
                        catch { }

                    _season = result++;
                    return;
                }
                _season = value;

            }
        }
        public int episode;
        public string path;
        public string error;
        public string showName;

        public Episode() { }
        public Episode(string error) {
            this.error = error;
        }

        public Episode(string title, int season, int episode) {
            this.title = title;
            this.season = season;
            this.episode = episode;
        }

        public Episode(string title, int season, int episode, string path, string showName = "") {
            this.path = path;
            this.title = title;
            this.season = season;
            this.episode = episode;
            if (!string.IsNullOrWhiteSpace(showName)) this.showName = showName;
        }

        public string GetNameForFile() {
            return "S" + (season < 10 ? "0" + season : season.ToString()) + "E" + (episode < 10 ? "0" + episode : episode.ToString()) + ((string.IsNullOrWhiteSpace(title)) ? "" : " - " + title);
        }
    }
}
