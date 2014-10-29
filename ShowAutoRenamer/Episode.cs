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

        public Episode() { }
        public Episode(string title) {
            this.title = title;
        }
    }
}
