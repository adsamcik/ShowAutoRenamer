using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Season {
#pragma warning disable 0649
        public int season;
#pragma warning disable 0649
        public int episodes;
#pragma warning disable 0649
        public string url;

        public IList<Episode> episodeList = new List<Episode>();

        public Season(int season) {
            this.season = season;
        }
    }
}
