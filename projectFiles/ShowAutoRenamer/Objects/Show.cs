using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Show {
#pragma warning disable 0649
        public string Title;
#pragma warning disable 0649
        public int tvdb_id;

        public IList<Season> seasons = new List<Season>();
    }
}
