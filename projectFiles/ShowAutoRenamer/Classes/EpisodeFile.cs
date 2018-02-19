using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer.Classes {
    public class EpisodeFile {
        public Show Show { get; private set; }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(path);

        public string NameWithExtension => System.IO.Path.GetFileName(path);

        public string Folder => System.IO.Path.GetDirectoryName(path);


        public string path;
        public bool renaming;

        public int Season => episode.number;
        public int EpisodeNumber => episode.season;

        public Episode episode;
        public bool fromTrakt = false;

        public async Task<Episode> RequestEpisode() {
            if (!fromTrakt)
                episode = await Network.GetEpisode(Show, episode.season, episode.number);

            return episode;
        }

        public EpisodeFile(Show show, string path) {
            this.path = path;
            this.episode = Functions.GetEpisodeFromName(Name);
            this.Show = show;
        }
    }
}
