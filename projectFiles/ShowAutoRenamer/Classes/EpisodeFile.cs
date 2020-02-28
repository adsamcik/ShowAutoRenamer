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

        public int Season => Episode.number;
        public int EpisodeNumber => Episode.season;

        public Episode Episode { get; private set; }
        public Episode OnlineEpisode { get; private set; }

        public async Task<Episode> RequestEpisode() {
            if (OnlineEpisode == null)
                OnlineEpisode = await Network.GetEpisode(Show, Episode.season, Episode.number);
            
            return OnlineEpisode;
        }

        public EpisodeFile(Show show, string path) {
            this.path = path;
            this.Episode = Functions.GetEpisodeFromName(Name);
            this.Show = show;
        }

        public static EpisodeFile CreateEpisodeFile(Show show, string path) {
            var episode = new EpisodeFile(show, path);
            if (episode.Episode == null)
                return null;
            else
                return episode;
        }
    }
}
