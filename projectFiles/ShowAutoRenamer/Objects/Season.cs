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

        public Show show;

        public IList<Episode> episodeList = new List<Episode>();

        public Season(int season, Show s) {
            this.season = season;
            this.show = s;
        }
    }
}
