using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Show {
        public string title;
        public IDs ids;

        public IList<Season> seasonList = new List<Season>();

        public Show() { }

        public Show(string title, int trakt) {
            this.title = title;
            this.ids = new IDs() { trakt = trakt };
        }

        public Show(string title) {
            this.title = title;
        }

        public class IDs {
            public int trakt;
            public string imdb;
            public int tmdb;
            public int tvdb;
        }


    }
}
