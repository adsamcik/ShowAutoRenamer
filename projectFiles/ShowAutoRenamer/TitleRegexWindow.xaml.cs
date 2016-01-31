using ShowAutoRenamer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShowAutoRenamer {
    /// <summary>
    /// Interaction logic for TitleRegexWindow.xaml
    /// </summary>
    public partial class TitleRegexWindow : Window {
        Episode e;
        int episodeAdd, seasonAdd;

        public TitleRegexWindow() {
            InitializeComponent();
            this.e = new Episode("Episode name", 1, 1, new ShowAutoRenamer.Show("Show name"));
            textBoxTitleRegex.Text = RenameData.regex;
        }

        public void Initialize(Episode e) {
            this.e = e;

            if (RenameData.isRegexSet)
                textBlockTitlePreview.Text = RegexTitle(textBoxTitleRegex.Text, this.e);

        }

        public void GetResults(out string regex, out int episodeAdd, out int seasonAdd) {
            regex = textBoxTitleRegex.Text;
            episodeAdd = this.episodeAdd;
            seasonAdd = this.seasonAdd;
        }

        private void DoneButtonClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        private void textBoxTitleRegex_TextChanged(object sender, TextChangedEventArgs e) {
            if (IsLoaded && !string.IsNullOrEmpty(textBoxTitleRegex.Text))
                textBlockTitlePreview.Text = RegexTitle(textBoxTitleRegex.Text, this.e);
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        string RegexTitle(string regex, Episode e) {
            regex = Regex.Replace(regex, "{title}", e.title, RegexOptions.IgnoreCase);
            regex = Regex.Replace(regex, "{showname}", e.show.title, RegexOptions.IgnoreCase);
            if (!DetectSubAddRegex(ref regex, "season", e.season, out seasonAdd))
                regex = Functions.ResolveZeroFormat("season", regex, e.season);
            if (!DetectSubAddRegex(ref regex, "episode", e.number, out episodeAdd))
                regex = Functions.ResolveZeroFormat("episode", regex, e.season);

            return regex;
        }

        bool DetectSubAddRegex(ref string text, string before, int beforeVal, out int addValue) {
            Match m;
            if ((m = Regex.Match(text, "{.?" + before + ".*?}")).Success) {
                if (m.Index + m.Length < text.Length && (text[m.Index + m.Length] == '-' || text[m.Index + m.Length] == '+')) {
                    int startAt = m.Index + m.Length;
                    int index = text.IndexOf('}', startAt);
                    if (index >= 0) {
                        int result;
                        if (int.TryParse(text.Substring(startAt, index - startAt), out result)) {
                            //text = Regex.Replace(text, "{.?" + before + "[\\+\\-0-9]+}", (beforeVal + result).ToString(isZero ? 2 : 1), RegexOptions.IgnoreCase);
                            text = Functions.ResolveZeroFormat(before, text, beforeVal + result);
                            addValue = result;
                            return true;
                        }
                    }
                }
            }
            addValue = 0;
            return false;
        }

    }
}
