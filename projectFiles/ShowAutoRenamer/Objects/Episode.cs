using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Episode {
        public string title;
        public int season;
        public int episode;
        public string path;
        public string error;

        public Episode() { }
        public Episode(string error) {
            this.error = error;
        }
        public Episode(string title, int season, int episode) {
            this.title = title;
            this.season = season;
            this.episode = episode;
        }
        public Episode(string title, int season, int episode, string path) {
            this.title = title;
            this.season = season;
            this.episode = episode;
            this.path = path;
        }
    }
}
