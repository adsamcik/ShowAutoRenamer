using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Show {
#pragma warning disable 0649
        public string title;
#pragma warning disable 0649
        public int tvdb_id;

        public IList<Season> seasonList = new List<Season>();

        public Show() { }

        public Show(string title, int tvdb_id) {
            this.title = title;
            this.tvdb_id = tvdb_id;
        }

        public Show(string title) {
            this.title = title;
        }


    }
}
