using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer.Classes {
    public class EpisodeFile {
        public string name {
            get {
                return System.IO.Path.GetFileNameWithoutExtension(path);
            }
        }

        public string nameWithExtension {
            get {
                return System.IO.Path.GetFileName(path);
            }
        }

        public string folder {
            get {
                return System.IO.Path.GetDirectoryName(path);
            }
        }

        public string path;
        public bool renaming;

        public EpisodeFile(string path) {
            this.path = path;
        }
    }
}
