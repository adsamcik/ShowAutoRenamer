using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowAutoRenamer {
    public class Notification {
        public string title;
        public string text;

        public bool searchRelated;

        public Notification(string title, string text, bool searchRelated = false) {
            this.title = title;
            this.text = text;
            this.searchRelated = searchRelated;
        }
    }
}
