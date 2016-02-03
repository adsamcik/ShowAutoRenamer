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
        public int season;
        public int number;
        public string path;
        public string error;

        public Show show;

        public Episode() { }
        public Episode(string error) {
            this.error = error;
        }

        public Episode(int season, int episode) {
            this.season = season;
            this.number = episode;
        }

        public Episode(string title, int season, int episode, Show s) {
            this.title = title;
            this.season = season;
            this.number = episode;
            this.show = s;
        }

        public Episode(string title, int season, int episode, string path, Show s) {
            this.path = path;
            this.title = title;
            this.season = season;
            this.number = episode;
            this.show = s;
        }

        public string GetNameForFile() {
            return Functions.RegexTitle(Path.GetFileNameWithoutExtension(path),this) + Path.GetExtension(path);
        }
    }
}
