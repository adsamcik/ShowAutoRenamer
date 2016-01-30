using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer.Classes {
    public static class RenameData {
        public static int episodeAdd, seasonAdd;
        public static string regex;

        public static void Clear() {
            episodeAdd = 0;
            seasonAdd = 0;
            regex = null;
        }
    }
}
